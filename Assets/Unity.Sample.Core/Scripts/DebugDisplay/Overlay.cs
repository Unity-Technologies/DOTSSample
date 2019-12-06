using UnityEngine;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using System.Runtime.InteropServices;
using System.IO;
using System;
using Unity.Burst;
using Unity.Sample.Core;
using UnityEngine.Rendering;

namespace Unity.DebugDisplay
{
    public struct Overlay
    {
        public enum Color
        {
            Black,
            Blue,
            Green,
            Cyan,
            Red,
            Magenta,
            Brown,
            Gray,
            DarkGray,
            BrightBlue,
            BrightGreen,
            BrightCyan,
            BrightRed,
            BrightMagenta,
            Yellow,
            White
        }

        public void Clear()
        {
            m_Text.ClearCells();
            m_TextReservations = m_Text.ReserveAll(); // clear out all the tex boxes
            m_GraphReservations = m_Graph.ReserveAll(); // clear out all the graphs
            m_LineReservations = m_Line.ReserveAll(); // clear out all the lines
        }

        public Text.Reservation m_TextReservations;
        public Line.Reservation m_LineReservations;
        public Graph.Reservation m_GraphReservations;
        public Graph.Data.Reservation m_GraphDataReservations;

        public struct Unit
        {
            public int m_begin;
            public int m_next;
            public int m_end;

            public Unit(int me, int writers, int writableBegin, int writableEnd)
            {
                var writables = writableEnd - writableBegin;
                m_begin = writableBegin + (me * writables) / writers;
                m_end = writableBegin + ((me + 1) * writables) / writers;
                if (m_begin > writableEnd)
                    m_begin = writableEnd;
                if (m_end > writableEnd)
                    m_end = writableEnd;
                m_next = m_begin;
            }

            public void Fill()
            {
                m_next = m_end;
            }

            public int Length => m_end - m_begin;
            public int Filled => m_next - m_begin;
            public int Remaining => m_end - m_next;
        }

        public struct Text : IDisposable
        {
            [StructLayout(LayoutKind.Explicit)]
            public struct Cell
            {
                [FieldOffset(0)] public byte unicodelo;
                [FieldOffset(1)] public byte color;
                [FieldOffset(2)] public ushort unicodehi;

                public uint unicode
                {
                    get
                    {
                        uint value = (uint) (unicodelo & 0x7f);
                        if ((unicodelo & 0x80) == 0x80)
                            value |= (uint) unicodehi << 7;
                        return value;
                    }
                    set
                    {
                        unicodelo = (byte) (value & 0x7f);
                        if (value > 0x7f)
                            unicodelo |= 0x80;
                        unicodehi = (ushort) (value >> 7);
                    }
                }

                public Color fg
                {
                    get => (Color) ((color >> 0) & 0xf);
                    set => color = (byte) ((color & 0xF0) | (((int) value & 0xF) << 0));
                }

                public Color bg
                {
                    get => (Color) ((color >> 4) & 0x7);
                    set => color = (byte) ((color & 0x8F) | (((int) value & 0x7) << 4));
                }

                public bool side
                {
                    get => (color & 0x80) == 0x80;
                    set
                    {
                        color &= 0x7F;
                        color = (byte) (color | (value ? 0x80 : 0x00));
                    }
                }
            }

            public struct Instance
            {
                public Vector3 worldPosition;
                public Vector2 firstCell;
                public Vector2 cellSize;
                public uint useWorldMatrix;
            }

            public const int kMaxInstances = 128;
            public const int kMaxColors = 16;

            public const int kMaxDisplayPixelsWide = 3840; // no support for 8K or 16K, yet
            public const int kMaxDisplayPixelsTall = 2160;
            public const int kCellPixelsWide = 8;
            public const int kCellPixelsTall = 16;
            public const int kMaxCellsWide = (kMaxDisplayPixelsWide + kCellPixelsWide - 1) / kCellPixelsWide;
            public const int kMaxCellsTall = (kMaxDisplayPixelsTall + kCellPixelsTall - 1) / kCellPixelsTall;
            public const int kMaxCells = kMaxCellsWide * kMaxCellsTall;

            [BurstDiscard] public int CellsWide => m_CellsWide;
            [BurstDiscard] public int CellsTall => m_CellsTall;

            public int m_CellsWide;
            public int m_CellsTall;
            public NativeArray<Instance> m_Instance;
            public NativeArray<Cell> m_Cell;
            public NativeArray<byte> m_Wide; // 1 bit for each UNICODE code point: am I wide (Chinese) or not (Roman)

            public unsafe void ClearCells()
            {
                UnsafeUtility.MemClear(m_Cell.GetUnsafePtr(), m_Cell.Length * sizeof(Cell));
            }
            public void Initialize(int cellsWide, int cellsTall)
            {
                m_CellsWide = cellsWide;
                m_CellsTall = cellsTall;
                m_Instance = new NativeArray<Instance>(kMaxInstances, Allocator.Persistent);
                m_Cell = new NativeArray<Cell>(kMaxCells, Allocator.Persistent);
                m_Wide = new NativeArray<byte>(8192, Allocator.Persistent);
            }

