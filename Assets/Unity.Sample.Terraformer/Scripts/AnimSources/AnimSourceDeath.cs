using System;
using Unity.Animation;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.DataFlowGraph;
using Unity.Sample.Core;
using UnityEngine;


public class AnimSourceDeath
{
    public struct Settings : IComponentData
    {
        public BlobAssetReference<Clip> Clip;
    }

    public struct SystemState : ISystemStateComponentData
    {
        public static SystemState Default => new SystemState();

        public NodeHandle<ClipNode> ClipNode;
        public NodeHandle<DeltaTimeNode> DeltaTimeNode;
        public NodeHandle<TimeCounterNode> TimeCounterNode;
        public float clipTime;
    }

    [UpdateInGroup(typeof(AnimSourceInitializationGroup))]
    [DisableAutoCreation]
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

            // Handle created entities
            Entities
                .WithoutBurst() // Can be removed once NodeSets are Burst-friendly
                .WithNone<SystemState>().ForEach((Entity entity, ref AnimSource.Data animSource, ref Settings settings) =>
            {

                GameDebug.Log(World,AnimSource.ShowLifetime,"Init Death entity:{0} state entity:{1}", entity, animSource.animStateEntity);

                var state = SystemState.Default;
                state.DeltaTimeNode = AnimationGraphHelper.CreateNode<DeltaTimeNode>(m_AnimationGraphSystem,"DeltaTimeNode");
                state.TimeCounterNode = AnimationGraphHelper.CreateNode<TimeCounterNode>(m_AnimationGraphSystem,"TimeCounterNode");
                state.ClipNode = AnimationGraphHelper.CreateNode<ClipNode>(m_AnimationGraphSystem,"ClipNode");

                nodeSet.Connect(state.DeltaTimeNode, DeltaTimeNode.KernelPorts.DeltaTime, state.TimeCounterNode, TimeCounterNode.KernelPorts.DeltaTime);
                nodeSet.Connect(state.TimeCounterNode, TimeCounterNode.KernelPorts.Time, state.ClipNode, ClipNode.KernelPorts.Time);

                commands.AddComponent(entity, state);

                animSource.outputNode = state.ClipNode;
                animSource.outputPortID = (OutputPortID)ClipNode.KernelPorts.Output;
            }).Run();

            // Handled deleted entities
            Entities
                .WithoutBurst() // Can be removed once NodeSets are Burst-friendly
                .WithNone<Settings>().ForEach((Entity entity, ref SystemState state) =>
            {
                Deinitialize(World, commands, entity, m_AnimationGraphSystem, state);
            }).Run();

            commands.Playback(EntityManager);
            commands.Dispose();

            return default;
        }

        static void Deinitialize(World world, EntityCommandBuffer cmdBuffer, Entity entity, AnimationGraphSystem animGraphSystem, SystemState state)
        {
            GameDebug.Log(world,AnimSource.ShowLifetime,"Deinit Death entity:{0}", entity);

            if (state.ClipNode != default && animGraphSystem.Set.Exists(state.ClipNode))
                AnimationGraphHelper.DestroyNode(animGraphSystem,state.ClipNode);

            if (state.DeltaTimeNode != default && animGraphSystem.Set.Exists(state.DeltaTimeNode))
                AnimationGraphHelper.DestroyNode(animGraphSystem,state.DeltaTimeNode);

            if (state.TimeCounterNode != default && animGraphSystem.Set.Exists(state.TimeCounterNode))
                AnimationGraphHelper.DestroyNode(animGraphSystem,state.TimeCounterNode);

            cmdBuffer.RemoveComponent<SystemState>(entity);
        }
    }

    [UpdateInGroup(typeof(AnimSourceApplyGroup))]
    [DisableAutoCreation]
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

            // Handle rig changes
            Entities
                .WithNone<AnimSource.HasValidRig>()
                .WithoutBurst() // Can be removed once NodeSets are Burst-friendly
                .ForEach((Entity entity, ref AnimSource.Data animSource, ref Settings settings, ref SystemState state) =>
            {
                if (!EntityManager.HasComponent<SharedRigDefinition>(animSource.animStateEntity))
                    return;

                var sharedRigDef = EntityManager.GetSharedComponentData<SharedRigDefinition>(animSource.animStateEntity);
                var rig = sharedRigDef.Value;

                var clipInstance = ClipManager.Instance.GetClipFor(rig, settings.Clip);
                nodeSet.SendMessage(state.ClipNode, ClipNode.SimulationPorts.ClipInstance, clipInstance);

                cmdBuffer.AddComponent<AnimSource.HasValidRig>(entity);
            }).Run();

            cmdBuffer.Playback(EntityManager);
            cmdBuffer.Dispose();

            return default;
        }
    }
}
