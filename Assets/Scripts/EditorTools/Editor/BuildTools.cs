using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEditor;
using System.Threading;
using Unity.Sample.Core;
using UnityEditorInternal;

public static class BuildTools
{
    public static void CopyDirectory(string SourcePath, string DestinationPath)
    {
        //Create all of the directories
        foreach (string dirPath in Directory.GetDirectories(SourcePath, "*",
            SearchOption.AllDirectories))
            Directory.CreateDirectory(dirPath.Replace(SourcePath, DestinationPath));

        //Copy all the files & Replaces any files with the same name
        foreach (string newPath in Directory.GetFiles(SourcePath, "*.*",
            SearchOption.AllDirectories))
            File.Copy(newPath, newPath.Replace(SourcePath, DestinationPath), true);
    }

    static UnityEditor.Build.Reporting.BuildReport BuildGame(string buildPath, string exeName, BuildTarget target,
        BuildOptions opts, string buildId, ScriptingImplementation scriptingImplementation, bool includeEditorMetadata = false)
    {
        var exePathName = buildPath + "/" + exeName;
        string fullBuildPath = Directory.GetCurrentDirectory() + "/" + buildPath;
        var levels = new List<string>
        {
            "Assets/Scenes/bootstrapper.unity",
            "Assets/Scenes/empty.unity"
        };

        // Add scenes referenced from levelinfo's
        foreach (var li in LoadLevelInfos())
            levels.Add(AssetDatabase.GetAssetPath(li.main_scene));

        Debug.Log("Levels in build:");
        foreach (var l in levels)
            Debug.Log(string.Format(" - {0}", l));

        Debug.Log("Building: " + exePathName);
        Directory.CreateDirectory(buildPath);

        if (scriptingImplementation == ScriptingImplementation.WinRTDotNET)
        {
            throw new Exception(string.Format("Unsupported scriptingImplementation {0}", scriptingImplementation));
        }

        MakeBuildFilesWritable(fullBuildPath);
        using (new MakeInstallationFilesWritableScope())
        {
            var il2cpp = scriptingImplementation == ScriptingImplementation.IL2CPP;

            var monoDirs = Directory.GetDirectories(fullBuildPath).Where(s => s.Contains("MonoBleedingEdge"));
            var il2cppDirs = Directory.GetDirectories(fullBuildPath)
                .Where(s => s.Contains("BackUpThisFolder_ButDontShipItWithYourGame"));
            var clearFolder = (il2cpp && monoDirs.Count() > 0) || (!il2cpp && il2cppDirs.Count() > 0);
            if (clearFolder)
            {
                Debug.Log(" deleting old folders ..");
                foreach (var file in Directory.GetFiles(fullBuildPath))
                    File.Delete(file);
                foreach (var dir in monoDirs)
                    Directory.Delete(dir, true);
                foreach (var dir in il2cppDirs)
                    Directory.Delete(dir, true);
                foreach (var dir in Directory.GetDirectories(fullBuildPath).Where(s => s.EndsWith("_Data")))
                    Directory.Delete(dir, true);
            }

            UnityEditor.PlayerSettings.SetScriptingBackend(BuildTargetGroup.Standalone, scriptingImplementation);

            if (il2cpp)
            {
                UnityEditor.PlayerSettings.SetIl2CppCompilerConfiguration(BuildTargetGroup.Standalone,
                    Il2CppCompilerConfiguration.Release);
            }

            if (includeEditorMetadata)
            {
                //PerformanceTest.SaveEditorInfo(opts, target);
            }

            Environment.SetEnvironmentVariable("BUILD_ID", buildId, EnvironmentVariableTarget.Process);
            Environment.SetEnvironmentVariable("BUILD_UNITY_VERSION", InternalEditorUtility.GetFullUnityVersion(),
                EnvironmentVariableTarget.Process);

            var time = DateTime.Now;
            GameDebug.Log("BuildPipeline.BuildPlayer started");
            var result = BuildPipeline.BuildPlayer(levels.ToArray(), exePathName, target, opts);
            GameDebug.Log("BuildPipeline.BuildPlayer ended. Duration:" + (DateTime.Now - time).TotalSeconds + "s");

            Environment.SetEnvironmentVariable("BUILD_ID", "", EnvironmentVariableTarget.Process);
            Environment.SetEnvironmentVariable("BUILD_UNITY_VERSION", "", EnvironmentVariableTarget.Process);

            Debug.Log(" ==== Build Done =====");

            var stepCount = result.steps.Count();
            Debug.Log(" Steps:" + stepCount);
            for (var i = 0; i < stepCount; i++)
            {
                var step = result.steps[i];
                Debug.Log("-- " + (i + 1) + "/" + stepCount + " " + step.name + " " + step.duration.Seconds + "s --");
                foreach (var msg in step.messages)
                    Debug.Log(msg.content);
            }

            return result;
        }
    }