            public void Initialize()
            {
                Initialize(Screen.width / kCellPixelsWide, Screen.height / kCellPixelsTall);
            }

            public void ClearTextBox(int index)
            {
                m_Instance[index] = new Instance();
            }

            public unsafe void SetTextBox(int cellX, int cellY, int cellW, int cellH, int index)
            {
                m_Instance[index] = new Instance
                {
                    worldPosition = new Vector2(cellX * kCellPixelsWide, cellY * kCellPixelsTall),
                    firstCell = new Vector2(cellX, cellY),
                    cellSize = new Vector2(cellW, cellH),
                    useWorldMatrix = 0
                };
            }

            public unsafe void SetTextBoxSmooth(Vector3 position, int cellX, int cellY, int cellW, int cellH, int index)
            {
                m_Instance[index] = new Instance
                {
                    worldPosition = position,
                    firstCell = new Vector2(cellX, cellY),
                    cellSize = new Vector2(cellW, cellH),
                    useWorldMatrix = 1
                };
            }

            public bool isWide(uint c)
            {
                var bits = m_Wide[(int) c >> 3];
                var mask = 1 << (int) (c & 7);
                return (bits & mask) != 0;
            }

            public void PutChar(int x, int y, Cell t)
            {
                if (x < 0 || x >= kMaxCellsWide || y < 0 || y >= kMaxCellsTall)
                    return;
                var offset = y * kMaxCellsWide + x;
                m_Cell[offset] = t;
            }

            public unsafe void PutChars(int x, int y, Cell t, int count)
            {
                while (count-- != 0)
                    PutChar(x++, y, t);
            }

            public void PutChar(ref int x, int y, uint utf32, Color fg, Color bg)
            {
                Cell t = new Cell {unicode = utf32, fg = fg, bg = bg};
                PutChar(x++, y, t);
                if (isWide(t.unicode))
                {
                    t.side = true;
                    PutChar(x++, y, t);
                }
            }

            public void PutChar(ref int x, int y, char utf16, Color fg, Color bg)
            {
                PutChar(ref x, y, (uint) utf16, fg, bg);
            }

            unsafe bool IsSurrogatePair(char* c, int index)
            {
                if (c[0] >= 0xD800 && c[0] <= 0xDBFF && c[1] >= 0xDC00 && c[1] <= 0xDFFF)
                    return true;
                return false;
            }

            unsafe uint ConvertToUtf32(char* c, int index)
            {
                if (!IsSurrogatePair(c, index))
                    return c[index];
                uint hi = (uint) c[index] - 0xD800;
                uint lo = (uint) c[index] - 0xDC00;
                uint offset = (hi << 10) | lo;
                return 0x10000 + offset;
            }

            unsafe public void PutChars(ref int x, int y, char* c, int length, Color fg, Color bg)
            {
                for (var i = 0; i < length; i += IsSurrogatePair(c, i) ? 2 : 1)
                {
                    uint utf32 = ConvertToUtf32(c, i);
                    PutChar(ref x, y, utf32, fg, bg);
                }
            }

            public unsafe void SetLabel(Vector3 position, int cellX, int cellY, char* c, int length, Color fg, Color bg,
                int index)
            {
                SetTextBoxSmooth(position, cellX, cellY, length, 1, index);
                PutChars(ref cellX, cellY, c, length, fg, bg);
            }

            public void Dispose()
            {
                m_Instance.Dispose();
                m_Cell.Dispose();
                m_Wide.Dispose();
            }

            public Reservation ReserveAll()
            {
                return new Reservation
                {
                    m_text = this,
                    m_unit = new Unit(0, 1, 0, m_Instance.Length)
                };
            }

            public struct Reservation : IDisposable
            {
                public Text m_text;
                public Unit m_unit;

                public Reservation Reserve(int count)
                {
                    if (m_unit.m_next + count > m_unit.m_end)
                        count = m_unit.m_end - m_unit.m_next;
                    var result = new Reservation
                    {
                        m_text = m_text,
                        m_unit = new Unit(0, 1, m_unit.m_next, m_unit.m_next + count)
                    };
                    m_unit.m_next += count;
                    return result;
                }

                public Reservation PartitionForJob(int me, int writers)
                {
                    return new Reservation
                    {
                        m_text = m_text,
                        m_unit = new Unit(me, writers, m_unit.m_begin, m_unit.m_end)
                    };
                }

                public void AddTextBox(int x, int y, int w, int h)
                {
                    if (m_unit.m_next < m_unit.m_end)
                        m_text.SetTextBox(x, y, w, h, m_unit.m_next++);
                }

