
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.NetCode;
using Unity.Sample.Core;

public static class Inventory
{
    public struct ItemEntry : IBufferElementData
    {
        public Entity entity;
    }

    public struct State : IComponentData
    {
        [GhostDefaultField]
        public sbyte activeSlot;
    }

    public struct InternalState : IComponentData
    {
        public sbyte lastActiveInventorySlot;
    }

    public static void Server_DestroyAll(EntityManager entityManager, EntityCommandBuffer cmdBuffer, Entity inventoryEntity)
    {
        var slots = entityManager.GetBuffer<ItemEntry>(inventoryEntity);
        for (int j = 0; j < slots.Length; j++)
        {
            GameDebug.Assert(entityManager.Exists(slots[j].entity));
            PrefabAssetManager.DestroyEntity(cmdBuffer,slots[j].entity);
        }
        slots.Clear();
    }

    // TODO (mogensh) when to update this ?
    [AlwaysSynchronizeSystem]
    public class Update : JobComponentSystem
    {
        protected override JobHandle OnUpdate(JobHandle inputDeps)
        {
            var PartOwnerVisibleFromEntity = GetComponentDataFromEntity<PartOwner.Visible>(true);
            var ItemInputStateFromEntity = GetComponentDataFromEntity<Item.InputState>(true);
            var PostUpdateCommands = new EntityCommandBuffer(Allocator.TempJob);
            Entities
                .WithNativeDisableContainerSafetyRestriction(PostUpdateCommands)
                .WithReadOnly(PartOwnerVisibleFromEntity)
                .WithReadOnly(ItemInputStateFromEntity)
                .WithoutBurst() // calls EntityManager.Exists()
                .ForEach((Entity entity, ref State state, ref InternalState internalState, ref DynamicBuffer<ItemEntry> items) =>
            {
                if (state.activeSlot != internalState.lastActiveInventorySlot)
                {
                    if (internalState.lastActiveInventorySlot != -1)
                    {
                        var index = FindSlotIndex(ItemInputStateFromEntity, items, internalState.lastActiveInventorySlot);

                        var oldItem = items[index].entity;

                        if(PartOwnerVisibleFromEntity.HasComponent(oldItem))
                            PostUpdateCommands.RemoveComponent<PartOwner.Visible>(oldItem);

                        internalState.lastActiveInventorySlot = -1;
                    }

                    if (state.activeSlot != -1)
                    {
                        var index = FindSlotIndex(ItemInputStateFromEntity, items, state.activeSlot);
                        if (index != -1)
                        {
                            var newItem = items[index].entity;

                            if (EntityManager.Exists(newItem))
                            {
                                if(!PartOwnerVisibleFromEntity.HasComponent(newItem))
                                    PostUpdateCommands.AddComponent(newItem,new PartOwner.Visible());

                                internalState.lastActiveInventorySlot = state.activeSlot;
                            }
                        }
                    }
                }
            }).Run();
            PostUpdateCommands.Playback(EntityManager);
            PostUpdateCommands.Dispose();
            return default;
        }

        static int FindSlotIndex(ComponentDataFromEntity<Item.InputState> itemInputStates, DynamicBuffer<ItemEntry> items, int slot)
        {
            for (int i = 0; i < items.Length; i++)
            {
                var item = itemInputStates[items[i].entity];
                if (item.slot == slot)
                    return i;
            }

            return -1;
        }
    }
}
