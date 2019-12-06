using System;
using Unity.Build;
using Unity.Build.Common;
using UnityEditor;
using UnityEngine;

public static class BuildSettingsRunner
{
    static bool HasArg(string name)
    {
        var nameLowercase = name.ToLower();
        var args = Environment.GetCommandLineArgs();
        for (var i = 0; i < args.Length; i++)
        {
            if (args[i].ToLower() == nameLowercase)
            {
                return true;
            }
        }

        return false;
    }

    static bool TryGetArg(string name, out string value)
    {
        var nameLowercase = name.ToLower();
        var args = Environment.GetCommandLineArgs();
        for (var i = 0; i < args.Length; i++)
        {
            if (args[i].ToLower() == nameLowercase && args.Length > i + 1)
            {
                value = args[i + 1];
                return true;
            }
        }

        value = default;
        return false;
    }

    static bool RunBuildSettingsAtPath(string assetPath, string buildDir, bool enableIL2CPP)
    {
        var buildSettings = AssetDatabase.LoadAssetAtPath<BuildSettings>(assetPath);
        if (buildSettings == null)
        {
            Debug.LogError($"No build settings found at path {assetPath}");
            return false;
        }

        // Set build directory
        buildSettings.SetComponent<OutputBuildDirectory>(new OutputBuildDirectory
        {
            OutputDirectory = $"{buildDir}/{buildSettings.name}"
        });

        if (enableIL2CPP)
        {
            if (buildSettings.TryGetComponent<ClassicScriptingSettings>(out var scriptingSettings))
            {
                Debug.Log("Overriding scripting backend to IL2CPP");
                scriptingSettings.ScriptingBackend = ScriptingImplementation.IL2CPP;
                buildSettings.SetComponent(scriptingSettings);
            }
            else
            {
                Debug.LogError("Attempting to enable IL2CPP but ClassicScriptingSettings component was not found");
                EditorApplication.Exit(1);
            }
        }

        var result = buildSettings.Build();

        Debug.Log(result.ToString());

        return result.Succeeded;
    }

    public static void RunBuildSettings()
    {
        const string buildSettingsArgName = "-a2-buildsettings";
        if (!TryGetArg(buildSettingsArgName, out var path))
        {
            Debug.LogError($"Missing argument {buildSettingsArgName}");
            EditorApplication.Exit(1);
        }

        if (!TryGetArg("-a2-build-dir", out var buildDir))
            buildDir = "Build";

        if (!RunBuildSettingsAtPath(path, buildDir, HasArg("-a2-enable-il2cpp")))
        {
            Debug.LogError("Build failed");
            EditorApplication.Exit(1);
        }

        Debug.Log("Build successful");
    }
}
