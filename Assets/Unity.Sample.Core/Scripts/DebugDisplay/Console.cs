using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine;
using Unity.DebugDisplay;

namespace Unity.DebugDisplay.NewConsoleThatYouShouldNotUseYet
{

    public class Console
    {
        const int k_HistorySize = 1024; // lines of history
        const int k_BufferSize = 80 * 1024; // arbitrarily set to 1000 scroll back lines at 80
        const int k_InputBufferSize = 512;

        int m_Width;
        int m_Height;
        int m_NumLines;
        int m_LastLine;
        int m_LastColumn;
        int m_LastVisibleLine;
        float m_ConsoleFoldout;
        float m_ConsoleFoldoutDest;

        bool m_ConsoleOpen;

        NativeArray<Overlay.Text.Cell> m_InputFieldBuffer;

        string InputFieldBufferToString()
        {
            string s = "";
            for (int i = 0; i < m_InputFieldLength; ++i)
            {
                if (m_InputFieldBuffer[i].side == false)
                    s = s + (char) m_InputFieldBuffer[i].unicode;
            }

            return s;
        }

        void InputFieldBufferFromString(string s)
        {
            m_InputFieldLength = 0;
            for (int i = 0; i < s.Length; ++i)
            {
                Overlay.Text.Cell t = new Overlay.Text.Cell {unicode = s[i]};
                m_InputFieldBuffer[m_InputFieldLength++] = t;
                if (Overlay.Managed.instance.m_Unmanaged.m_Text.isWide(s[i]))
                {
                    t.side = true;
                    m_InputFieldBuffer[m_InputFieldLength++] = t;
                }
            }
        }

        int m_CursorPos = 0;
        int m_InputFieldLength = 0;

        string[] m_History = new string[k_HistorySize];
        int m_HistoryDisplayIndex = 0;
        int m_HistoryNextIndex = 0;

        Overlay.Color m_BackgroundColor = Overlay.Color.Black;
        Overlay.Color m_TextColor = Overlay.Color.BrightGreen;
        Overlay.Color m_CursorCol = Overlay.Color.Green;

        NativeArray<Overlay.Text.Cell> m_ConsoleBuffer;

        public Console()
        {
            m_ConsoleBuffer = new NativeArray<Overlay.Text.Cell>(k_BufferSize, Allocator.Persistent);
            m_InputFieldBuffer = new NativeArray<Overlay.Text.Cell>(k_InputBufferSize, Allocator.Persistent);
            AddCommand("help", CmdHelp, "Show available commands");
        }

        void CmdHelp(string[] args)
        {
            foreach (var c in m_Commands)
            {
                Write("  {0,-15} {1}\n", c.Key, m_CommandDescriptions[c.Key]);
            }
        }

        public void Init()
        {
            Init(null);
        }

        public void Init(Overlay.Managed debugOverlay)
        {
            m_DebugOverlay = debugOverlay != null ? debugOverlay : Overlay.Managed.instance;
            Resize(Overlay.Managed.CellsWide, Overlay.Managed.CellsTall);
            Clear();
        }

        public void Shutdown()
        {
            m_InputFieldBuffer.Dispose();
            m_ConsoleBuffer.Dispose();
        }

        public delegate void CommandDelegate(string[] args);

        Dictionary<string, CommandDelegate> m_Commands = new Dictionary<string, CommandDelegate>();
        Dictionary<string, string> m_CommandDescriptions = new Dictionary<string, string>();

        public void AddCommand(string name, CommandDelegate callback, string description)
        {
            if (m_Commands.ContainsKey(name))
            {
                Write("Cannot add command {0} twice", name);
                return;
            }

            m_Commands.Add(name, callback);
            m_CommandDescriptions.Add(name, description);
        }

        public void Resize(int width, int height)
        {
            m_Width = width;
            m_Height = height;
            m_NumLines = m_ConsoleBuffer.Length / m_Width;

            m_LastLine = m_Height - 1;
            m_LastVisibleLine = m_Height - 1;
            m_LastColumn = 0;

            // TODO: copy old text to resized console
        }

        public unsafe void Clear()
        {
            UnsafeUtility.MemClear(m_ConsoleBuffer.GetUnsafePtr(),
                m_ConsoleBuffer.Length * UnsafeUtility.SizeOf<Overlay.Text.Cell>());
            m_LastColumn = 0;
        }

        public void Show(float shown)
        {
            m_ConsoleFoldoutDest = shown;
        }

