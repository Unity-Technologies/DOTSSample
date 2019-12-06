using System;
using Unity.Animation;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.DataFlowGraph;
using UnityEngine;


public class AnimSourceSprintBlend
{
    public struct AnimSourceEntities : IBufferElementData
    {
        public Entity Value;
    }

    public struct SystemState : ISystemStateComponentData
    {
        public static SystemState Default => new SystemState();
        public NodeHandle<MixerNode> MixerNode;
        public bool WasSprinting;
        public Entity AnimSourceEntityA;
        public Entity AnimSourceEntityB;
        public AnimSource.Data AnimSourceA;
        public AnimSource.Data AnimSourceB;
    }

    public struct ConnectionsUpdated : IComponentData
    {}

    [Serializable]
    public struct Settings : IComponentData
    {
        [Range(0f, 1f)]
        public float sprintTransitionSpeed;

        [Tooltip("Always reset child controllers on sprint state change")]
        public bool resetControllerOnChange;
    }

    [UpdateInGroup(typeof(AnimSourceInitializationGroup))]
    [DisableAutoCreation]
    [AlwaysSynchronizeSystem]
    class Initialize : JobComponentSystem
    {
        AnimationGraphSystem m_AnimationGraphSystem;

        protected override void OnCreate()
        {
            base.OnCreate();
            m_AnimationGraphSystem = World.GetExistingSystem<AnimationGraphSystem>();
            m_AnimationGraphSystem.AddRef();
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();

            var cmdBuffer = new EntityCommandBuffer(Allocator.Temp);
            var animationGraphSystem = m_AnimationGraphSystem;
            Entities
                .WithoutBurst() // Can be removed once NodeSets are Burst-friendly
                .ForEach((Entity entity, ref SystemState state) =>
            {
                Deinitialize(cmdBuffer, entity, animationGraphSystem, state);
            }).Run();

            cmdBuffer.Dispose();
            m_AnimationGraphSystem.RemoveRef();
        }

        protected override JobHandle OnUpdate(JobHandle inputDeps)
        {
            inputDeps.Complete();

            var commands = new EntityCommandBuffer(Allocator.TempJob);
            var animationGraphSystem = m_AnimationGraphSystem;

            // Handle create AnimSource
            Entities
                .WithoutBurst() // Can be removed once NodeSets are Burst-friendly
                .WithNone<SystemState>()
                .ForEach((Entity entity, DynamicBuffer<AnimSourceEntities> childAnimSourceEntities, ref AnimSource.Data animSource) =>
            {
                var state = SystemState.Default;

                state.MixerNode = AnimationGraphHelper.CreateNode<MixerNode>(animationGraphSystem, "MixerNode");

                state.AnimSourceEntityA = childAnimSourceEntities[0].Value;
                state.AnimSourceEntityB = childAnimSourceEntities[1].Value;

                state.AnimSourceA = EntityManager.GetComponentData<AnimSource.Data>(state.AnimSourceEntityA);
                state.AnimSourceB = EntityManager.GetComponentData<AnimSource.Data>(state.AnimSourceEntityB);

                animSource.outputNode = state.MixerNode;
                animSource.outputPortID = (OutputPortID)MixerNode.KernelPorts.Output;

                commands.AddComponent(entity, state);
            }).Run();

            // Handle destroyed entities
            Entities
                .WithoutBurst() // Can be removed once NodeSets are Burst-friendly
                .WithNone<AnimSource.Data>()
                .ForEach((Entity entity, ref SystemState state) =>
            {
                Deinitialize(commands, entity, animationGraphSystem, state);
            }).Run();

            commands.Playback(EntityManager);
            commands.Dispose();

            return default;
        }

        static void Deinitialize(EntityCommandBuffer cmdBuffer, Entity entity, AnimationGraphSystem animGraphSys, SystemState state)
        {
            AnimationGraphHelper.DestroyNode(animGraphSys,state.MixerNode);
            cmdBuffer.RemoveComponent<SystemState>(entity);
        }
    }

