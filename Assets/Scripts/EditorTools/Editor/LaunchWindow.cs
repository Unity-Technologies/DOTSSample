using System;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;
using System.IO;
using System.Linq;
using Unity.Sample.Core;
#if UNITY_EDITOR
using Unity.Build;
using Unity.Build.Common;
using UnityEditor;

public class LaunchWindow : EditorWindow
{
    private static Color c_RunColor = new Color(0.1f,0.9f,0.1f);
    private static Color c_StopColor = new Color(0.9f,0.2f,0.2f);
    private static Color c_BuildColor =  new Color(0.4f, 0.6f,0.8f);
    private static Color c_SelectColor = Color.yellow;


    [Serializable]
    internal class Entry
    {
        public bool runInEditor;
        public string name = "Entry name";
        public int count = 1;
        public bool selected;
        public bool showDetails;

        public string buildSettingsGUID;
        public string arguments;

        public BuildSettings GetBuildSettings()
        {
            var buildSettingsPath = AssetDatabase.GUIDToAssetPath(buildSettingsGUID);
            return AssetDatabase.LoadAssetAtPath<BuildSettings>(buildSettingsPath);
        }

        public void SetBuildSettings(BuildSettings settings)
        {
            var buildSettingsPath = AssetDatabase.GetAssetPath(settings);
            buildSettingsGUID = AssetDatabase.AssetPathToGUID(buildSettingsPath);
        }

        public bool GetBuildName(out string buildPath, out string buildName)
        {
            var buildSettings = GetBuildSettings();
            buildPath = "";
            buildName = "";
            if (buildSettings == null)
            {
                GameDebug.LogError("No buildsettings asset defined");
                return false;
            }

            var generalSettings = buildSettings.GetComponent<GeneralSettings>();
            if (generalSettings == null)
            {
                GameDebug.LogError("No GeneralSettings component for buildSetting");
                return false;
            }

            buildPath = Path.GetFullPath(buildSettings.GetOutputBuildDirectory());
            buildName = generalSettings.ProductName;

            return true;
        }
    }

    [Serializable]
    internal class Data
    {
        public List<Entry> entries = new List<Entry>();
    }

    [MenuItem("A2/Windows/LaunchWindow")]
    public static void ShowWindow()
    {
        GetWindow<LaunchWindow>(false, "Launch", true);
    }

    internal static void OpenAsset(LaunchWindowSettingsAsset config)
    {
        var win = GetWindow<LaunchWindow>();
        win.asset = config;
    }

    private void OnEnable()
    {
        LoadFromEditorPrefs();
        BuildManager.StatusEvent += StatusEvent;
    }

    private void StatusEvent()
    {
        Repaint();
    }


