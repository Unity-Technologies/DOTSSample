using System.IO;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEditor;
using UnityEditor.SceneManagement;
using Unity.NetCode;
using Unity.Sample.Core;
using Unity.Scenes;

[InitializeOnLoad]
public class EditorLevelManager
{
    static EditorLevelManager()
    {
        EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
        EditorApplication.update += OnUpdate;
    }

    public static void StartGameInEditor(string args)
    {
        // If editor already running we just process arguments
        if (EditorApplication.isPlaying)
        {
            Console.ProcessCommandLineArguments(args.Split(' '));
            return;
        }

        // Store command in playerprefs that will be consumed when playmode starts
        var count = PlayerPrefs.GetInt("CustomStartupCommandCount", 0);
        PlayerPrefs.SetString(string.Format("CustomStartupCommand{0}", count), args);
        count++;
        PlayerPrefs.SetInt("CustomStartupCommandCount", count);
        EditorApplication.isPlaying = true;
    }

    static void OnPlayModeStateChanged(PlayModeStateChange mode)
    {
        if(mode == PlayModeStateChange.ExitingEditMode)
        {
            /*
            // If we want to play in the editor as if we were launched as standalone, we unload whatever scene
            // you may have open to prevent entity conversion workflow from generating double entitites
            var startCommandCount = PlayerPrefs.GetInt("CustomStartupCommandCount", 0);
            if (startCommandCount > 0)
            {
                var scenePaths = new List<string>();
                for(int i = 0; i < EditorSceneManager.sceneCount; ++i)
                {
                    scenePaths.Add(EditorSceneManager.GetSceneAt(i).path);
                }
                PlayerPrefs.SetString("ScenesOpenBeforeLaunch", string.Join(";", scenePaths));

                if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                {
                    // Load the bootstrapper and proceed to launch
                    var s = EditorBuildSettings.scenes[0];
                    EditorSceneManager.OpenScene(s.path);
                }
                else
                {
                    // Cancel action
                    PlayerPrefs.SetInt("CustomStartupCommandCount", 0);
                    EditorApplication.isPlaying = false;
                }
            }
            */
        }
        else if (mode == PlayModeStateChange.EnteredEditMode)
        {
            /*
            // Close bootstrapper if it is there. Have to postpone that as unloading last
            // scene is not allowed
            bool closeFirstScene = false;
            if (EditorSceneManager.GetSceneAt(0).path == EditorBuildSettings.scenes[0].path)
                closeFirstScene = true;

            // Load previously opened scenes
            var scenes = PlayerPrefs.GetString("ScenesOpenBeforeLaunch");
            if (scenes.Length > 0)
            {
                foreach (var s in scenes.Split(';'))
                {
                    EditorSceneManager.OpenScene(s, OpenSceneMode.Additive);
                }
            }
            else
                closeFirstScene = false;

            if(closeFirstScene)
                EditorSceneManager.CloseScene(EditorSceneManager.GetSceneAt(0), true);

            PlayerPrefs.SetString("ScenesOpenBeforeLaunch", "");
            */
        }
        else if (mode == PlayModeStateChange.EnteredPlayMode)
        {
            // TODO (petera) remove this once open subscenes work with playmode
            bool editingAnySubscenes = false;
            foreach(var ss in SubScene.AllSubScenes)
            {
                if (ss.IsLoaded)
                    editingAnySubscenes = true;
            }
            if (editingAnySubscenes)
            {
                GameDebug.LogError("Entering playmode with open subscenes does not currently work properly. Leaving playmode.");
                EditorApplication.isPlaying = false;
                return;
            }

            if (Game.game == null)
            {
                //SceneManager.LoadScene(0);
                var go = (GameObject)GameObject.Instantiate(Resources.Load("Prefabs/Game", typeof(GameObject)));
                GameDebug.Assert(Game.game != null, "Failed to load Game prefab");
            }

            if (GameBootStrap.IsSingleLevelPlaymode)
                return;
            var startCommandCount = PlayerPrefs.GetInt("CustomStartupCommandCount", 0);
            if (startCommandCount > 0)
            {
                for (int i = 0; i < startCommandCount; i++)
                {
                    var key = string.Format("CustomStartupCommand{0}", i);
                    var args = PlayerPrefs.GetString(key, "");
                    Console.ProcessCommandLineArguments(args.Split(' '));
                    PlayerPrefs.DeleteKey(key);
                }

                PlayerPrefs.SetInt("CustomStartupCommandCount", 0);
            }
            else
            {
                // User pressed editor start button
                var scene = EditorSceneManager.GetSceneAt(0);
                var scenePath = scene.path;
                if (string.IsNullOrEmpty(scenePath))
                    return;

                var info = GetLevelInfoFor(scenePath);
                if (info != null)
                {
                    switch (info.levelType)
                    {
                        case LevelInfo.LevelType.Generic:
                            break;
                        case LevelInfo.LevelType.Gameplay:
                            //Console.EnqueueCommandNoHistory("preview");
                            var playType = ClientServerBootstrap.RequestedPlayType;
                            if (playType != ClientServerBootstrap.PlayType.Client)
                                Game.game.RequestGameLoop(typeof(ServerGameLoop), new []{scene.name});
                            if (playType != ClientServerBootstrap.PlayType.Server)
                                Game.game.RequestGameLoop(typeof(ClientGameLoop), new []{ClientServerBootstrap.RequestedAutoConnect});
                            break;
                        case LevelInfo.LevelType.Menu:
                            Console.SetOpen(false);
                            //Console.EnqueueCommandNoHistory("menu");
                            break;
                    }
                }
            }
        }
    }

    static void OnUpdate()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            if (EditorApplication.isPlaying && EditorApplication.isCompiling)
            {
                Debug.Log("Stopped play mode because compilation started.");
                EditorApplication.isPlaying = false;
            }
        }
    }

    public static LevelInfo GetLevelInfoFor(string scenePath)
    {
        foreach (var levelInfo in BuildTools.LoadLevelInfos())
        {
            if (AssetDatabase.GetAssetPath(levelInfo.main_scene) == scenePath)
            {
                return levelInfo;
            }
        }
        return null;
    }
}