    static void MakeBuildFilesWritable(string fullBuildPath)
    {
        // Set all files to be writeable (As Unity 2017.1 sets them to read only)
        string[] fileNames = Directory.GetFiles(fullBuildPath, "*.*", SearchOption.AllDirectories);
        foreach (var fileName in fileNames)
        {
            FileAttributes attributes = File.GetAttributes(fileName);
            attributes &= ~FileAttributes.ReadOnly;
            File.SetAttributes(fileName, attributes);
        }
    }

    class MakeInstallationFilesWritableScope : IDisposable
    {
        bool disposed;
        Dictionary<string, FileAttributes> originalFileAttributes = new Dictionary<string, FileAttributes>();

        public MakeInstallationFilesWritableScope()
        {
            /// Colossal hack to work around build postprocessing expecting everything to be writable in the unity
            /// installation, but if people have unity in p4 it will be readonly.
            string standalonePath;
            switch (Application.platform)
            {
                case RuntimePlatform.OSXEditor:
                    standalonePath = EditorApplication.applicationPath + "/Contents/PlaybackEngines/MacStandaloneSupport";
                    break;

                case RuntimePlatform.WindowsEditor:
                    standalonePath = EditorApplication.applicationPath.BeforeLast("/") + "/Data/PlaybackEngines/windowsstandalonesupport";
                    break;

                default:
                    Debug.Log("Unknown platform. Skipping file attributes modification of standalone players.");
                    return;
            }

            Debug.Log("Checking for read/only files in standalone players");
            if (Directory.Exists(standalonePath))
            {
                var files = Directory.GetFiles(standalonePath, "*.*", SearchOption.AllDirectories);
                foreach (var f in files)
                {
                    var attr = File.GetAttributes(f);
                    originalFileAttributes[f] = attr;
                    if ((attr & FileAttributes.ReadOnly) != 0)
                    {
                        attr = attr & ~FileAttributes.ReadOnly;
                        //Debug.Log("Setting " + f + " to read/write");
                        File.SetAttributes(f, attr);
                    }
                }
            }
            Debug.Log("Done.");
        }

        public void Dispose()
        {
            if (disposed)
                return;
            disposed = true;
            RestoreFileAttributes();
        }

        void RestoreFileAttributes()
        {
            // Restore file attributes to avoid "Can't clobber writable file" in Perforce when syncing new installation files
            Debug.Log("Restoring file attributes for files in standalone players");
            foreach (var item in originalFileAttributes)
            {
                var path = item.Key;
                var originalAttr = item.Value;
                if (File.GetAttributes(path) == originalAttr)
                    continue;

                //Debug.Log($"Restoring file attributes for {path}");
                File.SetAttributes(path, originalAttr);
            }
        }
    }

    public static List<LevelInfo> LoadLevelInfos()
    {
        return LoadAssetsOfType<LevelInfo>();
    }

    static void AddKeys(Dictionary<string, int> dictionary, string[] keys)
    {
        foreach (var key in keys)
        {
            if (dictionary.ContainsKey(key))
            {
                dictionary[key]++;
            }
            else
            {
                dictionary[key] = 1;
            }
        }
    }