                public unsafe void AddLabel(Vector3 position, char* c, int length, Color fg, Color bg)
                {
                    if (m_unit.m_next < m_unit.m_end)
                        m_text.SetLabel(position, 0, m_text.m_CellsTall + m_unit.m_next, c, length, fg, bg,
                            m_unit.m_next++);
                }

                [BurstDiscard]
                public unsafe void AddLabel(Vector3 position, string s, Color fg, Color bg)
                {
                    fixed (char* c = s)
                        AddLabel(position, c, s.Length, fg, bg);
                }

                public void Dispose()
                {
                    while (m_unit.m_next < m_unit.m_end)
                        m_text.ClearTextBox(m_unit.m_next++);
                }
            }
        }

        public struct Graph : IDisposable
        {
            public struct Data
            {
                public int offset;
                public int length;

                public void Validate()
                {
                    if ((length & (length - 1)) != 0)
                        throw new ArgumentException(
                            $"Length of data in graph is {length} which must be a power of two.");
                }

                public struct Reservation
                {
                    public Graph m_graph;
                    public Unit m_unit;

                    public Reservation Reserve(int count)
                    {
                        if (m_unit.m_next + count > m_unit.m_end)
                            count = m_unit.m_end - m_unit.m_next;
                        var result = new Reservation
                        {
                            m_graph = m_graph,
                            m_unit = new Unit(0, 1, m_unit.m_next, m_unit.m_next + count)
                        };
                        m_unit.m_next += count;
                        return result;
                    }

                    public Reservation PartitionForJob(int me, int writers)
                    {
                        return new Reservation
                        {
                            m_graph = m_graph,
                            m_unit = new Unit(me, writers, m_unit.m_begin, m_unit.m_end)
                        };
                    }

                    public float GetValue(int offset)
                    {
                        return m_graph.m_Data[m_unit.m_begin + (offset & (m_unit.Length - 1))];
                    }

                    public void SetValue(int offset, float value)
                    {
                        m_graph.m_Data[m_unit.m_begin + (offset & (m_unit.Length - 1))] = value;
                    }

                    public void AddValue(float value)
                    {
                        if (m_unit.m_next >= m_unit.m_end)
                            m_unit.m_next = m_unit.m_begin;
                        m_graph.m_Data[m_unit.m_next++] = value;
                    }

                    public Data GetData()
                    {
                        return new Data {offset = m_unit.m_begin, length = m_unit.Length};
                    }

                    public unsafe void CalcMinMaxMean(out float minValue, out float maxValue, out float mean)
                    {
                        float* f = (float*) m_graph.m_Data.GetUnsafePtr();
                        minValue = f[m_unit.m_begin];
                        maxValue = f[m_unit.m_begin];
                        float sum = f[m_unit.m_begin];
                        for (var i = m_unit.m_begin + 1; i < m_unit.m_end; i++)
                        {
                            var x = f[i];
                            sum += x;
                            if (x < minValue) minValue = x;
                            if (x > maxValue) maxValue = x;
                        }

                        mean = sum / m_unit.Length;
                    }

                    public unsafe void CalcStatistics(out float mean, out float variance, out float minValue,
                        out float maxValue)
                    {
                        CalcMinMaxMean(out minValue, out maxValue, out mean);
                        float* f = (float*) m_graph.m_Data.GetUnsafePtr();
                        float sum2 = 0;
                        for (var i = m_unit.m_begin; i < m_unit.m_end; i++)
                        {
                            float d = f[i] - mean;
                            sum2 += d * d;
                        }

                        variance = sum2 / m_unit.Length;
                    }
                }
            }

            public Data.Reservation ReserveAllData()
            {
                return new Data.Reservation
                {
                    m_graph = this,
                    m_unit = new Unit(0, 1, 0, m_Data.Length)
                };
            }

            public struct Sample
            {
                public Data data; // offset of first datum, count of data
                public Color color; // color of data
                public float xMin; // first data to display
                public float xMax; // last data to display
                public float yMin; // first Y value to display
                public float yMax; // last Y value to display
            }

            public struct InstanceSample
            {
                public int color; // color of the sample
                public int firstIndex; // first sample index in range to display
                public int indexMask; // AND sample index with this to make it wrap around
                public float indexMul; // multiply the pixel.x by this,
                public float indexAdd; // and then by this to get the sample index.
                public float sampleMul; // multiply the sample by this,
                public float sampleAdd; // and then add this to get the pixel.y
            }

            public struct Instance
            {
                public Vector2 screenPosition;
                public Vector2 cellSize;
                public int frameColor;
                public int samples;
                public InstanceSample sample0;
                public InstanceSample sample1;
            };

