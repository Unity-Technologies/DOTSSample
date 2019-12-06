using System;
using System.Collections.Generic;
using Unity.Build;
using Unity.Sample.Core;
using UnityEditor;
using UnityEngine;

[InitializeOnLoad]
public class BuildManager
{
    public delegate void StatusEventDelegate();

    public static StatusEventDelegate StatusEvent;


    private const string c_SessionStateKey = "BuildManagerBuildStack";

    [Serializable]
    class Data
    {
        public List<string> PendingBuilds = new List<string>();
    }

    static Data m_Data = new Data();
    private static BuildSettings m_currentBuildSettings;
    private static bool m_assembliesReloading;

    static BuildManager()
    {
        EditorApplication.update += Update;
        AssemblyReloadEvents.beforeAssemblyReload += OnBeforeAssemblyReload;
        AssemblyReloadEvents.afterAssemblyReload += OnAfterAssemblyReload;

        var str = SessionState.GetString(c_SessionStateKey, "");
        if (str != "")
        {
            m_Data = JsonUtility.FromJson<Data>(str);
            GameDebug.Log("restore. Count:" +m_Data.PendingBuilds.Count);
        }

    }

    public static void QueueBuild(BuildSettings buildSettings)
    {
        var path = AssetDatabase.GetAssetPath(buildSettings);

        if (m_Data.PendingBuilds.Contains(path))
            return;

        m_Data.PendingBuilds.Add(path);
    }


    public static bool IsBuilding()
    {
        return EditorApplication.isCompiling || m_Data.PendingBuilds.Count > 0 || m_currentBuildSettings != null ||
               m_assembliesReloading;
    }

    public static string GetStatus()
    {
        if (EditorApplication.isCompiling)
            return "Compiling";
        if (m_Data.PendingBuilds.Count > 0)
            return "Pending builds";
        if (m_currentBuildSettings != null)
            return "Building " + m_currentBuildSettings;
        if (m_assembliesReloading)
            return "Assebly reload";
        return "Idle";
    }

    private static void Update()
    {
        if (EditorApplication.isCompiling)
        {
//            GameDebug.Log("compiling...");
            return;
        }

        if (m_currentBuildSettings != null)
        {
            GameDebug.Log("############################## STARING BUILD : " + m_currentBuildSettings + " #########################");
            m_currentBuildSettings.Build();
            m_currentBuildSettings = null;
            SendStatusEvent();
            GameDebug.Log("############################## END BUILD ################################");
        }

        if (m_Data.PendingBuilds.Count > 0)
        {
            var dataPath = m_Data.PendingBuilds[0];
            m_Data.PendingBuilds.RemoveAt(0);

            // Store queue as it will get destroyed at asssmbly reload (after build or also when doing build?)
            var str = JsonUtility.ToJson(m_Data);
            SessionState.SetString(c_SessionStateKey,str);

            m_currentBuildSettings = AssetDatabase.LoadAssetAtPath<BuildSettings>(dataPath);

            SendStatusEvent();
            EditorApplication.QueuePlayerLoopUpdate();

        }
    }

    public static void OnBeforeAssemblyReload()
    {
        Debug.Log("Before Assembly Reload");
        m_assembliesReloading = true;
        SendStatusEvent();
    }

    public static void OnAfterAssemblyReload()
    {
        Debug.Log("After Assembly Reload");
        m_assembliesReloading = false;
        SendStatusEvent();
    }

    static void SendStatusEvent()
    {
        if (StatusEvent != null)
            StatusEvent();
    }
}
