using Unity.Animation;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.DataFlowGraph;
using Unity.Sample.Core;

public class AnimSourceSingleClip
{
    public struct Settings : IComponentData
    {
        public BlobAssetReference<Clip> Clip;
    }

    public struct SystemState : ISystemStateComponentData
    {
        public static SystemState Default => new SystemState();

        public NodeHandle<ClipPlayerNode> ClipNode;
        public float time;
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
                Deinitialize(World, cmdBuffer, entity, animationGraphSystem, state);
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

            // Handle created entities
            Entities
                .WithoutBurst() // Can be removed once NodeSets are Burst-friendly
                .WithNone<SystemState>()
                .ForEach((Entity entity, ref AnimSource.Data animSource, ref Settings settings) =>
            {
                GameDebug.Log(World,AnimSource.ShowLifetime,"Init AnimSourceSingleClip entity:{0}", entity);

                var state = SystemState.Default;

                state.ClipNode = AnimationGraphHelper.CreateNode<ClipPlayerNode>(animationGraphSystem, "SingleClipNode");

                // Expose input and outputs
                animSource.outputNode = state.ClipNode;
                animSource.outputPortID = (OutputPortID)LayerMixerNode.KernelPorts.Output;

                commands.AddComponent(entity,state);
            }).Run();

            // Handled deleted entities
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

        static void Deinitialize(World world, EntityCommandBuffer cmdBuffer, Entity entity, AnimationGraphSystem animGraphSys, SystemState state)
        {
            GameDebug.Log(world,AnimSource.ShowLifetime,"Deinit AnimSourceSingleClip entity:{0}", entity);

            AnimationGraphHelper.DestroyNode(animGraphSys,state.ClipNode);

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

            // TODO: (sunek) Do this once, instead all over the place
            var globalTime = m_GlobalGameTimeQuery.GetSingleton<GlobalGameTime>();
            var deltaTime = globalTime.frameDuration;

            var nodeSet = World.GetExistingSystem<AnimationGraphSystem>().Set;

            var cmdBuffer = new EntityCommandBuffer(Allocator.TempJob);

            // Handle rig changes
            Entities
                .WithNone<AnimSource.HasValidRig>()
                .WithoutBurst() // Can be removed once NodeSets are Burst-friendly
                .ForEach((Entity entity, ref AnimSource.Data animSource, ref Settings settings, ref SystemState state) =>
            {
                if (!EntityManager.HasComponent<Character.InterpolatedData>(animSource.animStateEntity))
                    return;

                if (!EntityManager.HasComponent<SharedRigDefinition>(animSource.animStateEntity))
                    return;

                var sharedRigDef = EntityManager.GetSharedComponentData<SharedRigDefinition>(animSource.animStateEntity);
                var rig = sharedRigDef.Value;

                var clipInstance = ClipManager.Instance.GetClipFor(rig, settings.Clip);
                nodeSet.SendMessage(state.ClipNode, ClipPlayerNode.SimulationPorts.ClipInstance, clipInstance);

                cmdBuffer.AddComponent<AnimSource.HasValidRig>(entity);
            }).Run();

            // Apply state
            Entities
                .WithoutBurst() // Can be removed once NodeSets are Burst-friendly
                .ForEach((Entity entity, ref AnimSource.Data animSource, ref Settings settings, ref SystemState state) =>
            {
                state.time += deltaTime;
                var time = state.time % settings.Clip.Value.Duration;
                nodeSet.SendMessage(state.ClipNode, ClipPlayerNode.SimulationPorts.Time, time);
            }).Run();

            cmdBuffer.Playback(EntityManager);
            cmdBuffer.Dispose();

            return default;
        }
    }
}