            public const int kMaxDisplayPixelsWide = 3840; // no support for 8K or 16K, yet
            public const int kMaxDisplayPixelsTall = 2160;
            public const int kCellPixelsWide = 8;
            public const int kCellPixelsTall = 16;
            public const int kMaxCellsWide = (kMaxDisplayPixelsWide + kCellPixelsWide - 1) / kCellPixelsWide;
            public const int kMaxCellsTall = (kMaxDisplayPixelsTall + kCellPixelsTall - 1) / kCellPixelsTall;
            public const int kMaxCells = kMaxCellsWide * kMaxCellsTall;

            public const int kMaxInstances = 16;
            public const int kMaxValues = 4096;
            public const int kMaxColors = 16;
            public NativeArray<Instance> m_Instance;
            public NativeArray<float> m_Data;

            [BurstDiscard] public static int CellsWide => Screen.width / kCellPixelsWide;
            [BurstDiscard] public static int CellsTall => Screen.height / kCellPixelsTall;

            public void Initialize()
            {
                m_Instance = new NativeArray<Instance>(kMaxInstances, Allocator.Persistent);
                m_Data = new NativeArray<float>(kMaxValues, Allocator.Persistent);
            }

            float recip(float f)
            {
                return (f == 0.0f) ? 1 : 1.0f / f;
            }

            public void ClearGraph(int index)
            {
                m_Instance[index] = new Instance();
            }

            public void SetGraph(int x, int y, int w, int h, Sample a, int index)
            {
                if (index >= m_Instance.Length)
                    return;
                a.data.Validate();

                a.xMax += 1;

                float axScale = (a.xMax - a.xMin) / (w * 8 - 1);
                float ayScale = (h * 16 - 2) * recip(a.yMin - a.yMax);

                m_Instance[index] = new Instance
                {
                    screenPosition = new Vector2(x * Graph.kCellPixelsWide, y * Graph.kCellPixelsTall),
                    cellSize = new Vector2(w, h),
                    frameColor = (int) Color.White,
                    samples = 1,
                    sample0 = new InstanceSample
                    {
                        color = (int) a.color,
                        firstIndex = a.data.offset,
                        indexMask = a.data.length - 1,
                        indexMul = axScale,
                        indexAdd = a.xMin,
                        sampleMul = ayScale,
                        sampleAdd = ayScale * -a.yMax + 1,
                    }
                };
            }

            public unsafe void SetGraph(int x, int y, int w, int h, Sample a, Sample b, int index)
            {
                if (index >= m_Instance.Length)
                    return;
                a.data.Validate();
                b.data.Validate();

                a.xMax += 1;
                b.xMax += 1;

                float axScale = (a.xMax - a.xMin) / (w * 8 - 1);
                float ayScale = (h * 16 - 2) * recip(a.yMin - a.yMax);
                float bxScale = (b.xMax - b.xMin) / (w * 8 - 1);
                float byScale = (h * 16 - 2) * recip(b.yMin - b.yMax);

                m_Instance[index] = new Instance
                {
                    screenPosition = new Vector2(x * Graph.kCellPixelsWide, y * Graph.kCellPixelsTall),
                    cellSize = new Vector2(w, h),
                    frameColor = (int) Color.White,
                    samples = 2,
                    sample0 = new InstanceSample
                    {
                        color = (int) a.color,
                        firstIndex = a.data.offset,
                        indexMask = a.data.length - 1,
                        indexMul = axScale,
                        indexAdd = a.xMin,
                        sampleMul = ayScale,
                        sampleAdd = ayScale * -a.yMax + 1,
                    },
                    sample1 = new InstanceSample
                    {
                        color = (int) b.color,
                        firstIndex = b.data.offset,
                        indexMask = b.data.length - 1,
                        indexMul = bxScale,
                        indexAdd = b.xMin,
                        sampleMul = byScale,
                        sampleAdd = byScale * -b.yMax + 1,
                    }
                };
            }

            public void Dispose()
            {
                m_Instance.Dispose();
                m_Data.Dispose();
            }

            public Reservation ReserveAll()
            {
                return new Reservation
                {
                    m_graph = this,
                    m_unit = new Unit(0, 1, 0, m_Instance.Length)
                };
            }

            public struct Reservation : IDisposable
            {
                public Graph m_graph;
                public Unit m_unit;

                public Reservation Reserve(int count)
                {
                    if (m_unit.m_next + count > m_unit.m_end)
                        count = m_unit.m_end - m_unit.m_next;
                    var result = new Reservation
                    {
                        m_graph = m_graph,
                        m_unit = new Unit(0, 1, m_unit.m_next, m_unit.m_next + count)
                    };
                    m_unit.m_next += count;
                    return result;
                }

                public Reservation PartitionForJob(int me, int writers)
                {
                    return new Reservation
                    {
                        m_graph = m_graph,
                        m_unit = new Unit(me, writers, m_unit.m_begin, m_unit.m_end)
                    };
                }

