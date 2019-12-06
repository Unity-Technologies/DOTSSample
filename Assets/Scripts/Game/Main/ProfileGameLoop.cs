using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
//using Unity.PerformanceBenchmarking;
//using Unity.PerformanceBenchmarking.Data;
//using Unity.PerformanceBenchmarking.Measurements;
using UnityEngine;
using UnityEngine.Profiling;
using Debug = UnityEngine.Debug;

#if false

public class ProfileGameLoop : Game.IGameLoop
{
    StateMachine<State> m_StateMachine;
    string m_LevelName;
    string m_ScenarioName;

    public bool Init(string[] args)
    {
        if (args.Length < 2)
            return false;

        m_LevelName = args[0];
        m_ScenarioName = args[1];

        PerformanceTest.StartRun();
        PerformanceTest.StartTest("A2 " + m_LevelName + " " + m_ScenarioName);
        m_StartTicks = Stopwatch.GetTimestamp();

        m_StateMachine = new StateMachine<State>();
        m_StateMachine.Add(State.Loading, null, UpdateLoadingState, null);
        m_StateMachine.Add(State.Active, EnterActiveState, UpdateActiveState, LeaveActiveState);

        Console.SetOpen(false);

        if (m_LevelName != ".")
        {
            Game.game.levelManager.LoadLevel(args[0]);
            m_StateMachine.SwitchTo(State.Loading);
        }
        else
        {
            m_StateMachine.SwitchTo(State.Active);
        }

        RenderSettings.rVSync.Value = "0";

        return true;
    }

    int m_Framecount;
    bool m_LoadingDone;
    bool m_ProfilerSamplingEnabled;
    float m_FirstUpdateTime;
    long m_StartTicks;
    float m_NextCpuProfileTime;
    double m_LastCpuUsage;
    double m_LastCpuUsageUser;
    double m_UserUsagePct;
    double m_TotalUsagePct;
    ProfilerScenario m_CurrentScenario;
    List<ProfilerMeasurement> m_ProfilerMeasurements;

    public void Update()
    {
        m_Framecount++;
        m_StateMachine.Update();

        if (!m_LoadingDone)
        {
            if (m_StateMachine.CurrentState() == State.Active)
            {
                m_FirstUpdateTime = Time.time;
                var elapsedTicks = Stopwatch.GetTimestamp() - m_StartTicks;
                Measure.Custom(new SampleGroupDefinition("Loading"), 0,
                    TimeSpan.FromTicks(elapsedTicks).TotalMilliseconds);

                EnableProfiler();

                m_LoadingDone = true;
            }
            return;
        }

        if (!m_ProfilerSamplingEnabled)
        {
            m_ProfilerSamplingEnabled = true;
            return;
        }

        Measure.Custom(new SampleGroupDefinition("FrameTime"), Time.time, Time.unscaledDeltaTime * 1000f);
        UpdateCpuUsageMeasurements();
        Measure.Custom(new SampleGroupDefinition("TotalCpuUsage", SampleUnits.Percentage), Time.time, m_TotalUsagePct);
        Measure.Custom(new SampleGroupDefinition("UserCpuUsage", SampleUnits.Percentage), Time.time, m_UserUsagePct);

        if (Time.time - m_FirstUpdateTime > m_CurrentScenario.CaptureTime)
        {
            GameDebug.Log("Capture time reached");
            Measure.Custom(new SampleGroupDefinition("FrameCount", SampleUnits.None), Time.time, m_Framecount);
            ReportData();
            Application.Quit();
        }
    }
    
    public void FixedUpdate()
    {
    }

    public void LateUpdate()
    {
    }

    public void Shutdown()
    {
    }