    void OnGUI()
    {
        var defaultGUIColor = GUI.color;
        var defaultGUIBackgrounColor = GUI.backgroundColor;

        EditorGUI.BeginChangeCheck();

        EditorGUILayout.BeginVertical();

        // Quick start buttons
        GUILayout.BeginHorizontal();
        {
            GUI.backgroundColor = c_BuildColor;
            if (GUILayout.Button("Build Selected"))
            {
                for (var i = 0; i < data.entries.Count; i++)
                {
                    var entry = data.entries[i];
                    if (!entry.selected)
                        continue;

                    QueueBuild(data.entries[i]);
                }
            }
            GUI.backgroundColor = defaultGUIBackgrounColor;

            GUI.backgroundColor = c_RunColor;
            if (GUILayout.Button("Start Selected"))
            {
                for (var i = 0; i < data.entries.Count; i++)
                {
                    var entry = data.entries[i];
                    if (!entry.selected)
                        continue;
                    StartEntry(data.entries[i]);
                }
            }
            GUI.backgroundColor = defaultGUIBackgrounColor;

            GUI.backgroundColor = c_StopColor;
            if (GUILayout.Button("Stop All"))
            {
                StopAll();
            }
            GUI.backgroundColor = defaultGUIBackgrounColor;

            if (GUILayout.Button("+", GUILayout.Width(30)))
            {
                data.entries.Add(new Entry());
            }
        }
        GUILayout.EndHorizontal();

        EditorGUI.BeginChangeCheck();
        var labelContent = new GUIContent("Configuration:", "None will save the configuration on this machine only (EditorPrefs)");
        var newAsset = EditorGUILayout.ObjectField(labelContent, m_Asset, typeof(LaunchWindowSettingsAsset), false) as LaunchWindowSettingsAsset;
        if (EditorGUI.EndChangeCheck())
        {
            asset = newAsset;
        }

        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

        // Draw entries
        int deleteEntry = -1;
        for (var i = 0; i < data.entries.Count; i++)
        {
            var entry = data.entries[i];
            //var style = "Box"; //  entry.runInEditor ? "selectionRect" : "Box";

            GUILayout.BeginHorizontal(GUILayout.ExpandHeight(false));
            {
                GUI.backgroundColor = entry.selected ? c_SelectColor : defaultGUIBackgrounColor;
                if (GUILayout.Button("S", GUILayout.Width(20)))
                    entry.selected = !entry.selected;
                GUI.backgroundColor = defaultGUIBackgrounColor;

                if (GUILayout.Button(entry.showDetails ? "^" : "v", GUILayout.Width(30)))
                    entry.showDetails = !entry.showDetails;

                entry.name = EditorGUILayout.TextField(entry.name);

                GUI.backgroundColor = c_BuildColor;
                if (GUILayout.Button("Build", GUILayout.Width(50)))
                {
                    QueueBuild(entry);
                    GUIUtility.ExitGUI();
                }
                GUI.backgroundColor = defaultGUIBackgrounColor;


                GUI.backgroundColor = c_RunColor;
                if (GUILayout.Button("Start", GUILayout.Width(50)))
                {
                    StartEntry(entry);
                }
                GUI.backgroundColor = defaultGUIBackgrounColor;

                entry.count = EditorGUILayout.IntField(entry.count, GUILayout.Width(30), GUILayout.ExpandWidth(false));

                GUI.backgroundColor = entry.runInEditor ? Color.yellow : GUI.backgroundColor;


                GUI.backgroundColor = defaultGUIBackgrounColor;

                var runInEditor = GUILayout.Toggle(entry.runInEditor, "Editor", new GUIStyle("Button"), GUILayout.Width(50));
                if (runInEditor != entry.runInEditor)
                {
                    for (var j = 0; j < data.entries.Count; j++)
                        data.entries[j].runInEditor = false;
                    entry.runInEditor = runInEditor;
                }

                if(GUILayout.Button("-", GUILayout.Width(30)))
                {
                    deleteEntry = i;
                }
            }
            GUILayout.EndHorizontal();

            if (entry.showDetails)
            {
                GUILayout.BeginHorizontal();

                GUILayout.Space(20);

                GUILayout.BeginVertical();

                EditorGUI.BeginChangeCheck();

                var buildSettings =
                    EditorGUILayout.ObjectField("Build settings",entry.GetBuildSettings(), typeof(BuildSettings), false, null) as BuildSettings;

                EditorGUILayout.LabelField("Command line args");
                entry.arguments = EditorGUILayout.TextArea(entry.arguments);


                if (EditorGUI.EndChangeCheck())
                {
                    entry.SetBuildSettings(buildSettings);

                    SaveSettings();
                }

                GUILayout.EndVertical();

                GUILayout.EndHorizontal();
            }
        }

        if(deleteEntry > -1)
        {
            data.entries.RemoveAt(deleteEntry);
            SaveSettings();
        }

        if (BuildManager.IsBuilding())
        {
            GUI.backgroundColor = c_BuildColor;
            var str = "Building: " + BuildManager.GetStatus();
            GUILayout.Label(str);
            GUI.backgroundColor = defaultGUIBackgrounColor;
        }

        GUILayout.FlexibleSpace();

        EditorGUILayout.EndScrollView();

        EditorGUILayout.EndVertical();



        if (EditorGUI.EndChangeCheck())
        {
            SaveSettings();
        }

        if (asset == null)
        {
            if (GUILayout.Button("Save configuration to asset..."))
            {
                SaveSettingsAs();
            }
        }
    }

    void SaveSettingsAs()
    {
        var path = EditorUtility.SaveFilePanelInProject("Save Launch configuration", "launchConfiguration", "asset", "Save configuration");
        if (string.IsNullOrEmpty(path))
            return;

        var settingsAsset = LaunchWindowSettingsAsset.Create(data);

        AssetDatabase.CreateAsset(settingsAsset, path);
        AssetDatabase.SaveAssets();

        asset = settingsAsset;
    }