                public void AddGraph(int x, int y, int w, int h, Graph.Sample a)
                {
                    if (m_unit.m_next < m_unit.m_end)
                        m_graph.SetGraph(x, y, w, h, a, m_unit.m_next++);
                }

                public void AddGraph(int x, int y, int w, int h, Graph.Sample a, Graph.Sample b)
                {
                    if (m_unit.m_next < m_unit.m_end)
                        m_graph.SetGraph(x, y, w, h, a, b, m_unit.m_next++);
                }

                public void Dispose()
                {
                    while (m_unit.m_next < m_unit.m_end)
                        m_graph.ClearGraph(m_unit.m_next++);
                }
            }
        }

        public struct Line : IDisposable
        {
            const int kMaxLines = 500;

            public struct Instance
            {
                public Vector4 m_begin;
                public Vector4 m_end;
            }

            public NativeArray<Instance> m_Instance;

            public void Initialize()
            {
                m_Instance = new NativeArray<Instance>(kMaxLines, Allocator.Persistent);
            }

            public void SetLine(Vector3 begin, Vector3 end, Color color, int index)
            {
                m_Instance[index] = new Instance
                {
                    m_begin = new Vector4(begin.x, begin.y, begin.z, (uint) color),
                    m_end = new Vector4(end.x, end.y, end.z, (uint) color)
                };
            }

            public void ClearLine(int index)
            {
                m_Instance[index] = new Instance { };
            }

            public void Dispose()
            {
                m_Instance.Dispose();
            }

            public Reservation ReserveAll()
            {
                return new Reservation
                {
                    m_line = this,
                    m_unit = new Unit(0, 1, 0, m_Instance.Length)
                };
            }

            public struct Reservation : IDisposable
            {
                public Line m_line;
                public Unit m_unit;

                public Reservation Reserve(int count)
                {
                    if (m_unit.m_next + count > m_unit.m_end)
                        count = m_unit.m_end - m_unit.m_next;
                    var result = new Reservation
                    {
                        m_line = m_line,
                        m_unit = new Unit(0, 1, m_unit.m_next, m_unit.m_next + count)
                    };
                    m_unit.m_next += count;
                    return result;
                }

                public Reservation PartitionForJob(int me, int writers)
                {
                    return new Reservation
                    {
                        m_line = m_line,
                        m_unit = new Unit(me, writers, m_unit.m_begin, m_unit.m_end)
                    };
                }

                public void AddLine(Vector3 begin, Vector3 end, Color color)
                {
                    if (m_unit.m_next < m_unit.m_end)
                        m_line.SetLine(begin, end, color, m_unit.m_next++);
                }

                public void Dispose()
                {
                    while (m_unit.m_next < m_unit.m_end)
                        m_line.ClearLine(m_unit.m_next++);
                }
            }
        }

        public Text m_Text;
        public Graph m_Graph;
        public Line m_Line;

        public const int kMaxColors = 16;

        public NativeArray<Vector4> m_ColorData;

        public void Initialize()
        {
            m_ColorData = new NativeArray<Vector4>(kMaxColors, Allocator.Persistent);
            m_Text.Initialize();
            m_Graph.Initialize();
            m_Line.Initialize();
            m_TextReservations = m_Text.ReserveAll();
            m_GraphReservations = m_Graph.ReserveAll();
            m_GraphDataReservations = m_Graph.ReserveAllData();
            m_LineReservations = m_Line.ReserveAll();
        }

        public Color Quantize(Vector4 color)
        {
            int index = 0;
            float best = (color - m_ColorData[0]).SqrMagnitude();
            for (int i = 1; i < m_ColorData.Length; ++i)
            {
                float distance = (color - m_ColorData[i]).SqrMagnitude();
                if (distance < best)
                {
                    best = distance;
                    index = i;
                }
            }

            return (Color) index;
        }

        public void Dispose()
        {
            m_Graph.Dispose();
            m_Text.Dispose();
            m_Line.Dispose();
            m_ColorData.Dispose();
        }

        public class Managed
        {
            [ConfigVar(Name = "show.overlay", DefaultValue = "1", Description = "Show the debug overlay")]
            public static ConfigVar showOverlay;

            public Overlay m_Unmanaged;

            Color m_ForegroundColor = Color.White;
            Color m_BackgroundColor = Color.Black;

            public int visibleLinesOfText = 40;

            static DebugDisplayResources resources;

            public static int PixelsWide => Screen.width;
            public static int PixelsTall => Screen.height;

            public static int CellsWide =>
                (PixelsWide + Overlay.Text.kCellPixelsWide - 1) / Overlay.Text.kCellPixelsWide;

            public static int CellsTall =>
                (PixelsTall + Overlay.Text.kCellPixelsTall - 1) / Overlay.Text.kCellPixelsTall;


