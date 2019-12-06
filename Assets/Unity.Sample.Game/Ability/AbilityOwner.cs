using System;
using Unity.Burst;
using Unity.Collections;
using Unity.DebugDisplay;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.NetCode;
using Unity.Physics;
using Unity.Physics.Systems;
using Unity.Sample.Core;
using RaycastHit = Unity.Physics.RaycastHit;


public static class AbilityOwner
{
    [ConfigVar(Name = "abilityowner.showstate", DefaultValue = "0", Description = "show state")]
    public static ConfigVar ShowInfo;

    public struct State : IComponentData
    {
        public int foo;
    }



    // List of all owned collection
    public struct OwnedCollection : IBufferElementData
    {
        public Entity collection;
        public bool enabled;
    }

    public struct OwnedAbility : IBufferElementData
    {
        public Entity entity;
        public Ability.AbilityTagValue tagValue;
        public bool isAction;
        public bool isActive;
    }

    private static int GetActiveButtonIndex(UserCommand cmd, ref AbilityCollection.AbilityEntry abilityEntry)
    {
        if(cmd.buttons.IsSet(abilityEntry.ActivateButton0))
            return 0;
        if(cmd.buttons.IsSet(abilityEntry.ActivateButton1))
            return 1;
        if(cmd.buttons.IsSet(abilityEntry.ActivateButton2))
            return 2;
        if(cmd.buttons.IsSet(abilityEntry.ActivateButton3))
            return 3;
        return -1;
    }

    abstract class UpdateAbilityOwnership : JobComponentSystem
    {
        protected override JobHandle OnUpdate(JobHandle inputDeps)
        {
            inputDeps.Complete();

            var AbilityCollectionStateFromEntity = GetComponentDataFromEntity<AbilityCollection.State>();
            var ItemInputStateFromEntity = GetComponentDataFromEntity<Item.InputState>(true);
            var AbilityEntryFromEntity = GetBufferFromEntity<AbilityCollection.AbilityEntry>(true);
            var AbilityTagFromEntity = GetComponentDataFromEntity<Ability.AbilityTag>(true);
            var AbilityActionFromEntity = GetComponentDataFromEntity<Ability.AbilityAction>(true);
            var AbilityStateActiveFromEntity = GetComponentDataFromEntity<Ability.AbilityStateActive>(true);

            Entities
                .WithReadOnly(ItemInputStateFromEntity)
                .WithReadOnly(AbilityEntryFromEntity)
                .WithReadOnly(AbilityTagFromEntity)
                .WithReadOnly(AbilityActionFromEntity)
                .WithReadOnly(AbilityStateActiveFromEntity)
                .ForEach((Entity ownerEntity, ref DynamicBuffer<OwnedCollection> ownedCollections, ref DynamicBuffer<OwnedAbility> ownedAbilities,
                    in Inventory.State inventoryState, in DynamicBuffer<Inventory.ItemEntry> items) =>
                {
                    //
                    // Update owned collections
                    //

                    // Rebuild owned collection buffer
                    // TODO (mogensh) This should only run when needed (set of AbilityCollection change). Store last active in inventory or sumtin ?
                    ownedCollections.Clear();

                    // Add root ability collection
                    if (AbilityCollectionStateFromEntity.HasComponent(ownerEntity))
                    {
                        ownedCollections.Add(new OwnedCollection
                        {
                            collection = ownerEntity,
                            enabled = true,
                        });
                    }

                    // Add inventory collections
                    var activeSlot = inventoryState.activeSlot;
                    for (int i = 0; i < items.Length; i++)
                    {
                        var itemEntity = items[i].entity;
                        if (itemEntity == Entity.Null)
                            continue;

                        if (!AbilityCollectionStateFromEntity.HasComponent(itemEntity))
                            continue;

                        var item = ItemInputStateFromEntity[itemEntity];
                        var collectionActive = (item.slot == activeSlot);

                        ownedCollections.Add(new OwnedCollection
                        {
                            collection = itemEntity,
                            enabled = collectionActive,
                        });
                    }


                    // Make sure all collections have owner as owner
                    for (int i = 0; i < ownedCollections.Length; i++)
                    {
                        var collection = ownedCollections[i].collection;
                        var abilityCollection = AbilityCollectionStateFromEntity[collection];
                        abilityCollection.abilityOwner = ownerEntity;
                        AbilityCollectionStateFromEntity[collection] = abilityCollection;
                    }

                    //
                    // Update owned abilities
                    //

                    ownedAbilities.Clear();
                    for (int i = 0; i < ownedCollections.Length; i++)
                    {
                        var collectionEntity = ownedCollections[i].collection;
                        var abilityEntries = AbilityEntryFromEntity[collectionEntity];
                        for (int j = 0; j < abilityEntries.Length; j++)
                        {
                            var abilityEntity = abilityEntries[j].entity;

                            var abilityTag = AbilityTagFromEntity[abilityEntity];
                            ownedAbilities.Add(new OwnedAbility
                            {
                                entity = abilityEntity,
                                tagValue = abilityTag.Value,
                                isAction = AbilityActionFromEntity.HasComponent(abilityEntity),
                                isActive = AbilityStateActiveFromEntity.HasComponent(abilityEntity)
                            });
                        }
                    }
                }).Run();

            return default;
        }
    }