    static List<T> LoadAssetsOfType<T>() where T : UnityEngine.Object
    {
        var result = new List<T>();
        var assets = AssetDatabase.FindAssets("t:" + typeof(T).Name);
        foreach (var a in assets)
        {
            var path = AssetDatabase.GUIDToAssetPath(a);
            result.Add(AssetDatabase.LoadAssetAtPath<T>(path));
        }
        return result;
    }

    static string GetBuildName()
    {
        var buildNumber = System.Environment.GetEnvironmentVariable("BUILD_NUMBER");
        if (buildNumber == null)
        {
            buildNumber = "Dev";
        }

        var changeSet = System.Environment.GetEnvironmentVariable("P4_CHANGELIST");
        if (changeSet == null)
        {
            changeSet = "0";
        }

        var now = System.DateTime.Now;
        var name = now.ToString("yyyyMMdd") + "." + buildNumber + "." + changeSet;
        return name;
    }

    static string GetLongBuildName(string executableNameNoExtension, BuildTarget target, string buildName)
    {
        return executableNameNoExtension + "_" + target.ToString() + "_" + buildName;
    }

    static string GetBuildPath(BuildTarget target, string executableNameNoExtension, string buildName, ScriptingImplementation scriptingImplementation /* = ScriptingImplementation.Mono2x*/)
    {
        return string.Format("Builds/{0}-{1}/{2}", target.ToString(), scriptingImplementation.ToString(), GetLongBuildName(executableNameNoExtension, target, buildName));
    }

    static string GetBuildFolderPath(BuildTarget target)
    {
        return "Builds/" + target.ToString();
    }

    [MenuItem("Assets/ResirializeAssets")]
    public static void ReserializeProject()
    {
        if (Selection.assetGUIDs.Length == 0)
            return;

        List<string> paths = new List<string>();
        foreach (var g in Selection.assetGUIDs)
            paths.Add(AssetDatabase.GUIDToAssetPath(g));

        if (EditorUtility.DisplayDialog("Reserialize " + paths.Count + " assets", "Do you want to reserialize " + paths.Count + " assets?", "Yes, I do!"))
        {
            foreach (var p in paths)
            {
                var a = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(p);
                EditorUtility.SetDirty(a);
            }
            AssetDatabase.SaveAssets();
        }
    }

    [MenuItem("A2/BuildSystem/Win64/OpenBuildFolder")]
    public static void OpenBuildFolder()
    {
        var target = BuildTarget.StandaloneWindows64;
        var buildPath = GetBuildFolderPath(target);
        if (Directory.Exists(buildPath))
        {
            Debug.Log("Opening " + buildPath);
            var p = new System.Diagnostics.Process();
            p.StartInfo = new System.Diagnostics.ProcessStartInfo("explorer.exe", Path.GetFullPath(buildPath));
            p.Start();
        }
        else
        {
            Debug.LogWarning("No build folder found here: " + buildPath);
        }
    }

    //TODO: convert this to use BuildDefintion
    [MenuItem("A2/BuildSystem/Win64/Deploy-Mono")]
    public static void Deploy()
    {
        Debug.Log("Window64 Deploying...");
        var target = BuildTarget.StandaloneWindows64;
        var buildName = GetBuildName();
        var buildPath = GetBuildPath(target, Application.productName, buildName, ScriptingImplementation.Mono2x);
        //string executableName = Application.productName + ".exe";

        var platform = target.ToString();
        var clientApi = "b5176262e35ba4aa8280a68aae7b0492";

        // TODO: Figure out if it's possible to initialize cloud / unityconnect here instead
        var projectId = CloudProjectSettings.projectId;
        var orgId = CloudProjectSettings.organizationId;
        var projectName = CloudProjectSettings.projectId;
        var accessToken = CloudProjectSettings.accessToken;

        var deploy = new ConnectedGames.Build.DeployTools(OnProgressUpdate, clientApi, projectId, orgId, projectName, accessToken);

        var dstPath = buildName + ".zip";

        Debug.Log("Starting upload src=" + buildPath + " platform=" + platform + " isClient=N/A" +
            " clientApi=" + clientApi + " projectId=" + projectId + " orgId=" + orgId +
            " projectName=" + projectName + " accessToken=" + accessToken);

        deploy.CompressAndUpload(buildPath, buildPath + "/" + dstPath, platform, buildName);
        while (!deploy.Done)
        {
            deploy.UpdateLoop();
            Thread.Sleep(100);
        }
    }