    void SaveSettings()
    {
        if (asset == null)
        {
            var json = JsonUtility.ToJson(data);
            EditorPrefs.SetString(editorPrefName, json);
        }
        else
        {
            EditorUtility.SetDirty(asset);
        }
    }

    void LoadFromEditorPrefs()
    {
        var str = EditorPrefs.GetString(editorPrefName, "");
        if (str != "")
            m_DataFromEditorPrefs = JsonUtility.FromJson<Data>(str);
        else
            m_DataFromEditorPrefs = new Data();
    }

    void StartEntry(Entry entry)
    {
        var args = "";

        // Convert line breaks to space and remove commented out lines
        if (entry.arguments != null)
        {
            var lines = entry.arguments.Split(new String[] { "\r\n", "\n"  }, StringSplitOptions.RemoveEmptyEntries).ToList();
            lines.RemoveAll(str => str.Contains("//"));
            lines.ForEach(str => args += str + " ");
        }

//        var config = entry.GetConfig();
//        if (config != null)
//        {
//            var configPath = AssetDatabase.GetAssetPath(config);
//            var fullPath = Path.GetFullPath(configPath);
//            args += " +exec " + fullPath;
//        }

        int standaloneCount = entry.count;
        if (!Application.isPlaying && entry.runInEditor)
        {
            EditorLevelManager.StartGameInEditor(args);
            standaloneCount--;
        }


        string buildPath;
        string buildName;
        var result = entry.GetBuildName(out buildPath, out buildName);
        if (!result)
            return;
        var buildExe = buildName + ".exe";
        for (var i = 0; i < standaloneCount; i++)
        {
            RunBuild(buildPath, buildExe, args + " -title \"" + entry.name + "\"");
        }
    }

    static void RunBuild(string buildPath, string buildExe, string args)
    {
        GameDebug.Log("Starting " + buildPath + "/" + buildExe + " " + args);
        var process = new System.Diagnostics.Process();
        process.StartInfo.UseShellExecute = args.Contains("-batchmode");
        process.StartInfo.FileName = buildPath + "/" + buildExe;    // mogensh: for some reason we now need to specify project path
        process.StartInfo.Arguments = args;
        process.StartInfo.WorkingDirectory = buildPath;
        process.Start();
    }

    void QueueBuild(Entry entry)
    {
        var buildSettings = entry.GetBuildSettings();
        if(buildSettings == null)
            GameDebug.LogError("No build settings defined");

        BuildManager.QueueBuild(buildSettings);
        Repaint();
    }


    static string GetBuildExe(BuildTarget buildTarget)
    {
        switch (buildTarget)
        {
            case BuildTarget.PS4:
                return "AutoBuild/AutoBuild.bat";

            case BuildTarget.StandaloneOSX:
                return "AutoBuild.app/Contents/MacOS/A2";

            default:
                return "AutoBuild.exe";
        }
    }

    void StopAll()
    {
        EditorApplication.isPlaying = false;

        for (var i = 0; i < data.entries.Count; i++)
        {
            var entry = data.entries[i];

            var buildSettings = entry.GetBuildSettings();
            if (buildSettings == null)
                continue;

            string buildName;
            string buildPath;
            entry.GetBuildName(out buildPath, out buildName);

            var processName = buildName;

            var processes = Process.GetProcessesByName(processName);
            foreach (var process in processes)
            {
                if (process.HasExited)
                    continue;

                process.Kill();
            }
        }

    }

    internal LaunchWindowSettingsAsset asset
    {
        get => m_Asset;
        set
        {
            m_Asset = value;
            if (m_Asset == null)
            {
                LoadFromEditorPrefs();
            }
        }
    }

    internal Data data
    {
        get
        {
            if (m_Asset != null)
                return m_Asset.m_Data;
            return m_DataFromEditorPrefs;
        }

    }

    List<BuildSettings> m_BuildSettings = new List<BuildSettings>();
    int m_currentPickerWindow;

    LaunchWindowSettingsAsset m_Asset;
    const string editorPrefName = "LauchWindowData";
    Data m_DataFromEditorPrefs;
    Vector2 scrollPosition;
}

#endif