    void EnableProfiler()
    {
        if (Debug.isDebugBuild)
        {
            var filename = "A2 " + m_LevelName + " " + m_ScenarioName + " " +
                           DateTime.Now.ToString("yyyy-MM-dd-HH-mm-ss");
            var outputFile = Application.dataPath + "/" + filename + ".raw";
            Profiler.logFile = outputFile;
            Profiler.enableBinaryLog = true;
            Profiler.enabled = true;

            m_ProfilerMeasurements = new List<ProfilerMeasurement>();
            m_ProfilerMeasurements.Add(Measure.ProfilerMarkers(new SampleGroupDefinition("GC.Collect")));

            m_ProfilerMeasurements.Add(Measure.ProfilerMarkers(new SampleGroupDefinition("PlayerLoop")));
            m_ProfilerMeasurements.Add(Measure.ProfilerMarkers(new SampleGroupDefinition("Render Thread")));

            m_ProfilerMeasurements.Add(Measure.ProfilerMarkers(new SampleGroupDefinition("WaitForJobGroupID")));

            m_ProfilerMeasurements.Add(Measure.ProfilerMarkers(new SampleGroupDefinition("EarlyUpdate.UpdateTextureStreamingManager")));
            m_ProfilerMeasurements.Add(Measure.ProfilerMarkers(new SampleGroupDefinition("FixedUpdate.PhysicsFixedUpdate")));
            m_ProfilerMeasurements.Add(Measure.ProfilerMarkers(new SampleGroupDefinition("Update.ScriptRunBehaviourUpdate")));
            m_ProfilerMeasurements.Add(Measure.ProfilerMarkers(new SampleGroupDefinition("Update.DirectorUpdate")));
            m_ProfilerMeasurements.Add(Measure.ProfilerMarkers(new SampleGroupDefinition("SimulationSystemGroup")));
            m_ProfilerMeasurements.Add(Measure.ProfilerMarkers(new SampleGroupDefinition("PreLateUpdate.DirectorUpdateAnimationBegin")));
            m_ProfilerMeasurements.Add(Measure.ProfilerMarkers(new SampleGroupDefinition("PreLateUpdate.DirectorUpdateAnimationEnd")));
            m_ProfilerMeasurements.Add(Measure.ProfilerMarkers(new SampleGroupDefinition("PreLateUpdate.ScriptRunBehaviourLateUpdate")));
            m_ProfilerMeasurements.Add(Measure.ProfilerMarkers(new SampleGroupDefinition("PostLateUpdate.VFXUpdate")));
            m_ProfilerMeasurements.Add(Measure.ProfilerMarkers(new SampleGroupDefinition("PostLateUpdate.UpdateAllRenderers")));
            m_ProfilerMeasurements.Add(Measure.ProfilerMarkers(new SampleGroupDefinition("PostLateUpdate.FinishFrameRendering")));
            m_ProfilerMeasurements.Add(Measure.ProfilerMarkers(new SampleGroupDefinition("PostLateUpdate.PresentAfterDraw")));

            m_ProfilerMeasurements.Add(Measure.ProfilerMarkers(new SampleGroupDefinition("HDRenderPipeline::Render " + m_ScenarioName)));
            m_ProfilerMeasurements.Add(Measure.ProfilerMarkers(new SampleGroupDefinition("Gfx.PresentFrame")));

            m_ProfilerMeasurements.Add(Measure.ProfilerMarkers(new SampleGroupDefinition("GBuffer")));
            m_ProfilerMeasurements.Add(Measure.ProfilerMarkers(new SampleGroupDefinition("Render shadows")));
        }
    }

    void UpdateCpuUsageMeasurements()
    {
        if (Time.time > m_NextCpuProfileTime)
        {
            const float interval = 5.0f;
            m_NextCpuProfileTime = Time.time + interval;
            var process = Process.GetCurrentProcess();
            var user = process.UserProcessorTime.TotalMilliseconds;
            var total = process.TotalProcessorTime.TotalMilliseconds;
            m_UserUsagePct = (float)(user - m_LastCpuUsageUser) / 10.0f / interval;
            m_TotalUsagePct = (float)(total - m_LastCpuUsage) / 10.0f / interval;
            m_LastCpuUsage = total;
            m_LastCpuUsageUser = user;
        }
    }

    void DisableProfiler()
    {
        if (Debug.isDebugBuild)
        {
            foreach (var profilerMeasurement in m_ProfilerMeasurements)
            {
                profilerMeasurement.Dispose();
            }
            Profiler.enableBinaryLog = false;
            Profiler.enabled = false;
        }
    }

    void LogResults(PerformanceTestRun run)
    {
        foreach (var result in run.Results)
        {
            GameDebug.Log(result.ToString());
        }
    }

    void ReportData()
    {
        DisableProfiler();

        PerformanceTest.EndTest();
        var run = PerformanceTest.EndRun();
        
        LogResults(run);

        var filename = "A2 " + m_LevelName + " " + m_ScenarioName + " " + DateTime.Now.ToString("yyyy-MM-dd-HH-mm-ss");
        var outputFile = Application.dataPath + "/" + filename + ".json";
        GameDebug.Log("Reporting data to: " + outputFile);
        var json = JsonUtility.ToJson(run);
        File.WriteAllText(outputFile, json);
    }

    void UpdateLoadingState()
    {
        if (Game.game.levelManager.IsCurrentLevelLoaded())
            m_StateMachine.SwitchTo(State.Active);
    }

    void EnterActiveState()
    {
        var scenarios = Resources.FindObjectsOfTypeAll<ProfilerScenario>();
        foreach (var scenario in scenarios)
        {
            if (System.String.Equals(scenario.name, m_ScenarioName, System.StringComparison.OrdinalIgnoreCase))
            {
                GameDebug.Log("Profiling scenario: " + scenario.name);
                m_CurrentScenario = scenario;
                scenario.gameObject.SetActive(true);
                break;
            }
        }
    }

    void UpdateActiveState()
    {
    }

    void LeaveActiveState()
    {
    }

    enum State
    {
        Loading,
        Active
    }
}
#endif