    [UpdateInGroup(typeof(AnimSourceUpdateAGroup))]
    [DisableAutoCreation]
    [AlwaysSynchronizeSystem]
    class UpdateSystem : JobComponentSystem
    {
        protected override JobHandle OnUpdate(JobHandle inputDeps)
        {
            inputDeps.Complete();

            // TODO (mogensh) find cleaner way to get time
            var globalTime = GetEntityQuery(ComponentType.ReadOnly<GlobalGameTime>()).GetSingleton<GlobalGameTime>();
            var deltaTime = globalTime.frameDuration;

            var commands = new EntityCommandBuffer(Allocator.TempJob);
            var characterInterpolatedDataFromEntity = GetComponentDataFromEntity<Character.InterpolatedData>(true);
            var abilityInterpolatedStateFromEntity = GetComponentDataFromEntity<AbilityMovement.InterpolatedState>(true);
            var animSourceAllowWriteFromEntity = GetComponentDataFromEntity<AnimSource.AllowWrite>(true);
            var ownedAbilityBufferFromEntity = GetBufferFromEntity<AbilityOwner.OwnedAbility>(true);

            // Update state
            Entities
                .ForEach((Entity entity, ref AnimSource.Data animSource, ref Settings settings,
                    ref SystemState state, ref AnimSource.AllowWrite allowWrite) =>
            {
                if (!characterInterpolatedDataFromEntity.HasComponent(animSource.animStateEntity))
                    return;

                var charInterpolatedState = characterInterpolatedDataFromEntity[animSource.animStateEntity];
                var abilityMovementEntity = Ability.FindAbility(ownedAbilityBufferFromEntity, animSource.animStateEntity, AbilityMovement.Tag);
                if (abilityMovementEntity == Entity.Null)
                    return;

                var abilityMovement = abilityInterpolatedStateFromEntity[abilityMovementEntity];

                if (allowWrite.FirstUpdate && abilityMovement.previousCharLocoState != AbilityMovement.LocoState.Jump &&
                    abilityMovement.previousCharLocoState != AbilityMovement.LocoState.DoubleJump &&
                    abilityMovement.previousCharLocoState != AbilityMovement.LocoState.InAir)
                {
                    charInterpolatedState.sprintWeight = charInterpolatedState.sprinting ? 1 : 0;
                }

                var transitionSpeed = settings.sprintTransitionSpeed * 60 * deltaTime;

                if (charInterpolatedState.sprinting)
                {
                    charInterpolatedState.sprintWeight = math.clamp(charInterpolatedState.sprintWeight + transitionSpeed, 0f, 1f);
                }
                else
                {
                    charInterpolatedState.sprintWeight = math.clamp(charInterpolatedState.sprintWeight - transitionSpeed, 0f, 1f);
                }

                // Control which child state we update and whether to send First Update
                if (!charInterpolatedState.sprinting)
                {
                    var firstUpdate = state.WasSprinting && settings.resetControllerOnChange || allowWrite.FirstUpdate;
                    if (animSourceAllowWriteFromEntity.HasComponent(state.AnimSourceEntityA))
                    {
                        commands.SetComponent(state.AnimSourceEntityA, new AnimSource.AllowWrite {FirstUpdate = firstUpdate});
                    }
                    else
                    {
                        commands.AddComponent(state.AnimSourceEntityA, new AnimSource.AllowWrite { FirstUpdate = firstUpdate });
                    }

                    if (animSourceAllowWriteFromEntity.HasComponent(state.AnimSourceEntityB))
                    {
                        commands.RemoveComponent<AnimSource.AllowWrite>(state.AnimSourceEntityB);
                    }
                }
                else
                {
                    var firstUpdate = !state.WasSprinting && settings.resetControllerOnChange || allowWrite.FirstUpdate;
                    if (animSourceAllowWriteFromEntity.HasComponent(state.AnimSourceEntityB))
                    {
                        commands.SetComponent(state.AnimSourceEntityB, new AnimSource.AllowWrite {FirstUpdate = firstUpdate});
                    }
                    else
                    {
                        commands.AddComponent(state.AnimSourceEntityB, new AnimSource.AllowWrite { FirstUpdate = firstUpdate });
                    }

                    if (animSourceAllowWriteFromEntity.HasComponent(state.AnimSourceEntityA))
                    {
                        commands.RemoveComponent<AnimSource.AllowWrite>(state.AnimSourceEntityA);
                    }
                }

                state.WasSprinting = charInterpolatedState.sprinting;
                allowWrite.FirstUpdate = false;

                commands.SetComponent(animSource.animStateEntity, charInterpolatedState);
            }).Run();

            // Remove allow update from children (when no longer update self)
            // TODO: (sunek) Another reason why we must insist on this updates before it's children..
            Entities
                .WithNone<AnimSource.AllowWrite>()
                .ForEach((Entity entity, ref AnimSource.Data animSource, ref Settings settings,
                    ref SystemState state) =>
            {
                if (animSourceAllowWriteFromEntity.HasComponent(state.AnimSourceEntityA))
                {
                    commands.RemoveComponent<AnimSource.AllowWrite>(state.AnimSourceEntityA);
                }
                if (animSourceAllowWriteFromEntity.HasComponent(state.AnimSourceEntityB))
                {
                    commands.RemoveComponent<AnimSource.AllowWrite>(state.AnimSourceEntityB);
                }
            }).Run();

            commands.Playback(EntityManager);
            commands.Dispose();

            return default;
        }
    }

