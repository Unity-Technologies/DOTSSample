using System;
using Unity.Animation;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.DataFlowGraph;
using UnityEngine.Serialization;


public class AnimSourceIK
{
    [Serializable]
    public struct Settings : IComponentData
    {
        public IkBindings.TwoBoneIKCProperties IkSettings;
        [FormerlySerializedAs("IkDataHigh")]
        public IkBindings.TwoBoneIKData IkData;
        public BlobAssetReference<RigDefinition> RigAsset;
    }

    public struct SystemState : ISystemStateComponentData
    {
        public static SystemState Default => new SystemState();

        public NodeHandle<TwoBoneIKNode> IkNode;
    }

    [UpdateInGroup(typeof(AnimSourceInitializationGroup))]
    [DisableAutoCreation]
    [AlwaysSynchronizeSystem]
    class InitSystem : JobComponentSystem
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

            // Initialize
            Entities
                .WithoutBurst() // Can be removed once NodeSets are Burst-friendly
                .WithNone<SystemState>()
                .ForEach((Entity entity, ref AnimSource.Data animSource, ref Settings settings) =>
            {
                var state = SystemState.Default;
                state.IkNode = AnimationGraphHelper.CreateNode<TwoBoneIKNode>(animationGraphSystem, "IkNode");

                commands.AddComponent(entity, state);

                animSource.inputNode = state.IkNode;
                animSource.inputPortID = (InputPortID)TwoBoneIKNode.KernelPorts.Input;
                animSource.outputNode = state.IkNode;
                animSource.outputPortID = (OutputPortID)TwoBoneIKNode.KernelPorts.Output;
            }).Run();

            // Deinitialize
            Entities
                .WithoutBurst() // Can be removed once NodeSets are Burst-friendly
                .WithNone<Settings>()
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
            if (state.IkNode != default && animGraphSys.Set.Exists(state.IkNode))
                AnimationGraphHelper.DestroyNode(animGraphSys,state.IkNode);

            cmdBuffer.RemoveComponent<SystemState>(entity);
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

            var cmdBuffer = new EntityCommandBuffer(Allocator.TempJob);

            Entities
                .WithNone<AnimSource.HasValidRig>()
                .WithoutBurst() // Can be removed once NodeSets are Burst-friendly
                .ForEach((Entity entity, ref AnimSource.Data animSource, ref Settings settings, ref SystemState state) =>
            {
                if (!EntityManager.HasComponent<SharedRigDefinition>(animSource.animStateEntity))
                    return;

                var sharedRigDef = EntityManager.GetSharedComponentData<SharedRigDefinition>(animSource.animStateEntity);
                var rig = sharedRigDef.Value;


                // Set the rig, bindings and values
                nodeSet.SendMessage(state.IkNode, TwoBoneIKNode.SimulationPorts.RigDefinition, rig);

                BlobAssetReference<AnimationAssetDatabase.RigMap> rigMap;
                AnimationAssetDatabase.GetOrCreateRigMapping(World, settings.RigAsset, rig, out rigMap);

                var ikData = new TwoBoneIKNode.TwoBoneIKData
                {
                    // Remap the bone idx to current skeleton
                    Root = settings.IkData.Root == -1 ? -1 : rigMap.Value.BoneMap[settings.IkData.Root],
                    Mid = settings.IkData.Mid == -1 ? -1 : rigMap.Value.BoneMap[settings.IkData.Mid],
                    Tip = settings.IkData.Tip == -1 ? -1 : rigMap.Value.BoneMap[settings.IkData.Tip],
                    Hint = settings.IkData.Hint == -1 ? -1 : rigMap.Value.BoneMap[settings.IkData.Hint],
                    Target = settings.IkData.Target == -1 ? -1 : rigMap.Value.BoneMap[settings.IkData.Target],

                    WeightChannelIdx = settings.IkSettings.WeightCurve,
                    LimbLengths = settings.IkData.LimbLengths,
                    TargetOffset = settings.IkData.TargetOffset
                };

                nodeSet.SendMessage(state.IkNode, TwoBoneIKNode.SimulationPorts.TwoBoneIKSetup, in ikData);

                nodeSet.SetData(state.IkNode, TwoBoneIKNode.KernelPorts.Weight, settings.IkSettings.Weight);
                nodeSet.SetData(state.IkNode, TwoBoneIKNode.KernelPorts.TargetPositionWeight, settings.IkSettings.TargetPositionWeight);
                nodeSet.SetData(state.IkNode, TwoBoneIKNode.KernelPorts.TargetRotationWeight, settings.IkSettings.TargetRotationWeight);
                nodeSet.SetData(state.IkNode, TwoBoneIKNode.KernelPorts.HintWeight, settings.IkSettings.HintWeight);

                cmdBuffer.AddComponent<AnimSource.HasValidRig>(entity);
            }).Run();

            cmdBuffer.Playback(EntityManager);
            cmdBuffer.Dispose();

            return default;
        }
    }
}
