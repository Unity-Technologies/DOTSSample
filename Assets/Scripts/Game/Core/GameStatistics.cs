using UnityEngine;
using System.Collections;
using UnityEngine.Profiling;
using System;
using System.Collections.Generic;
using Unity.DebugDisplay;
using Unity.Entities;
using Unity.Sample.Core;

public class GameStatistics
{
    public int rtt;
    public float commandAge;

    private readonly int _no_frames = 128;

    private Overlay.Graph.Data.Reservation frameTimeData;
    private Overlay.Graph.Data.Reservation ticksPerFrameData0;
    private Overlay.Graph.Data.Reservation ticksPerFrameData1;

    public GameStatistics()
    {
        m_FrequencyMS = System.Diagnostics.Stopwatch.Frequency / 1000;
        m_StopWatch = new System.Diagnostics.Stopwatch();
        m_StopWatch.Start();
        m_LastFrameTicks = m_StopWatch.ElapsedTicks;

        m_GraphicsDeviceName = SystemInfo.graphicsDeviceName;

        Console.AddCommand("show.profilers", CmdShowProfilers, "Show available profilers.");

        frameTimeData = Overlay.Managed.instance.m_Unmanaged.m_GraphDataReservations.Reserve(_no_frames);
        ticksPerFrameData0 = Overlay.Managed.instance.m_Unmanaged.m_GraphDataReservations.Reserve(_no_frames);
        ticksPerFrameData1 = Overlay.Managed.instance.m_Unmanaged.m_GraphDataReservations.Reserve(_no_frames);
    }

    void CmdShowProfilers(string[] args)
    {
        var names = new List<string>();
        Sampler.GetNames(names);
        string search = args.Length > 0 ? args[0].ToLower() : null;
        for (var i = 0; i < names.Count; i++)
        {
            if (search == null || names[i].ToLower().Contains(search))
                Console.Write(names[i]);
        }
    }

    int m_LastWorldTick;

    void SnapTime()
    {
        long now = m_StopWatch.ElapsedTicks;
        long duration = now - m_LastFrameTicks;

        m_LastFrameTicks = now;

        float d = (float)duration / m_FrequencyMS;
        m_FrameDurationMS = m_FrameDurationMS * 0.9f + 0.1f * d;

        frameTimeData.SetValue(Time.frameCount, d);
    }

    public void TickLateUpdate()
    {
        SnapTime();
        if (showCompactStats.IntValue > 0)
        {
            DrawCompactStats();
        }
        if (showFPS.IntValue > 0)
        {
            DrawFPS();
        }
    }

    private const int kAverageFrameCount = 64;


    char[] buf = new char[256];
    void DrawCompactStats()
    {
        Overlay.Managed.Write(2, 0, "FPS:{0}", Mathf.RoundToInt(1000.0f / m_FrameDurationMS));
        if (rtt > 0)
            Overlay.Managed.Write(64, 0, "RTT:{0}", rtt);
        else
            Overlay.Managed.Write(64, 0, "RTT:---");
        Overlay.Managed.Write(32, 0, "CMD:{0:0.00}", commandAge);
    }

    void DrawFPS()
    {
        Overlay.Managed.Write(0, 1, "{0} FPS ({1:##.##} ms)", Mathf.RoundToInt(1000.0f / m_FrameDurationMS), m_FrameDurationMS);
        float minDuration = float.MaxValue;
        float maxDuration = float.MinValue;
        float sum = 0;
        for (var i = 0; i < _no_frames; i++)
        {
            var frametime = frameTimeData.GetValue(i);
            sum += frametime;
            if (frametime < minDuration) minDuration = frametime;
            if (frametime > maxDuration) maxDuration = frametime;
        }

        Overlay.Managed.Write(Overlay.Color.Green, 0, 2, "{0:##.##}", minDuration);
        Overlay.Managed.Write(Overlay.Color.Gray, 6, 2, "{0:##.##}", sum / _no_frames);
        Overlay.Managed.Write(Overlay.Color.Red, 12, 2, "{0:##.##}", maxDuration);

        Overlay.Managed.Write(0, 3, "Frame #: {0}", Time.frameCount);

        Overlay.Managed.Write(0, 4, m_GraphicsDeviceName);


        int y = 6;


        if (showFPS.IntValue < 3)
            return;

        y++;

        using (var graph = Overlay.Managed.instance.m_Unmanaged.m_GraphReservations.Reserve(2))
        {
            ticksPerFrameData0.CalcMinMaxMean(out var min0, out var max0, out var _); // lame and slow, remove once you have reasonable default min/max
            ticksPerFrameData1.CalcMinMaxMean(out var min1, out var max1, out var _); // lame and slow, remove once you have reasonable default min/max
            graph.AddGraph(0, y, 40, 4, new Overlay.Graph.Sample {
                data = ticksPerFrameData0.GetData(),
                color = Overlay.Color.Green,
                xMin = Time.frameCount - _no_frames,
                xMax = Time.frameCount,
                yMin = min0,
                yMax = max0
            },new Overlay.Graph.Sample {
                data = ticksPerFrameData1.GetData(),
                color = Overlay.Color.Gray,
                xMin = Time.frameCount - _no_frames,
                xMax = Time.frameCount,
                yMin = min1,
                yMax = max1
            });
            graph.AddGraph(0, y + 6, 40, 4, new Overlay.Graph.Sample {
                data = frameTimeData.GetData(),
                color = Overlay.Color.Red,
                xMin = Time.frameCount - _no_frames,
                xMax = Time.frameCount,
                yMin = 5,
                yMax = 100,
            });
        }

        if (World.AllWorlds.Count > 0)
        {
            var world = World.AllWorlds[0];
            var gameTimeSystem = world.GetExistingSystem<GameTimeSystem>();
            Overlay.Managed.Write(0, y + 8, "Tick: {0:##.#}", 1000.0f * gameTimeSystem.GetWorldTime().tickInterval);
        }
    }

    System.Diagnostics.Stopwatch m_StopWatch;
    long m_LastFrameTicks; // Ticks at start of last frame
    float m_FrameDurationMS;
    long m_FrequencyMS;
    string m_GraphicsDeviceName;


    [ConfigVar(Name = "show.fps", DefaultValue = "0", Description = "Set to value > 0 to see fps stats.")]
    public static ConfigVar showFPS;

    [ConfigVar(Name = "show.compactstats", DefaultValue = "1", Description = "Set to value > 0 to see compact stats.")]
    public static ConfigVar showCompactStats;
}
