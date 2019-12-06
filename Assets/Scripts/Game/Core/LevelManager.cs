using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Sample.Core;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;
using UnityEngine.SceneManagement;


public enum LevelState
{
    Loading,
    Loaded,
}

public class Level
{
    public LevelState state;
    public string name;
    public AsyncOperation loadOperation;
}

public class LevelManager
{
    public Level currentLevel { get; private set; }

    public void Init()
    {
#if UNITY_EDITOR
        if (GameBootStrap.IsSingleLevelPlaymode)
            currentLevel = new Level {state = LevelState.Loaded, name = UnityEditor.SceneManagement.EditorSceneManager.GetSceneAt(0).name};
#endif
    }

    public bool IsCurrentLevelLoaded()
    {
        return currentLevel != null && currentLevel.state == LevelState.Loaded;
    }

    public bool IsLoadingLevel()
    {
        return currentLevel != null && currentLevel.state == LevelState.Loading;
    }

    public string GetScenePathFromName(string name)
    {
#if UNITY_EDITOR
        foreach(var l in AssetDatabase.FindAssets("t:" + typeof(LevelInfo).Name))
        {
            var path = AssetDatabase.GUIDToAssetPath(l);
            if (path.ToLower().EndsWith("/" + name.ToLower() + ".asset"))
            {
                var levelInfo = AssetDatabase.LoadAssetAtPath<LevelInfo>(path);
                return AssetDatabase.GetAssetPath(levelInfo.main_scene);
            }
        }
#else
        for(int i = 0, c = SceneManager.sceneCountInBuildSettings; i < c; ++i)
        {
            if(SceneUtility.GetScenePathByBuildIndex(i).ToLower().EndsWith(name.ToLower()+".unity"))
            {
                return SceneUtility.GetScenePathByBuildIndex(i);
            }
        }
#endif
        return "";
    }

    public bool CanLoadLevel(string name)
    {
        return GetScenePathFromName(name) != "";
    }

    // Name is the name of the LevelInfo asset (sans .asset)
    public bool LoadLevel(string name)
    {
        if (currentLevel != null)
            UnloadLevel();

        var path = GetScenePathFromName(name);
        if(path == "")
        {
            GameDebug.Log("Unable to find " + name + " in included scenes");
            return false;
        }

#if UNITY_EDITOR
        AsyncOperation mainLoadOperation = UnityEditor.SceneManagement.EditorSceneManager.LoadSceneAsyncInPlayMode(path, new LoadSceneParameters(LoadSceneMode.Single));
#else
        AsyncOperation mainLoadOperation = SceneManager.LoadSceneAsync(path, new LoadSceneParameters(LoadSceneMode.Single));
#endif
        if (mainLoadOperation == null)
        {
            GameDebug.Log("Failed to load level : " + name);
            return false;
        }

        var newLevel = new Level();
        newLevel.name = name;

        currentLevel = newLevel;
        currentLevel.loadOperation = mainLoadOperation;

        return true;
    }

    public void UnloadLevel()
    {
        if (currentLevel == null)
            return;

        if (currentLevel.state == LevelState.Loading)
            throw new NotImplementedException("TODO : Implement unload during load");

        // TODO : Load empty scene for now
        SceneManager.LoadScene(1);

        currentLevel = null;
    }

    public void Update()
    {
        if (currentLevel != null && currentLevel.state == LevelState.Loading)
        {
            var done = currentLevel.loadOperation.isDone;
            if (done)
            {
                // Do activation here?
                currentLevel.state = LevelState.Loaded;

                if (Game.GameLoopCount == 1)
                {
                    if (Game.GetGameLoop<ServerGameLoop>() != null)
                        StripCode(BuildType.Server, true);
                    else if (Game.GetGameLoop<ClientGameLoop>() != null)
                        StripCode(BuildType.Client, true);
                    else
                        StripCode(BuildType.Default, true);
                }
                else
                    StripCode(BuildType.Default, true);

                GameDebug.Log("Scene " + currentLevel.name + " loaded");
            }
        }
    }

    // TODO (petera) this code was moved here to make it available outside of editor - until we start cooking for client/server
    public enum BuildType
    {
        Default,
        Client,
        Server,
    }

    public static void StripCode(BuildType buildType, bool isDevelopmentBuild)
    {
        GameDebug.Log("Stripping code for " + buildType.ToString() + " (" + (isDevelopmentBuild ? "DevBuild" : "NonDevBuild") + ")");
        var deleteBehaviors = new List<MonoBehaviour>();
        var deleteGameObjects = new List<GameObject>();

        foreach (var behavior in UnityEngine.Object.FindObjectsOfType<MonoBehaviour>())
        {
            if (behavior.GetType().GetCustomAttributes(typeof(EditorOnlyComponentAttribute), false).Length > 0)
                deleteBehaviors.Add(behavior);
            else if (behavior.GetType().GetCustomAttributes(typeof(EditorOnlyGameObjectAttribute), false).Length > 0)
                deleteGameObjects.Add(behavior.gameObject);
            else if (buildType == BuildType.Server && behavior.GetType().GetCustomAttributes(typeof(ClientOnlyComponentAttribute), false).Length > 0)
                deleteBehaviors.Add(behavior);
            else if (buildType == BuildType.Client && behavior.GetType().GetCustomAttributes(typeof(ServerOnlyComponentAttribute), false).Length > 0)
                deleteBehaviors.Add(behavior);
            else if (!isDevelopmentBuild && behavior.GetType().GetCustomAttributes(typeof(DevelopmentOnlyComponentAttribute), false).Length > 0)
                deleteBehaviors.Add(behavior);
        }

        GameDebug.Log(string.Format("Stripping {0} game object(s) and {1} behavior(s)", deleteGameObjects.Count, deleteBehaviors.Count));

        foreach (var gameObject in deleteGameObjects)
            UnityEngine.Object.DestroyImmediate(gameObject);

        foreach (var behavior in deleteBehaviors)
            UnityEngine.Object.DestroyImmediate(behavior);
    }
}