    [UpdateInGroup(typeof(AbilityUpdateSystemGroup))]
    [UpdateBefore(typeof(PrepareOwnerForAbilityUpdate))]
    [DisableAutoCreation]
    [AlwaysSynchronizeSystem]
    class InitialUpdateAbilityOwnership : UpdateAbilityOwnership { }

    [UpdateInGroup(typeof(AbilityUpdateSystemGroup))]
    [UpdateAfter(typeof(AbilityUpdateCommandBufferSystem))]
    [DisableAutoCreation]
    [AlwaysSynchronizeSystem]
    class FinalUpdateAbilityOwnership : UpdateAbilityOwnership { }

    [UpdateInGroup(typeof(AbilityUpdateSystemGroup))]
    [UpdateBefore(typeof(BehaviourRequestPhase))]
    [DisableAutoCreation]
    [AlwaysSynchronizeSystem]
    class PrepareOwnerForAbilityUpdate : JobComponentSystem
    {
        [NativeDisableParallelForRestriction] ComponentDataFromEntity<AbilityCollection.State> m_AbilityCollectionStateData;

        protected override JobHandle OnUpdate(JobHandle inputDeps)
        {
            inputDeps.Complete();

            var PostUpdateCommands = new EntityCommandBuffer(Allocator.TempJob);

            var PredictingTick = World.GetExistingSystem<GhostPredictionSystemGroup>().PredictingTick;

            var AbilityCollectionAbilityEntryData = GetBufferFromEntity<AbilityCollection.AbilityEntry>(true);
            var EnabledAbilityData = GetComponentDataFromEntity<Ability.EnabledAbility>(true);

            Entities
                .WithReadOnly(AbilityCollectionAbilityEntryData)
                .WithReadOnly(EnabledAbilityData)
                .WithNativeDisableContainerSafetyRestriction(PostUpdateCommands)
                .ForEach((Entity entity, DynamicBuffer<OwnedCollection> ownedCollections, ref State state,
                    in PlayerControlled.State playerControlledState, in PredictedGhostComponent predictionData) =>
            {
                if (!GhostPredictionSystemGroup.ShouldPredict(PredictingTick, predictionData))
                    return;

                // Remove enabledAbility from all disabled collections // TODO (mogensh) avoid doing this every frame
                for (int i = 0; i < ownedCollections.Length; i++)
                {
                    var collection = ownedCollections[i].collection;

                    var collectionEnabled = ownedCollections[i].enabled;
                    var abilities = AbilityCollectionAbilityEntryData[collection];
                    for (int j = 0; j < abilities.Length; j++)
                    {
                        var ability = abilities[j].entity;
                        var abilityEnabled = EnabledAbilityData.Exists(ability);

                        if (abilityEnabled != collectionEnabled)
                            PostUpdateCommands.RemoveComponent<Ability.EnabledAbility>(ability);
                    }
                }

                // Update enabledEntity
                // TODO (mogensh) merge loop with loop above ??
                // TODO (mogensh) for now we setup reference to character on abilities every frame. Should we only detect collection set change and do this?
                for (int collIndex = 0; collIndex < ownedCollections.Length; collIndex++)
                {
                    if (!ownedCollections[collIndex].enabled)
                        continue;

                    var abilityEntries =
                        AbilityCollectionAbilityEntryData[ownedCollections[collIndex].collection];
                    for (int j = 0; j < abilityEntries.Length; j++)
                    {
                        var entry = abilityEntries[j];
                        var abilityEntity = entry.entity;

                        var enabledAbility = Ability.EnabledAbility.Default;
                        enabledAbility.owner = entity;
                        enabledAbility.activeButtonIndex =
                            GetActiveButtonIndex(playerControlledState.command, ref entry);

                        if (EnabledAbilityData.Exists(abilityEntity))
                            PostUpdateCommands.SetComponent(abilityEntity, enabledAbility);
                        else
                            PostUpdateCommands.AddComponent(abilityEntity, enabledAbility);
                    }
                }

            }).Run();

            PostUpdateCommands.Playback(EntityManager);
            PostUpdateCommands.Dispose();

            return default;
        }
    }