    [UpdateInGroup(typeof(AnimSourceApplyGroup))]
    [DisableAutoCreation]
    [AlwaysSynchronizeSystem]
    class PrepareGraph : JobComponentSystem
    {
        protected override JobHandle OnUpdate(JobHandle inputDeps)
        {
            inputDeps.Complete();

            var nodeSet = World.GetExistingSystem<AnimationGraphSystem>().Set;
            var commands = new EntityCommandBuffer(Allocator.TempJob);

            Entities
                .WithNone<AnimSource.HasValidRig>()
                .WithoutBurst() // Can be removed once NodeSets are Burst-friendly
                .ForEach((Entity entity, ref AnimSource.Data animSource, ref SystemState state) =>
            {
                if (!EntityManager.HasComponent<SharedRigDefinition>(animSource.animStateEntity))
                    return;

                var sharedRigDef = EntityManager.GetSharedComponentData<SharedRigDefinition>(animSource.animStateEntity);
                var rig = sharedRigDef.Value;

                nodeSet.SendMessage(state.MixerNode, MixerNode.SimulationPorts.RigDefinition, rig);
                commands.AddComponent<AnimSource.HasValidRig>(entity);
            }).Run();

            Entities
                .WithoutBurst() // Can be removed once NodeSets are Burst-friendly
                .WithNone<ConnectionsUpdated>()
                .WithAll<AnimSource.Data>()
                .ForEach((Entity entity, ref SystemState state) =>
            {
                nodeSet.Connect(state.AnimSourceA.outputNode, state.AnimSourceA.outputPortID, state.MixerNode, (InputPortID)MixerNode.KernelPorts.Input0);
                nodeSet.Connect(state.AnimSourceB.outputNode, state.AnimSourceB.outputPortID, state.MixerNode, (InputPortID)MixerNode.KernelPorts.Input1);

                commands.AddComponent(entity, new ConnectionsUpdated());
            }).Run();

            // Apply state
            Entities
                .WithoutBurst() // Can be removed once NodeSets are Burst-friendly
                .ForEach((Entity entity, ref AnimSource.Data animSource, ref Settings settings, ref SystemState state) =>
            {
                if (!EntityManager.HasComponent<Character.InterpolatedData>(animSource.animStateEntity))
                    return;

                var charInterpolatedState = EntityManager.GetComponentData<Character.InterpolatedData>(animSource.animStateEntity);

                var smoothedWeight = math.smoothstep(0f, 1f, charInterpolatedState.sprintWeight);
                nodeSet.SendMessage(state.MixerNode, MixerNode.SimulationPorts.Blend, smoothedWeight);
            }).Run();

            commands.Playback(EntityManager);
            commands.Dispose();

            return default;
        }
    }
}