    private static void OnProgressUpdate(string fileName, double progress)
    {
        Debug.Log(fileName + ":" + progress);
    }

    public enum UserCfgType
    {
        PlayerConfig = 0,
        ServerConfig,
        None
    }

    public struct BuildDefinition
    {
        public BuildTarget Target;
        public ScriptingImplementation ScriptingImplementation;
        public BuildOptions BuildOptions;
        public bool IncludeEditorMetadata;
        public string ExecutableNameOverride;
        public string ExecutableExtension;
        public string BuildPathOverride;
        public string BuildNameOverride;
        public bool GenerateSteamBatFile;
        public UserCfgType UserCfgType;
        public bool SkipBuildBundles;
        public GameObject GameConfig;
    }

    static void GenerateServerScriptFor(BuildTarget target, string executableName, string buildPath)
    {
        var scriptPath = buildPath;
        string[] scriptContents;
        switch (target)
        {
            case BuildTarget.StandaloneWindows64:
            case BuildTarget.StandaloneWindows:
            {
                scriptContents = new[]
                {
                    "REM start game server on level_01",
                    executableName + " -nographics -batchmode -noboot +serve whitebox_arena_a +game.modename assault"
                };
                scriptPath += "/server.bat";
                break;
            }
            case BuildTarget.StandaloneLinux64:
            case BuildTarget.StandaloneOSX:
            {
                scriptContents = new[]
                {
                    "#!/bin/bash",
                    "# start game server on level_01",
                    "./" +executableName + " -nographics -batchmode -noboot +serve whitebox_arena_a +game.modename assault"
                };
                scriptPath += "/server.sh";
                break;
            }
            default:
            {
                throw new Exception("Don't know how to generate Server script for platform: " + target);
            }
        }

        File.WriteAllLines(scriptPath, scriptContents);
    }

    static void GeneratePlayerConfig(string buildPath)
    {
        Debug.Log(" Generating player config files...");

        Debug.Log("Writing config files");

        // Build empty user.cfg
        File.WriteAllLines(buildPath + "/user.cfg", new string[] { });
        Debug.Log("  user.cfg");

        // Build boot.cfg
        var bootCfg = new[]
        {
            "preview whitebox_arena_a"
            //"client",
            //"load level_menu"
        };
        File.WriteAllLines(buildPath + "/" + Game.k_BootConfigFilename, bootCfg);
        Debug.Log("  " + Game.k_BootConfigFilename);

        Debug.Log("Generating player config files done");
    }

    static void GenerateServerConfig(string buildPath)
    {
        Debug.Log("Writing server config files");

        // Build empty user.cfg
        File.WriteAllLines(buildPath + "/user.cfg", new string[] { });
        Debug.Log("  user.cfg");

        // Build boot.cfg
        var bootCfg = new[]
        {
            "game.modename assault",
            "serve whitebox_arena_a"
        };
        File.WriteAllLines(buildPath + "/" + Game.k_BootConfigFilename, bootCfg);
        Debug.Log("  " + Game.k_BootConfigFilename);

        Debug.Log("Writing server config files done");
    }