            public void Init()
            {
                m_Unmanaged.Initialize();
                if (resources == null)
                {
                    resources = Resources.Load<DebugDisplayResources>("DebugDisplayResources");
                    Debug.Assert(resources != null, "Unable to load DebugDisplayResources");
                    Stream s = new MemoryStream(resources.wide.bytes);
                    BinaryReader br = new BinaryReader(s);
                    var wide = new byte[s.Length];
                    br.Read(wide, 0, (int) s.Length);
                    for (var i = 0; i < s.Length; ++i)
                        m_Unmanaged.m_Text.m_Wide[i] = wide[i];
                }

                SetPalette(windows);
            }

            public void Shutdown()
            {
                if (m_GraphSampleBuffer != null)
                    m_GraphSampleBuffer.Release();
                m_GraphSampleBuffer = null;
                if (m_GraphInstanceBuffer != null)
                    m_GraphInstanceBuffer.Release();
                m_GraphInstanceBuffer = null;

                if (m_TextCellBuffer != null)
                    m_TextCellBuffer.Release();
                m_TextCellBuffer = null;
                if (m_TextInstanceBuffer != null)
                    m_TextInstanceBuffer.Release();
                m_TextInstanceBuffer = null;

                if (m_LineVertexBuffer != null)
                    m_LineVertexBuffer.Release();
                m_LineVertexBuffer = null;

                if (m_ColorBuffer != null)
                    m_ColorBuffer.Release();
                m_ColorBuffer = null;

                m_Unmanaged.Dispose();
            }

            public void TickLateUpdate()
            {
                // Recreate compute buffer if needed.
                if (m_ColorBuffer == null || m_ColorBuffer.count != m_Unmanaged.m_ColorData.Length)
                {
                    if (m_ColorBuffer != null)
                    {
                        m_ColorBuffer.Release();
                        m_ColorBuffer = null;
                    }

                    m_ColorBuffer = new ComputeBuffer(m_Unmanaged.m_ColorData.Length, UnsafeUtility.SizeOf<Vector4>());
                    resources.textMaterial.SetBuffer("colorBuffer", m_ColorBuffer);
                    resources.graphMaterial.SetBuffer("colorBuffer", m_ColorBuffer);
                    resources.lineMaterial.SetBuffer("colorBuffer", m_ColorBuffer);
                }

                if (m_TextCellBuffer == null || m_TextCellBuffer.count != m_Unmanaged.m_Text.m_Cell.Length)
                {
                    if (m_TextCellBuffer != null)
                    {
                        m_TextCellBuffer.Release();
                        m_TextCellBuffer = null;
                    }

                    m_TextCellBuffer = new ComputeBuffer(m_Unmanaged.m_Text.m_Cell.Length,
                        UnsafeUtility.SizeOf<Overlay.Text.Cell>());
                    resources.textMaterial.SetBuffer("textBuffer", m_TextCellBuffer);
                }

                if (m_TextInstanceBuffer == null || m_TextInstanceBuffer.count != m_Unmanaged.m_Text.m_Instance.Length)
                {
                    if (m_TextInstanceBuffer != null)
                    {
                        m_TextInstanceBuffer.Release();
                        m_TextInstanceBuffer = null;
                    }

                    m_TextInstanceBuffer = new ComputeBuffer(m_Unmanaged.m_Text.m_Instance.Length,
                        UnsafeUtility.SizeOf<Overlay.Text.Instance>());
                    resources.textMaterial.SetBuffer("positionBuffer", m_TextInstanceBuffer);
                }

                if (m_LineVertexBuffer == null || m_LineVertexBuffer.count != m_Unmanaged.m_Line.m_Instance.Length)
                {
                    if (m_LineVertexBuffer != null)
                    {
                        m_LineVertexBuffer.Release();
                        m_LineVertexBuffer = null;
                    }

                    m_LineVertexBuffer = new ComputeBuffer(m_Unmanaged.m_Line.m_Instance.Length,
                        UnsafeUtility.SizeOf<Overlay.Line.Instance>());
                    resources.lineMaterial.SetBuffer("positionBuffer", m_LineVertexBuffer);
                }

                if (m_GraphSampleBuffer == null || m_GraphSampleBuffer.count != m_Unmanaged.m_Graph.m_Data.Length)
                {
                    if (m_GraphSampleBuffer != null)
                    {
                        m_GraphSampleBuffer.Release();
                        m_GraphSampleBuffer = null;
                    }

                    m_GraphSampleBuffer =
                        new ComputeBuffer(m_Unmanaged.m_Graph.m_Data.Length, UnsafeUtility.SizeOf<float>());
                    resources.graphMaterial.SetBuffer("sampleBuffer", m_GraphSampleBuffer);
                }

                if (m_GraphInstanceBuffer == null ||
                    m_GraphInstanceBuffer.count != m_Unmanaged.m_Graph.m_Instance.Length)
                {
                    if (m_GraphInstanceBuffer != null)
                    {
                        m_GraphInstanceBuffer.Release();
                        m_GraphInstanceBuffer = null;
                    }

                    m_GraphInstanceBuffer = new ComputeBuffer(m_Unmanaged.m_Graph.m_Instance.Length,
                        UnsafeUtility.SizeOf<Overlay.Graph.Instance>());
                    resources.graphMaterial.SetBuffer("instanceBuffer", m_GraphInstanceBuffer);
                }

                m_ColorBuffer.SetData(m_Unmanaged.m_ColorData);
                m_TextCellBuffer.SetData(m_Unmanaged.m_Text.m_Cell);
                m_GraphSampleBuffer.SetData(m_Unmanaged.m_Graph.m_Data);

                m_NumGraphsToDraw = m_Unmanaged.m_GraphReservations.m_unit.Filled;
                m_NumTextBoxesToDraw = m_Unmanaged.m_TextReservations.m_unit.Filled;
                m_NumLinesToDraw = m_Unmanaged.m_LineReservations.m_unit.Filled;

                m_GraphInstanceBuffer.SetData(m_Unmanaged.m_Graph.m_Instance, 0, 0, m_NumGraphsToDraw);
                m_TextInstanceBuffer.SetData(m_Unmanaged.m_Text.m_Instance, 0, 0, m_NumTextBoxesToDraw);
                m_LineVertexBuffer.SetData(m_Unmanaged.m_Line.m_Instance, 0, 0, m_NumLinesToDraw);

                var scales = new Vector4(1.0f / CellsWide, 1.0f / CellsTall, 1.0f / PixelsWide, 1.0f / PixelsTall);
                resources.textMaterial.SetVector("scales", scales);
                resources.graphMaterial.SetVector("scales", scales);
                resources.lineMaterial.SetVector("scales", scales);

                _Clear();
            }