        public void TickUpdate()
        {
            if (Input.GetKeyDown(KeyCode.F12))
            {
                m_ConsoleOpen = !m_ConsoleOpen;
                m_ConsoleFoldoutDest = m_ConsoleOpen ? 1.0f : 0.0f;
                Show(m_ConsoleFoldoutDest);
            }

            if (!m_ConsoleOpen)
                return;

            Scroll((int) Input.mouseScrollDelta.y);
            if (Input.anyKey)
            {
                if (Input.GetKeyDown(KeyCode.LeftArrow) && m_CursorPos > 0)
                    m_CursorPos--;
                else if (Input.GetKeyDown(KeyCode.RightArrow) && m_CursorPos < m_InputFieldLength)
                    m_CursorPos++;
                else if (Input.GetKeyDown(KeyCode.Home) ||
                         (Input.GetKeyDown(KeyCode.A) &&
                          (Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl))))
                    m_CursorPos = 0;
                else if (Input.GetKeyDown(KeyCode.End) ||
                         (Input.GetKeyDown(KeyCode.E) &&
                          (Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl))))
                    m_CursorPos = m_InputFieldLength;
                else if (Input.GetKeyDown(KeyCode.Tab))
                    TabComplete();
                else if (Input.GetKeyDown(KeyCode.UpArrow))
                    HistoryPrev();
                else if (Input.GetKeyDown(KeyCode.DownArrow))
                    HistoryNext();
                else
                {
                    // TODO replace with garbage free alternative (perhaps impossible until new input system?)
                    var inputString = Input.inputString;
                    for (var i = 0; i < inputString.Length; i++)
                    {
                        var ch = inputString[i];
                        if (ch == '\b')
                            Backspace();
                        else if (ch == '\n' || ch == '\r')
                        {
                            var s = InputFieldBufferToString();
                            HistoryStore(s);
                            ExecuteCommand(s);
                            m_InputFieldLength = 0;
                            m_CursorPos = 0;
                        }
                        else
                            Type(ch);
                    }
                }
            }
        }

        void HistoryPrev()
        {
            if (m_HistoryDisplayIndex == 0 || m_HistoryNextIndex - m_HistoryDisplayIndex >= m_History.Length - 1)
                return;

            if (m_HistoryDisplayIndex == m_HistoryNextIndex)
                m_History[m_HistoryNextIndex % m_History.Length] = InputFieldBufferToString();

            m_HistoryDisplayIndex--;

            var s = m_History[m_HistoryDisplayIndex % m_History.Length];

            InputFieldBufferFromString(s);
            m_InputFieldLength = s.Length;
            m_CursorPos = s.Length;
        }

        void HistoryNext()
        {
            if (m_HistoryDisplayIndex == m_HistoryNextIndex)
                return;


            m_HistoryDisplayIndex++;

            var s = m_History[m_HistoryDisplayIndex % m_History.Length];

            InputFieldBufferFromString(s);
            m_InputFieldLength = s.Length;
            m_CursorPos = s.Length;
        }

        void HistoryStore(string cmd)
        {
            m_History[m_HistoryNextIndex % m_History.Length] = cmd;
            m_HistoryNextIndex++;
            m_HistoryDisplayIndex = m_HistoryNextIndex;
        }

        void ExecuteCommand(string command)
        {
            var splitCommand = command.Split(null as char[], System.StringSplitOptions.RemoveEmptyEntries);
            if (splitCommand.Length < 1)
                return;

            Write('>' + string.Join(" ", splitCommand) + '\n');
            var commandName = splitCommand[0].ToLower();

            CommandDelegate commandDelegate;

            if (m_Commands.TryGetValue(commandName, out commandDelegate))
            {
                var arguments = new string[splitCommand.Length - 1];
                System.Array.Copy(splitCommand, 1, arguments, 0, splitCommand.Length - 1);
                commandDelegate(arguments);
            }
            else
            {
                Write("Unknown command: {0}\n", splitCommand[0]);
            }
        }

        int blinking = 0;

        public void TickLateUpdate()
        {
            if (m_ConsoleOpen)
            {
                m_DebugOverlay.visibleLinesOfText = m_Height;

                var lastY = m_Height - 2;
                for (var y = lastY; y > 4; --y)
                {
                    int line = m_LastVisibleLine - (lastY - y);
                    var idx = (line % m_NumLines) * m_Width;
                    for (var x = 0; x < m_Width; x++)
                    {
                        var c = m_ConsoleBuffer[idx + x];
                        m_DebugOverlay.m_Unmanaged.m_Text.PutChar(x, y, c);
                    }
                }

                var blank = new Overlay.Text.Cell { };
                m_DebugOverlay.m_Unmanaged.m_Text.PutChars(0, m_Height - 1, blank, m_Width);

                var horizontalScroll = m_CursorPos - m_Width + 1;
                horizontalScroll = Mathf.Max(0, horizontalScroll);
                for (var i = horizontalScroll; i < m_InputFieldLength; i++)
                {
                    var c = m_InputFieldBuffer[i];
                    c.fg = m_TextColor;
                    c.bg = m_BackgroundColor;
                    if (c.unicode != '\0')
                        m_DebugOverlay.m_Unmanaged.m_Text.PutChar(i - horizontalScroll, m_Height - 1, c);
                }

                ++blinking;
                if (((blinking >> 3) & 1) == 1)
                {
                    var cursor = new Overlay.Text.Cell {unicode = '▒', fg = m_CursorCol, bg = m_BackgroundColor};
                    m_DebugOverlay.m_Unmanaged.m_Text.PutChar(m_CursorPos - horizontalScroll, m_Height - 1, cursor);
                }
            }
            else
            {
                m_DebugOverlay.visibleLinesOfText = 4;
            }
        }

        void NewLine()
        {
            // Only scroll view if at bottom
            if (m_LastVisibleLine == m_LastLine)
                m_LastVisibleLine++;

            m_LastLine++;
            m_LastColumn = 0;
        }

        void Scroll(int amount)
        {
            m_LastVisibleLine += amount;

            // Prevent going past last line
            if (m_LastVisibleLine > m_LastLine)
                m_LastVisibleLine = m_LastLine;

            if (m_LastVisibleLine < m_Height - 1)
                m_LastVisibleLine = m_Height - 1;

            // Prevent wrapping around
            if (m_LastVisibleLine < m_LastLine - m_NumLines + m_Height)
                m_LastVisibleLine = m_LastLine - m_NumLines + m_Height;
        }

        public void _Write(char[] buf, int length)
        {
            const string hexes = "0123456789ABCDEF";
            Overlay.Color col = Overlay.Color.Gray;
            for (int i = 0; i < length; i++)
            {
                if (buf[i] == '\n')
                {
                    NewLine();
                    continue;
                }

                // Parse color markup of the form ^AF7 -> color(0xAA, 0xFF, 0x77)
                if (buf[i] == '^' && i < length - 3)
                {
                    var r = (uint) hexes.IndexOf(buf[i + 1]);
                    var g = (uint) hexes.IndexOf(buf[i + 2]);
                    var b = (uint) hexes.IndexOf(buf[i + 3]);
                    var rgba = new Vector4(r, g, b, 15) / 15.0f;
                    col = Overlay.Managed.instance.m_Unmanaged.Quantize(rgba);
                    i += 3;
                    continue;
                }

                var idx = (m_LastLine % m_NumLines) * m_Width + m_LastColumn;
                var t = new Overlay.Text.Cell {unicode = buf[i], fg = col};
                m_ConsoleBuffer[idx] = t;
                m_LastColumn++;
                if (m_LastColumn >= m_Width)
                {
                    NewLine();
                }
            }
        }

        static char[] _buf = new char[1024];

        public void Write(string format)
        {
            var l = StringFormatter.Write(ref _buf, 0, format);
            _Write(_buf, l);
        }

        public void Write<T>(string format, T arg)
        {
            var l = StringFormatter.Write(ref _buf, 0, format, arg);
            _Write(_buf, l);
        }

        public void Write<T0, T1>(string format, T0 arg0, T1 arg1)
        {
            var l = StringFormatter.Write(ref _buf, 0, format, arg0, arg1);
            _Write(_buf, l);
        }

        public void Write<T0, T1, T2>(string format, T0 arg0, T1 arg1, T2 arg2)
        {
            var l = StringFormatter.Write(ref _buf, 0, format, arg0, arg1, arg2);
            _Write(_buf, l);
        }

        void Type(char c)
        {
            if (m_InputFieldLength >= m_InputFieldBuffer.Length)
                return;

            for (int i = 0; i < m_InputFieldLength - m_CursorPos; ++i)
                m_InputFieldBuffer[m_InputFieldLength - i] = m_InputFieldBuffer[m_InputFieldLength - 1 - i];
            m_InputFieldBuffer[m_CursorPos] = new Overlay.Text.Cell {unicode = c};
            m_CursorPos++;
            m_InputFieldLength++;
        }

        void Backspace()
        {
            if (m_CursorPos == 0)
                return;
            for (int i = 0; i < m_InputFieldLength - m_CursorPos; ++i)
                m_InputFieldBuffer[i + m_CursorPos - 1] = m_InputFieldBuffer[i + m_CursorPos];
            m_CursorPos--;
            m_InputFieldLength--;
        }

        void TabComplete()
        {
            string prefix = InputFieldBufferToString().Substring(0, m_CursorPos);

            // Look for possible tab completions
            List<string> matches = new List<string>();

            foreach (var c in m_Commands)
            {
                var name = c.Key;
                if (!name.StartsWith(prefix, true, null))
                    continue;
                matches.Add(name);
            }

            if (matches.Count == 0)
                return;

            // Look for longest common prefix
            int lcp = matches[0].Length;
            for (var i = 0; i < matches.Count - 1; i++)
            {
                lcp = Mathf.Min(lcp, CommonPrefix(matches[i], matches[i + 1]));
            }

            var bestMatch = matches[0].Substring(prefix.Length, lcp - prefix.Length);
            foreach (var c in bestMatch)
                Type(c);
            if (matches.Count > 1)
            {
                // write list of possible completions
                for (var i = 0; i < matches.Count; i++)
                    Write(" {0}\n", matches[i]);
            }

            if (matches.Count == 1)
                Type(' ');
        }

        // Returns length of largest common prefix of two strings
        static int CommonPrefix(string a, string b)
        {
            int minl = Mathf.Min(a.Length, b.Length);
            for (int i = 1; i <= minl; i++)
            {
                if (!a.StartsWith(b.Substring(0, i), true, null))
                    return i - 1;
            }

            return minl;
        }

        Overlay.Managed m_DebugOverlay;
    }
}