    static void GenerateSteamBatFile(string zipName, string buildName, string buildPath)
    {
        Debug.Log("Writing steam upload bat file.");
        var steamBat = new string[]
        {
            @"@echo off",
            @"color",
            @"echo Verifying Steam SDK...",
            @"if not exist c:\steam\sdk\tools\ContentBuilder (",
            @"    echo Failed. Steam SDK must be installed at c:\steam",
            @"    goto :err",
            @")",
            @"echo OK. Found SDK at c:\steam",
            @"echo Looking for zipped game...",
            @"if not exist " + zipName + " (",
            @"    echo Failed. Did not locate zip: " + zipName,
            @"    goto :err",
            @")",
            @"echo OK. Found game at " + zipName,
            @"echo Removing old stage area",
            @"rmdir /s /q c:\steam\stage",
            @"echo Extracting build to staging area",
            @"""c:\Program Files\7-Zip\7z.exe"" x " + zipName + @" -oc:\steam\stage",
            @" c:",
            @" cd c:\steam\sdk\tools\ContentBuilder",
            @"set /p steamuser=""Enter steam user:""",
            @"builder\steamcmd.exe +login %steamuser% +run_app_build_http ..\scripts\app_build_1147610.vdf +quit",
            @"echo All done!",
            @"goto :ok",
            @":err",
            @"color 4f",
            @"goto :done",
            @":ok",
            @"color 2f",
            @":done",
            @"pause"
        };
        var steamBatName = "steam_upload_" + buildName + ".bat";
        File.WriteAllLines(buildPath + "/../" + steamBatName, steamBat);
        Debug.Log("  " + steamBatName);
    }

    public static void CreateBuildForDefinition(BuildDefinition buildDefinition)
    {
        Debug.Log(buildDefinition.Target.ToString() + " build started. (" + buildDefinition.ScriptingImplementation + ")");
        var buildName = GetBuildName();
        if (!string.IsNullOrWhiteSpace(buildDefinition.BuildNameOverride))
            buildName = buildDefinition.BuildNameOverride;

            var executableName = Application.productName;
        if (!string.IsNullOrWhiteSpace(buildDefinition.ExecutableNameOverride))
            executableName = buildDefinition.ExecutableNameOverride;

        var executableNameNoExtenstion = executableName;

        var buildPath = GetBuildPath(buildDefinition.Target, executableNameNoExtenstion, buildName, buildDefinition.ScriptingImplementation);
        if (!string.IsNullOrWhiteSpace(buildDefinition.BuildPathOverride))
            buildPath = buildDefinition.BuildPathOverride;

        Directory.CreateDirectory(buildPath);

        if (!string.IsNullOrWhiteSpace(buildDefinition.ExecutableExtension))
            executableName += buildDefinition.ExecutableExtension;


        var res = BuildGame(buildPath, executableName, buildDefinition.Target,
                            buildDefinition.BuildOptions, buildName, buildDefinition.ScriptingImplementation, buildDefinition.IncludeEditorMetadata);

        if (!res)
            throw new Exception("BuildPipeline.BuildPlayer failed");
        if (res.summary.result != UnityEditor.Build.Reporting.BuildResult.Succeeded)
            throw new Exception("BuildPipeline.BuildPlayer failed: " + res.ToString());

        Debug.Log(buildDefinition.Target.ToString() + " build completed...");
    }