            public static void SetColor(Color col)
            {
                if (instance == null)
                    return;
                instance.m_ForegroundColor = col;
            }

            public unsafe static void Write(int x, int y, char[] buf, int count)
            {
                if (instance == null)
                    return;
                for (var i = 0; i < count; i++)
                    instance.m_Unmanaged.m_Text.PutChar(ref x, y, buf[i], instance.m_ForegroundColor,
                        instance.m_BackgroundColor);
            }

            public static void Write(int x, int y, string format)
            {
                if (instance == null)
                    return;
                var l = StringFormatter.Write(ref _buf, 0, format);
                instance._DrawText(x, y, ref _buf, l);
            }

            public static void Write<T>(int x, int y, string format, T arg)
            {
                if (instance == null)
                    return;
                var l = StringFormatter.Write<T>(ref _buf, 0, format, arg);
                instance._DrawText(x, y, ref _buf, l);
            }

            public static void Write(Color col, int x, int y, string format)
            {
                if (instance == null)
                    return;
                var c = instance.m_ForegroundColor;
                instance.m_ForegroundColor = col;
                var l = StringFormatter.Write(ref _buf, 0, format);
                instance._DrawText(x, y, ref _buf, l);
                instance.m_ForegroundColor = c;
            }

            public static void Write<T>(Color col, int x, int y, string format, T arg)
            {
                if (instance == null)
                    return;
                var c = instance.m_ForegroundColor;
                instance.m_ForegroundColor = col;
                var l = StringFormatter.Write(ref _buf, 0, format, arg);
                instance._DrawText(x, y, ref _buf, l);
                instance.m_ForegroundColor = c;
            }

            public static void Write<T0, T1>(Color col, int x, int y, string format, T0 arg0, T1 arg1)
            {
                if (instance == null)
                    return;
                var c = instance.m_ForegroundColor;
                instance.m_ForegroundColor = col;
                var l = StringFormatter.Write(ref _buf, 0, format, arg0, arg1);
                instance._DrawText(x, y, ref _buf, l);
                instance.m_ForegroundColor = c;
            }

            public static void Write<T0, T1>(int x, int y, string format, T0 arg0, T1 arg1)
            {
                if (instance == null)
                    return;
                var l = StringFormatter.Write(ref _buf, 0, format, arg0, arg1);
                instance._DrawText(x, y, ref _buf, l);
            }

            public static void Write<T0, T1, T2>(int x, int y, string format, T0 arg0, T1 arg1, T2 arg2)
            {
                if (instance == null)
                    return;
                var l = StringFormatter.Write(ref _buf, 0, format, arg0, arg1, arg2);
                instance._DrawText(x, y, ref _buf, l);
            }

            public static void Write<T0, T1, T2, T3>(int x, int y, string format, T0 arg0, T1 arg1, T2 arg2, T3 arg3)
            {
                if (instance == null)
                    return;
                var l = StringFormatter.Write(ref _buf, 0, format, arg0, arg1, arg2, arg3);
                instance._DrawText(x, y, ref _buf, l);
            }