    [UpdateInGroup(typeof(AbilityUpdateSystemGroup))]
    [UpdateAfter(typeof(BehaviourRequestPhase))]
    [UpdateBefore(typeof(MovementUpdatePhase))]
    [DisableAutoCreation]
    [AlwaysSynchronizeSystem]
    class SelectActiveBehavior : JobComponentSystem
    {
        protected override JobHandle OnUpdate(JobHandle inputDeps)
        {
            inputDeps.Complete();

            // Cooldown to Idle
            Entities
                .ForEach((ref Ability.AbilityControl control, ref Ability.EnabledAbility enabled, ref Ability.AbilityStateCooldown cooldown) =>
            {
                if (cooldown.requestIdle)
                    control.behaviorState = Ability.AbilityControl.State.Idle;
            }).Run();

            // Active to Cooldown
            Entities
                .ForEach((ref Ability.AbilityControl control, ref Ability.EnabledAbility enabled, ref Ability.AbilityStateActive active) =>
            {
                if (active.requestCooldown)
                    control.behaviorState = Ability.AbilityControl.State.Cooldown;
            }).Run();

            // Select
            var abilityEntryBufferFromEntity = GetBufferFromEntity<AbilityCollection.AbilityEntry>(true);
            var abilityStateIdleFromEntity = GetComponentDataFromEntity<Ability.AbilityStateIdle>(true);
            var abilityStateActiveFromEntity = GetComponentDataFromEntity<Ability.AbilityStateActive>(true);
            var abilityControlFromEntity = GetComponentDataFromEntity<Ability.AbilityControl>(false);
            var PredictingTick = World.GetExistingSystem<GhostPredictionSystemGroup>().PredictingTick;
            Entities
                .WithReadOnly(abilityEntryBufferFromEntity)
                .WithReadOnly(abilityStateIdleFromEntity)
                .WithReadOnly(abilityStateActiveFromEntity)
                .WithNativeDisableParallelForRestriction(abilityControlFromEntity)
                .ForEach((DynamicBuffer<OwnedCollection> ownedCollections, ref State state, ref PredictedGhostComponent predictionData) =>
                {
                    if (!GhostPredictionSystemGroup.ShouldPredict(PredictingTick, predictionData))
                        return;

                    // Initialize ability info
                    var abilityEntries = new NativeList<AbilityCollection.AbilityEntry>(Allocator.Temp);
                    var oldAbilityEntryStates = new NativeList<Ability.AbilityControl.State>(Allocator.Temp);
                    var newAbilityEntryStates = new NativeList<Ability.AbilityControl.State>(Allocator.Temp);
                    var deactivateRequested = new NativeList<bool>(Allocator.Temp);
                    var activatableIndices = new NativeList<int>(Allocator.Temp);
                    var activeIndices = new NativeList<int>(Allocator.Temp);
                    for (int collIndex = 0; collIndex < ownedCollections.Length; collIndex++)
                    {
                        if (!ownedCollections[collIndex].enabled)
                            continue;

                        var abilityEntryBuffer = abilityEntryBufferFromEntity[ownedCollections[collIndex].collection];
                        for (int i = 0; i < abilityEntryBuffer.Length; i++)
                        {
                            var ability = abilityEntryBuffer[i].entity;

                            if (abilityStateIdleFromEntity.HasComponent(ability))
                            {
                                if (abilityStateIdleFromEntity[ability].requestActive)
                                {
                                    activatableIndices.Add(abilityEntries.Length);
                                    abilityEntries.Add(abilityEntryBuffer[i]);
                                    oldAbilityEntryStates.Add(Ability.AbilityControl.State.Idle);
                                    newAbilityEntryStates.Add(Ability.AbilityControl.State.Idle);
                                    deactivateRequested.Add(false);
                                }
                                continue;
                            }
                            if (abilityStateActiveFromEntity.HasComponent(ability))
                            {
                                activeIndices.Add(abilityEntries.Length);
                                abilityEntries.Add(abilityEntryBuffer[i]);
                                oldAbilityEntryStates.Add(Ability.AbilityControl.State.Active);
                                newAbilityEntryStates.Add(Ability.AbilityControl.State.Active);
                                deactivateRequested.Add(false);
                                continue;
                            }
                        }
                    }

                    // Attempt to activate abilities
                    for (int i = 0; i < activatableIndices.Length; i++)
                    {
                        var activatableIndex = activatableIndices[i];
                        var activatableAbilityEntry = abilityEntries[activatableIndex];

                        var canActivate = true;
                        for (int j = 0; j < activeIndices.Length; j++)
                        {
                            var activeIndex = activeIndices[j];
                            if (newAbilityEntryStates[activeIndex] != Ability.AbilityControl.State.Active)
                                continue;

                            var activeAbilityEntry = abilityEntries[activeIndex];
                            // Can new ability *not* run with active ability ?
                            if ((activatableAbilityEntry.abilityType & ~activeAbilityEntry.canRunWith) > 0)
                            {
                                // Can new ability interrupt ?
                                if ((activeAbilityEntry.abilityType & activatableAbilityEntry.canInterrupt) > 0)
                                {
                                    newAbilityEntryStates[activeIndex] = Ability.AbilityControl.State.Cooldown;
                                }
                                else
                                {
                                    // Not allowed to run, so request deactivate
                                    deactivateRequested[activeIndex] = true;
                                    canActivate = false;
                                }
                            }
                        }
                        if (canActivate)
                        {
                            newAbilityEntryStates[activatableIndex] = Ability.AbilityControl.State.Active;
                            activeIndices.Add(activatableIndex);
                        }
                    }

                    // Update ability state
                    for (int i = 0; i < abilityEntries.Length; i++)
                    {
                        if (oldAbilityEntryStates[i] == Ability.AbilityControl.State.Idle)
                        {
                            if (newAbilityEntryStates[i] == Ability.AbilityControl.State.Active)
                            {
                                // Idle to active
                                var abilityCtrl = abilityControlFromEntity[abilityEntries[i].entity];
                                abilityCtrl.behaviorState = Ability.AbilityControl.State.Active;
                                abilityCtrl.requestDeactivate = deactivateRequested[i];
                                abilityControlFromEntity[abilityEntries[i].entity] = abilityCtrl;
                            }
                        }
                        else if (oldAbilityEntryStates[i] == Ability.AbilityControl.State.Active)
                        {
                            if (newAbilityEntryStates[i] == Ability.AbilityControl.State.Cooldown)
                            {
                                // Active to cooldown
                                var abilityCtrl = abilityControlFromEntity[abilityEntries[i].entity];
                                abilityCtrl.behaviorState = Ability.AbilityControl.State.Cooldown;
                                abilityControlFromEntity[abilityEntries[i].entity] = abilityCtrl;
                            }
                            else if (deactivateRequested[i])
                            {
                                // Deactivate requested
                                var abilityCtrl = abilityControlFromEntity[abilityEntries[i].entity];
                                abilityCtrl.requestDeactivate = true;
                                abilityControlFromEntity[abilityEntries[i].entity] = abilityCtrl;
                            }
                        }
                    }

                    abilityEntries.Dispose();
                    oldAbilityEntryStates.Dispose();
                    newAbilityEntryStates.Dispose();
                    deactivateRequested.Dispose();
                    activatableIndices.Dispose();
                    activeIndices.Dispose();
                }).Run();

            return default;
        }
    }

    