    static void PostProcessByDefinition(BuildDefinition buildDefinition)
    {
        Debug.Log(buildDefinition.Target + " build postprocessing...");
        var target = buildDefinition.Target;
        var buildName = GetBuildName();
        if (!string.IsNullOrWhiteSpace(buildDefinition.BuildNameOverride))
            buildName = buildDefinition.BuildNameOverride;

        var executableName = Application.productName;
        if (!string.IsNullOrWhiteSpace(buildDefinition.ExecutableNameOverride))
            executableName = buildDefinition.ExecutableNameOverride;

        var executableNameNoExtenstion = executableName;

        var buildPath = GetBuildPath(target, executableNameNoExtenstion, buildName, buildDefinition.ScriptingImplementation);
        if (!string.IsNullOrWhiteSpace(buildDefinition.BuildPathOverride))
            buildPath = buildDefinition.BuildPathOverride;

        if (!string.IsNullOrWhiteSpace(buildDefinition.ExecutableExtension))
            executableName += buildDefinition.ExecutableExtension;

        if (!Directory.Exists(buildPath))
        {
            throw new Exception("No build here: " + buildPath);
        }

        switch (buildDefinition.UserCfgType)
        {
            case UserCfgType.None:
                break;
            case UserCfgType.PlayerConfig:
                GeneratePlayerConfig(buildPath);
                GenerateServerScriptFor(target, executableName, buildPath);
                break;
            case UserCfgType.ServerConfig:
                GenerateServerConfig(buildPath);
                break;
            default:
                throw new Exception("Unhandled config type: " + buildDefinition.UserCfgType);
        }

        if (buildDefinition.GenerateSteamBatFile)
        {
            var zipName = GetLongBuildName(executableNameNoExtenstion, target, buildName) + ".zip";
            GenerateSteamBatFile(zipName, buildName, buildPath);
        }
    }
    static readonly GameObject A2_DefaultGameConfg = AssetDatabase.LoadAssetAtPath<GameObject>("Assets\\GameConfig_DotsShooter.prefab");

    static readonly BuildDefinition k_MacOSMono = new BuildDefinition
    {
        Target = BuildTarget.StandaloneOSX,
        ScriptingImplementation = ScriptingImplementation.Mono2x,
        ExecutableExtension = ".app",
        GameConfig = A2_DefaultGameConfg,
    };

    static readonly BuildDefinition k_Win64Mono = new BuildDefinition
    {
        Target = BuildTarget.StandaloneWindows64,
        ExecutableExtension = ".exe",
        ScriptingImplementation = ScriptingImplementation.Mono2x,
        GameConfig = A2_DefaultGameConfg,
        GenerateSteamBatFile = true,
    };

    static readonly BuildDefinition k_Win64IL2CPP = new BuildDefinition
    {
        Target = BuildTarget.StandaloneWindows64,
        ExecutableExtension = ".exe",
        ScriptingImplementation = ScriptingImplementation.IL2CPP,
        GameConfig = A2_DefaultGameConfg,
        GenerateSteamBatFile = true,
    };

    static readonly BuildDefinition k_Win64MonoProfilingRelease = new BuildDefinition
    {
        Target = BuildTarget.StandaloneWindows64,
        ExecutableExtension = ".exe",
        ScriptingImplementation = ScriptingImplementation.Mono2x,
        GameConfig = A2_DefaultGameConfg,
        GenerateSteamBatFile = true,
        IncludeEditorMetadata = true,
        UserCfgType = UserCfgType.None
    };

    static readonly BuildDefinition k_Win64MonoProfilingDevelopment = new BuildDefinition
    {
        Target = BuildTarget.StandaloneWindows64,
        ExecutableExtension = ".exe",
        ScriptingImplementation = ScriptingImplementation.Mono2x,
        GameConfig = A2_DefaultGameConfg,
        GenerateSteamBatFile = true,
        IncludeEditorMetadata = true,
        BuildOptions = BuildOptions.Development,
        UserCfgType = UserCfgType.None
    };

    static readonly BuildDefinition k_PS4Mono = new BuildDefinition
    {
        Target = BuildTarget.PS4,
        ExecutableExtension = ".exe",
        ScriptingImplementation = ScriptingImplementation.Mono2x,
        GameConfig = A2_DefaultGameConfg,
        UserCfgType = UserCfgType.None
    };

    static readonly BuildDefinition k_Linux64Server = new BuildDefinition
    {
        Target = BuildTarget.StandaloneLinux64,
        ScriptingImplementation = ScriptingImplementation.Mono2x,
        GameConfig = A2_DefaultGameConfg,
        ExecutableNameOverride = "server-linux.x86_64",
        UserCfgType = UserCfgType.ServerConfig,
        BuildOptions = BuildOptions.EnableHeadlessMode,
    };

