using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Unity.Entities;
using Unity.NetCode;
using Unity.Sample.Core;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
using Unity.NetCode.Editor;
#endif



// Use the GameBootStrap to get in early and grab a copy of the system types
public class GameBootStrap : ClientServerBootstrap
{
#if UNITY_EDITOR
    [InitializeOnLoadMethod]
    static void SetupGhostDefaults()
    {
        GhostAuthoringComponentEditor.DefaultRootPath = "/Scripts/Networking/Generated/";
        GhostAuthoringComponentEditor.DefaultUpdateSystemPrefix = "Client/";
        GhostAuthoringComponentEditor.DefaultSerializerPrefix = "Server/";

        GhostAuthoringComponentEditor.GhostDefaultOverrides.Remove("Unity.Transforms.Translation");
        GhostAuthoringComponentEditor.GhostDefaultOverrides.Remove("Unity.Transforms.Rotation");
        GhostAuthoringComponentEditor.GhostDefaultOverrides.Remove("Unity.Transforms.LocalToWorld");

        GhostSnapshotValue.GameSpecificTypes.Add(new GhostSnapshotValueAngle());
    }
#endif
    public static List<Type> Systems { get { if (s_Systems == null) InitializeSystemsList(); return s_Systems; } }

    public static World DefaultWorld { get; private set; }

    static List<Type> s_Systems = null;

#if UNITY_EDITOR
    public static bool IsSingleLevelPlaymode { get; private set; }
#endif

    public override bool Initialize(string defaultWorldName)
    {
#if UNITY_EDITOR
        // TODO (timj) make this check more generic
        if (UnityEditor.SceneManagement.EditorSceneManager.GetSceneAt(0).name.ToLower() != "bootstrapper")
        {
            IsSingleLevelPlaymode = true;
            if (!GameApp.IsInitialized)
            {
                //SceneManager.LoadScene(0);
                var go = (GameObject)GameObject.Instantiate(Resources.Load("Prefabs/Game", typeof(GameObject)));
                GameDebug.Assert(GameApp.IsInitialized, "Failed to load Game prefab");
            }
            return base.Initialize(defaultWorldName);
        }
        IsSingleLevelPlaymode = false;
#endif
        var systems = DefaultWorldInitialization.GetAllSystems(WorldSystemFilterFlags.Default);
        GenerateSystemLists(systems);

        DefaultWorld = new World(defaultWorldName);
        World.DefaultGameObjectInjectionWorld = DefaultWorld;

        DefaultWorldInitialization.AddSystemsToRootLevelSystemGroups(DefaultWorld, ExplicitDefaultWorldSystems);
        ScriptBehaviourUpdateOrder.UpdatePlayerLoop(DefaultWorld);

        return true;
    }
    public static void InitializeSystemsList()
    {
        s_Systems = new List<Type>();
        foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
        {
            if (!TypeManager.IsAssemblyReferencingEntities(assembly))
                continue;

            IReadOnlyList<Type> allTypes;
            try
            {
                allTypes = assembly.GetTypes();
            }
            catch (ReflectionTypeLoadException e)
            {
                allTypes = e.Types.Where(t => t != null).ToList();
                Debug.LogWarning(
                    $"failed loading assembly: {(assembly.IsDynamic ? assembly.ToString() : assembly.Location)}");
            }
            s_Systems.AddRange(allTypes);
        }
    }
}
