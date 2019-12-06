using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Sample.Core;
using UnityEngine;


[UpdateInGroup(typeof(ClientLateUpdateGroup))]
[DisableAutoCreation]
[AlwaysSynchronizeSystem]
public class SoundRequestSystem : JobComponentSystem
{
    protected override void OnCreate()
    {
        base.OnCreate();
        var entity = GetSingletonEntity<SoundRegistry>();
        var registry = EntityManager.GetSharedComponentData<SoundRegistry>(entity);
        SoundSystem.Instance.SetRegistry(registry);
    }
    protected override JobHandle OnUpdate(JobHandle inputDeps)
    {
        var PostUpdateCommands = new EntityCommandBuffer(Allocator.TempJob);
        var LocalToWorldFromEntity = GetComponentDataFromEntity<Unity.Transforms.LocalToWorld>(true);
        Entities
            .WithReadOnly(LocalToWorldFromEntity)
            .WithNativeDisableContainerSafetyRestriction(PostUpdateCommands)
            .WithoutBurst() // Uses EntityManager.Exists
            .ForEach((Entity e, ref SoundRequest req) =>
        {
            // If handle is invalid, let go of this tracker
            if(!SoundSystem.Instance.IsValid(ref req.soundHandle))
            {
                PostUpdateCommands.DestroyEntity(e);
                return;
            }
            // Update position
            if (!EntityManager.Exists(req.trackEntity))
            {
                GameDebug.LogWarning("Sound trying to follow invalid entity " + req.trackEntity);
                PostUpdateCommands.DestroyEntity(e);
                return;
            }
            if (!LocalToWorldFromEntity.HasComponent(req.trackEntity))
            {
                GameDebug.LogWarning("Sound trying to follow entity " + req.trackEntity + " which does not have a transform");
                PostUpdateCommands.DestroyEntity(e);
                return;
            }
            var ltw = LocalToWorldFromEntity[req.trackEntity];
            SoundSystem.Instance.UpdatePosition(ref req.soundHandle, ltw.Position);
        }).Run();
        PostUpdateCommands.Playback(EntityManager);
        PostUpdateCommands.Dispose();
        return default;
    }
}