            void _DrawText(int x, int y, ref char[] text, int length)
            {
                const string hexes = "0123456789ABCDEF";
                var col = m_ForegroundColor;
                for (var i = 0; i < length; i++)
                {
                    if (text[i] == '^' && i < length - 3)
                    {
                        var r = hexes.IndexOf(text[i + 1]);
                        var g = hexes.IndexOf(text[i + 2]);
                        var b = hexes.IndexOf(text[i + 3]);
                        var rgba = new Vector4(r, g, b, 15) / 15.0f;
                        col = m_Unmanaged.Quantize(rgba);
                        i += 3;
                        continue;
                    }

                    m_Unmanaged.m_Text.PutChar(ref x, y, text[i], col, 0);
                }
            }

            void _Clear()
            {
                m_Unmanaged.Clear();
                m_Unmanaged.m_TextReservations.AddTextBox(0, 0, Overlay.Text.kMaxCellsWide,
                    visibleLinesOfText); // steal one text box for the full-screen thing
            }

            static char[] _buf = new char[1024];

            public void Render()
            {
                resources.lineMaterial.SetPass(0);
                Graphics.DrawProceduralNow(MeshTopology.Lines, m_NumLinesToDraw, 1);
                resources.textMaterial.SetPass(0);
                Graphics.DrawProceduralNow(MeshTopology.Triangles, m_NumTextBoxesToDraw * 6, 1);
                resources.graphMaterial.SetPass(0);
                Graphics.DrawProceduralNow(MeshTopology.Triangles, m_NumGraphsToDraw * 6, 1);
            }

            int m_NumTextBoxesToDraw = 0;
            int m_NumGraphsToDraw = 0;
            int m_NumLinesToDraw = 0;

            ComputeBuffer m_GraphSampleBuffer; // one big 1D array of floats
            ComputeBuffer m_GraphInstanceBuffer; // one thing for each graph to display
            ComputeBuffer m_TextCellBuffer; // one big 2D array of character cells
            ComputeBuffer m_TextInstanceBuffer; // one thing for each text box to display
            ComputeBuffer m_LineVertexBuffer; // one big 1D array of line vertex positions.

            ComputeBuffer m_ColorBuffer;

            public uint[] molokai = new uint[16]
            {
                0x181915, 0x7a0137, 0x498702, 0xc5bb63,
                0x3465a4, 0x75507b, 0x4194b0, 0xd3d7cf,
                0x45473c, 0xe00265, 0x6dca04, 0xe6db74,
                0x1d71ca, 0xaf5fff, 0x5cd1f8, 0xffffff,
            };

            public uint[] windows = new uint[16]
            {
                0x000000, 0x800000, 0x008000, 0x808000,
                0x000080, 0x800080, 0x008080, 0xc0c0c0,
                0x808080, 0xff0000, 0x00ff00, 0xffff00,
                0x0000ff, 0xff00ff, 0x00ffff, 0xffffff,
            };

            public void SetPalette(uint[] source)
            {
                for (int i = 0; i < m_Unmanaged.m_ColorData.Length; ++i)
                {
                    uint r = (source[i] >> 0) & 0xff;
                    uint g = (source[i] >> 8) & 0xff;
                    uint b = (source[i] >> 16) & 0xff;
                    m_Unmanaged.m_ColorData[i] = new Vector4(r, g, b, 255) / 255.0f;
                }
            }

            private static Managed _instance;

            public static void Initialize()
            {
                _instance = new Managed();
                _instance.Init();
            }

            public static void DoShutdown()
            {
                if (_instance != null)
                    _instance.Shutdown();
            }

            public static Managed instance { get { return _instance; } }

            void _Render(CameraType cameraType, CommandBuffer cmd)
            {
                if (cameraType != CameraType.Game)
                    return;
                
                cmd.DrawProcedural(Matrix4x4.identity, resources.textMaterial, 0, MeshTopology.Triangles, m_NumTextBoxesToDraw * 6, 1);
                cmd.DrawProcedural(Matrix4x4.identity, resources.graphMaterial, 0, MeshTopology.Triangles, m_NumGraphsToDraw * 6, 1);
            }

            void _Render3D(CameraType cameraType, CommandBuffer cmd)
            {
                cmd.DrawProcedural(Matrix4x4.identity, resources.lineMaterial, 0, MeshTopology.Lines, m_NumLinesToDraw, 1);
            }

            public static bool ShouldShow()
            {
                if (showOverlay == null)
                    return false;
                return showOverlay.IntValue != 0;
            }

            public static void Render(CameraType cameraType, CommandBuffer cmd)
            {
                if(!ShouldShow())
                    return;
                instance?._Render(cameraType, cmd);
            }

            public static void Render3D(CameraType cameraType, CommandBuffer cmd)
            {
                if(!ShouldShow())
                    return;
                instance?._Render3D(cameraType, cmd);
            }

        }

    }
}