    [UpdateInGroup(typeof(AbilityUpdateSystemGroup))]
    [UpdateAfter(typeof(SelectActiveBehavior))]
    [UpdateBefore(typeof(MovementUpdatePhase))]
    [DisableAutoCreation]
    [AlwaysSynchronizeSystem]
    class PrintAbilityStatusBehavior : JobComponentSystem
    {
        protected override JobHandle OnUpdate(JobHandle inputDeps)
        {
            inputDeps.Complete();

            var PredictingTick = World.GetExistingSystem<GhostPredictionSystemGroup>().PredictingTick;

            if (AbilityOwner.ShowInfo.IntValue > 0)
            {
                Entities
                    .WithoutBurst() // Debug output
                    .ForEach((Entity entity, ref PredictedGhostComponent predictedEntity, ref State state) =>
                {
                    if (!GhostPredictionSystemGroup.ShouldPredict(PredictingTick, predictedEntity))
                        return;

                    var ownedCollections = EntityManager.GetBuffer<OwnedCollection>(entity);

                    int x = 1;
                    int yIdle = 2;
                    int yActive = 2;
                    int yCooldown = 2;
                    int indent = 30;
                    Overlay.Managed.Write(x, 1, "Ability controller");
                    Overlay.Managed.Write(x, yIdle++, "Idle");
                    Overlay.Managed.Write(x + indent, yActive++, "Active");
                    Overlay.Managed.Write(x + indent * 2, yCooldown++, "Cooldown");

                    for (int collIndex = 0; collIndex < ownedCollections.Length; collIndex++)
                    {
                        //                        Overlay.Managed.Write(x, y++, " AbilityCollection");
                        var abilityEntries =
                            EntityManager.GetBuffer<AbilityCollection.AbilityEntry>(ownedCollections[collIndex].collection);

                        for (int i = 0; i < abilityEntries.Length; i++)
                        {
                            var abilityEntry = abilityEntries[i];
                            var abilityEntity = abilityEntry.entity;

                            var abilityCtrl = EntityManager.GetComponentData<Ability.AbilityControl>(abilityEntity);
                            if (EntityManager.HasComponent<Ability.AbilityStateIdle>(abilityEntity))
                            {
                                PrintAbility(x, ref yIdle, abilityEntity, ref abilityCtrl, ref abilityEntry);
                            }
                            if (EntityManager.HasComponent<Ability.AbilityStateActive>(abilityEntity))
                            {
                                PrintAbility(x + indent, ref yActive, abilityEntity, ref abilityCtrl, ref abilityEntry);
                            }
                            if (EntityManager.HasComponent<Ability.AbilityStateCooldown>(abilityEntity))
                            {
                                PrintAbility(x + indent * 2, ref yCooldown, abilityEntity, ref abilityCtrl, ref abilityEntry);
                            }
                        }
                    }
                }).Run();
            }

            return default;
        }

        void PrintAbility(int x, ref int y, Entity entity, ref Ability.AbilityControl abilityCtrl, ref AbilityCollection.AbilityEntry entry)
        {
            Overlay.Managed.Write(x, y++, "  Entity:" + entity);
            Overlay.Managed.Write(x, y++, "    Type:" + Convert.ToString(entry.abilityType, 2));
            Overlay.Managed.Write(x, y++, "    RunWith:" + Convert.ToString(entry.canRunWith, 2) + " Intrupt:" + Convert.ToString(entry.canInterrupt, 2));
            Overlay.Managed.Write(x, y++, "    request deactivate:" + abilityCtrl.requestDeactivate);
        }
    }
}
