using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.NetCode;
using Unity.Sample.Core;
using UnityEngine;

public static class Item
{
    public struct InputState : IComponentData
    {
        public static InputState Default => new InputState();

        [GhostDefaultField]
        public Entity owner;
        [GhostDefaultField]
        public byte slot;
        [GhostDefaultField]
        public int playerId;

        public int failedInitializationCount;
    }

    [UpdateInGroup(typeof(InitializationSystemGroup))]
    [AlwaysSynchronizeSystem]
    public class Initialize : JobComponentSystem
    {
        public struct Initialized : ISystemStateComponentData
        {}

        protected override JobHandle OnUpdate(JobHandle inputDeps)
        {
            var InventoryItemEntryFromEntity = GetBufferFromEntity<Inventory.ItemEntry>(false);
            var PostUpdateCommands = new EntityCommandBuffer(Allocator.TempJob);
            Entities.WithNone<Initialized>()
                .WithNativeDisableContainerSafetyRestriction(PostUpdateCommands)
                .WithoutBurst() // Captures managed data
                .ForEach((Entity entity, ref InputState state) =>
            {
                // TODO (timj) find a better way to handle this. Owner is a weak reference so it might not exist yet - for now just give it some time
                if (state.failedInitializationCount < 10 && !EntityManager.Exists(state.owner))
                {
                    ++state.failedInitializationCount;
                    return;
                }
                GameDebug.Assert(InventoryItemEntryFromEntity.Exists(state.owner), "Item owner does not have inventory");

                // Register item in owner inventory
                var items = InventoryItemEntryFromEntity[state.owner];
                items.Add(new Inventory.ItemEntry
                {
                    entity = entity,
                });

                PostUpdateCommands.AddComponent(entity,new Initialized());

                var attachEntity = new RigAttacher.AttachEntity
                {
                    Value = state.owner,
                };
                PostUpdateCommands.AddComponent(entity,attachEntity);
            }).Run();

            Entities.WithNone<InputState>()
                .WithAll<Initialized>()
                .WithNativeDisableContainerSafetyRestriction(PostUpdateCommands)
                .ForEach((Entity entity) =>
            {
                PostUpdateCommands.RemoveComponent<Initialized>(entity);
            }).Run();

            PostUpdateCommands.Playback(EntityManager);
            PostUpdateCommands.Dispose();
            return default;
        }
    }
}
