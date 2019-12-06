using System;
using Unity.Animation;
using Unity.Collections;
using Unity.DataFlowGraph;
using Unity.Entities;
using Unity.Jobs;
using Unity.Sample.Core;
using UnityEngine;

public class AnimSourceTwist
{
    [Serializable]
    public struct BoneReferences : IComponentData
    {
        public int DriverIndex;
        public int TwistJointA;
        public int TwistJointB;
        public int TwistJointC;
    }

    [Serializable]
    public struct Factors : IComponentData
    {
        [Range(0f, 1f)] public float FactorA;
        [Range(0f, 1f)] public float FactorB;
        [Range(0f, 1f)] public float FactorC;
    }
    
    public struct Settings : IComponentData
    {
        public BlobAssetReference<RigDefinition> rigReference;
        public BoneReferences boneReferences;
        public Factors factors;
        public float twistMult;
    }

    public struct SystemState : ISystemStateComponentData
    {
        public static SystemState Default => new SystemState();
        public NodeHandle<TwistNode> TwistNode;
        public BoneReferences currentRigBoneIdx;
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
                Deinitialize(World, cmdBuffer, entity, m_AnimationGraphSystem, state);
            }).Run();

            cmdBuffer.Dispose();
            m_AnimationGraphSystem.RemoveRef();
        }

        protected override JobHandle OnUpdate(JobHandle inputDeps)
        {
            inputDeps.Complete();

            var nodeSet = m_AnimationGraphSystem.Set;
            var commands = new EntityCommandBuffer(Allocator.TempJob);
            var animationGraphSystem = m_AnimationGraphSystem;

            Entities
                .WithoutBurst() // Can be removed once NodeSets are Burst-friendly
                .WithNone<SystemState>()
                .ForEach((Entity entity, ref AnimSource.Data animSource, ref Settings settings) =>
            {
                GameDebug.Log(World,AnimSource.ShowLifetime,"Init KnockBack entity:{0} state entity:{1}", entity, animSource.animStateEntity);

                var state = SystemState.Default;
                state.TwistNode = AnimationGraphHelper.CreateNode<TwistNode>(animationGraphSystem, "Twist");
                
                // Expose input and outputs
                animSource.inputNode = state.TwistNode;
                animSource.inputPortID = (InputPortID)TwistNode.KernelPorts.Input;
                animSource.outputNode = state.TwistNode;
                animSource.outputPortID = (OutputPortID)TwistNode.KernelPorts.Output;
                commands.AddComponent(entity, state);
                
            }).Run();

            Entities
                .WithoutBurst() // Can be removed once NodeSets are Burst-friendly
                .WithNone<Settings>()
                .ForEach((Entity entity, ref SystemState state) =>
            {
                Deinitialize(World, commands, entity, animationGraphSystem, state);
            }).Run();

            commands.Playback(EntityManager);
            commands.Dispose();

            return default;
        }

        static void Deinitialize(World world,EntityCommandBuffer cmdBuffer, Entity entity, AnimationGraphSystem animGraphSys, SystemState state)
        {
            GameDebug.Log(world,AnimSource.ShowLifetime,"Deinit KnockBack entity:{0}", entity);

            if (state.TwistNode != default && animGraphSys.Set.Exists(state.TwistNode))
                AnimationGraphHelper.DestroyNode(animGraphSys,state.TwistNode);

            cmdBuffer.RemoveComponent<SystemState>(entity);
        }
    }
    
    [UpdateInGroup(typeof(AnimSourceApplyGroup))]
    [DisableAutoCreation]
    [AlwaysSynchronizeSystem]
    class PrepareGraph : JobComponentSystem
    {
        private EntityQuery m_GlobalGameTimeQuery;

        protected override void OnCreate()
        {
            m_GlobalGameTimeQuery = GetEntityQuery(ComponentType.ReadOnly<GlobalGameTime>());
        }

        protected override JobHandle OnUpdate(JobHandle inputDeps)
        {
            inputDeps.Complete();

            var nodeSet = World.GetExistingSystem<AnimationGraphSystem>().Set;

            var cmdBuffer = new EntityCommandBuffer(Allocator.TempJob);

            // Handle rig change
            Entities
                .WithNone<AnimSource.HasValidRig>()
                .WithoutBurst() // Can be removed once NodeSets are Burst-friendly
                .ForEach((Entity entity, ref AnimSource.Data animSource, ref SystemState state, ref Settings settings) =>
            {
                if (!EntityManager.HasComponent<SharedRigDefinition>(animSource.animStateEntity))
                    return;

                var sharedRigDef = EntityManager.GetSharedComponentData<SharedRigDefinition>(animSource.animStateEntity);
                var rig = sharedRigDef.Value;
                
                nodeSet.SendMessage(state.TwistNode, TwistNode.SimulationPorts.RigDefinition, rig);

                // Remap rig indexes
                // TODO: (sunek) Can we use the rig re-mapper from the animation package?
                BlobAssetReference<AnimationAssetDatabase.RigMap> rigMap;
                AnimationAssetDatabase.GetOrCreateRigMapping(World, settings.rigReference, rig, out rigMap);
                
                state.currentRigBoneIdx.DriverIndex = settings.boneReferences.DriverIndex != -1 ? 
                    rigMap.Value.BoneMap[settings.boneReferences.DriverIndex] : -1;
                
                state.currentRigBoneIdx.TwistJointA = settings.boneReferences.TwistJointA != -1 ? 
                    rigMap.Value.BoneMap[settings.boneReferences.TwistJointA] : -1;
                
                state.currentRigBoneIdx.TwistJointB = settings.boneReferences.TwistJointB != -1 ? 
                    rigMap.Value.BoneMap[settings.boneReferences.TwistJointB] : -1;
                
                state.currentRigBoneIdx.TwistJointC = settings.boneReferences.TwistJointC != -1 ? 
                    rigMap.Value.BoneMap[settings.boneReferences.TwistJointC] : -1;
                
                var nodeSettings = settings;
                nodeSettings.boneReferences = state.currentRigBoneIdx;
                
                nodeSet.SendMessage(state.TwistNode, TwistNode.SimulationPorts.Settings, nodeSettings);
                cmdBuffer.AddComponent<AnimSource.HasValidRig>(entity);

            }).Run();
            
            cmdBuffer.Playback(EntityManager);
            cmdBuffer.Dispose();

            return default;
        }
    }
}