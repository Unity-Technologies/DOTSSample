using UnityEngine;
using Unity.Entities;
using Unity.Jobs;
using Unity.Scenes;

#if UNITY_EDITOR
[ExecuteAlways]
[UpdateInGroup(typeof(InitializationSystemGroup))]
[AlwaysSynchronizeSystem]
public class ConfigureEditorSystem : JobComponentSystem
{
    protected override void OnCreate()
    {
        if (UnityEditor.EditorApplication.isPlaying)
            return;
        World.GetOrCreateSystem<SceneSystem>().BuildSettingsGUID = ClientGameLoopSystem.ClientBuildSettingsGUID;
    }

    protected override JobHandle OnUpdate(JobHandle inputDeps)
    {
        return default;
    }
}
#endif