    static readonly BuildDefinition k_Linux64ServerOnlyExecutable = new BuildDefinition
    {
        Target = BuildTarget.StandaloneLinux64,
        ScriptingImplementation = ScriptingImplementation.Mono2x,
        GameConfig = A2_DefaultGameConfg,
        ExecutableNameOverride = "server-linux.x86_64",
        UserCfgType = UserCfgType.ServerConfig,
        BuildOptions = BuildOptions.EnableHeadlessMode,
        SkipBuildBundles = true,
    };

    static readonly BuildDefinition k_Linux64 = new BuildDefinition
    {
        Target = BuildTarget.StandaloneLinux64,
        ScriptingImplementation = ScriptingImplementation.Mono2x,
        GameConfig = A2_DefaultGameConfg,
        UserCfgType = UserCfgType.ServerConfig,
    };

    [MenuItem("A2/BuildSystem/MacOS/CreateBuildMacOS")]
    public static void CreateBuildMacOS()
    {
        CreateBuildForDefinition(k_MacOSMono);
        PostProcessByDefinition(k_MacOSMono);
    }

    [MenuItem("A2/BuildSystem/Win64/CreateBuildWindows64")]
    public static void CreateBuildWindows64()
    {
        CreateBuildForDefinition(k_Win64Mono);
        PostProcessByDefinition(k_Win64Mono);
    }

    [MenuItem("A2/BuildSystem/Win64/CreateBuildWindows64-IL2CPP")]
    public static void CreateBuildWindows64IL2CPP()
    {
        CreateBuildForDefinition(k_Win64IL2CPP);
        PostProcessByDefinition(k_Win64IL2CPP);
    }

    [MenuItem("A2/BuildSystem/Win64/PostProcessIL2CPP")]
    public static void PostProcessWindows64Il2CPP()
    {
        PostProcessByDefinition(k_Win64IL2CPP);
    }

    [MenuItem("A2/BuildSystem/Win64/PostProcess")]
    public static void PostProcessWindows64Mono()
    {
        PostProcessByDefinition(k_Win64Mono);
    }

    [MenuItem("A2/BuildSystem/Win64/CreateProfilingReleaseBuildWindows64")]
    public static void CreateProfilingReleaseBuildWindows64()
    {
        CreateBuildForDefinition(k_Win64MonoProfilingRelease);
    }

    [MenuItem("A2/BuildSystem/Win64/CreateProfilingDevelopmentBuildWindows64")]
    public static void CreateProfilingDevelopmentBuildWindows64()
    {
        CreateBuildForDefinition(k_Win64MonoProfilingDevelopment);
    }

    [MenuItem("A2/BuildSystem/PS4/CreateBuildPS4")]
    public static void CreateBuildPS4()
    {
        CreateBuildForDefinition(k_PS4Mono);
    }

    [MenuItem("A2/BuildSystem/Linux64/CreateBuildLinux64")]
    public static void CreateBuildLinux64Mono()
    {
        CreateBuildForDefinition(k_Linux64);
        PostProcessByDefinition(k_Linux64);
    }

    [MenuItem("A2/BuildSystem/Linux64/PostProcessServer")]
    public static void PostProcessLinuxServer()
    {
        PostProcessByDefinition(k_Linux64Server);
    }

    [MenuItem("A2/BuildSystem/Linux64/CreateBuildLinux64Server")]
    public static void CreateBuildLinux64Server()
    {
        CreateBuildForDefinition(k_Linux64Server);
        PostProcessByDefinition(k_Linux64Server);
    }

    // This is just a little convenience for when iterating on Linux specific code
    // Build a full build and then use this to just build the executable into the same build folder.
    [MenuItem("A2/BuildSystem/Linux64/CreateBuildLinux64_OnlyExecutable")]
    public static void CreateBuildLinux64_OnlyExecutable()
    {
        CreateBuildForDefinition(k_Linux64ServerOnlyExecutable);
    }
}
