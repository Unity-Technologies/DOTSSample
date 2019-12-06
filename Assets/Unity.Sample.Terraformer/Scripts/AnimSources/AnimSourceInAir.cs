using Unity.Animation;
using Unity.Collections;
using Unity.DataFlowGraph;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Sample.Core;

public class AnimSourceInAir
{
    public struct Settings : IComponentData
    {
        public BlobAssetReference<Clip> animInAir;
        public BlobAssetReference<Clip> animLandAntic;
        public BlobAssetReference<Clip> animAimDownToUp;
        public BlobAssetReference<Clip> AdditiveRefPose;

        public float landAnticStartHeight;
        public float blendDuration;
        public float aimDuringReloadPitch;
    }

    public struct SystemState : ISystemStateComponentData
    {
        public static SystemState Default => new SystemState();

        public NodeHandle<MixerNode> MainMixer;
        public NodeHandle<ClipPlayerNode> InAirClipNode;
        public NodeHandle<ClipNode> LandAnticClipNode;
        public NodeHandle<ClipNode> AimUpDownClipNode;
        public NodeHandle<ClipNode> AdditiveRefPose;
        public NodeHandle<DeltaNode> AimDelta;
        public NodeHandle<LayerMixerNode> AimMixer;

        public float AimVerticalDuration;
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

            // Handle created entities
            Entities
                .WithoutBurst() // Can be removed once NodeSets are Burst-friendly
                .WithNone<SystemState>()
                .ForEach((Entity entity, ref AnimSource.Data animSource, ref Settings settings) =>
            {
                GameDebug.Log(World,AnimSource.ShowLifetime,"Init AnimSourceInAir entity:{0}", entity);

                var state = SystemState.Default;

                state.MainMixer = AnimationGraphHelper.CreateNode<MixerNode>(m_AnimationGraphSystem,"MainMixer");

                state.InAirClipNode = AnimationGraphHelper.CreateNode<ClipPlayerNode>(m_AnimationGraphSystem,"InAirClipNode");
                nodeSet.Connect(state.InAirClipNode, ClipPlayerNode.KernelPorts.Output, state.MainMixer, MixerNode.KernelPorts.Input0);

                state.LandAnticClipNode = AnimationGraphHelper.CreateNode<ClipNode>(m_AnimationGraphSystem,"LandAnticClipNode");
                state.AdditiveRefPose = AnimationGraphHelper.CreateNode<ClipNode>(m_AnimationGraphSystem,"AdditiveRefPose");
                state.AimDelta = AnimationGraphHelper.CreateNode<DeltaNode>(m_AnimationGraphSystem,"AimDelta");

                nodeSet.Connect(state.LandAnticClipNode, ClipNode.KernelPorts.Output, state.MainMixer, MixerNode.KernelPorts.Input1);

                state.AimMixer = AnimationGraphHelper.CreateNode<LayerMixerNode>(m_AnimationGraphSystem,"AimMixer");
                nodeSet.Connect(state.MainMixer, MixerNode.KernelPorts.Output, state.AimMixer, LayerMixerNode.KernelPorts.Input0);
                nodeSet.SendMessage(state.AimMixer, LayerMixerNode.SimulationPorts.BlendModeInput0, BlendingMode.Override);
                nodeSet.SendMessage(state.AimMixer, LayerMixerNode.SimulationPorts.WeightInput0, 1f);

                state.AimUpDownClipNode = AnimationGraphHelper.CreateNode<ClipNode>(m_AnimationGraphSystem,"AimUpDownClipNode");

                nodeSet.Connect(state.AdditiveRefPose, ClipNode.KernelPorts.Output, state.AimDelta, DeltaNode.KernelPorts.Subtract);
                nodeSet.Connect(state.AimUpDownClipNode, ClipNode.KernelPorts.Output, state.AimDelta, DeltaNode.KernelPorts.Input);
                nodeSet.Connect(state.AimDelta, DeltaNode.KernelPorts.Output, state.AimMixer, LayerMixerNode.KernelPorts.Input1);

                nodeSet.SendMessage(state.AimMixer, LayerMixerNode.SimulationPorts.BlendModeInput1, BlendingMode.Additive);
                nodeSet.SendMessage(state.AimMixer, LayerMixerNode.SimulationPorts.WeightInput1, 1f);

                // Store clip info
                state.AimVerticalDuration = settings.animAimDownToUp.Value.Duration;

                // Expose input and outputs
                animSource.outputNode = state.AimMixer;
                animSource.outputPortID = (OutputPortID)LayerMixerNode.KernelPorts.Output;

                commands.AddComponent(entity,state);
            }).Run();

            // Handled deleted entities
            var animationGraphSystem = m_AnimationGraphSystem;
            Entities
                .WithoutBurst() // Can be removed once NodeSets are Burst-friendly
                .WithNone<Settings>().ForEach((Entity entity, ref SystemState state) =>
            {
                Deinitialize(World, commands, entity, animationGraphSystem, state);
            }).Run();

            commands.Playback(EntityManager);
            commands.Dispose();

            return default;
        }

        static void Deinitialize(World world, EntityCommandBuffer cmdBuffer, Entity entity, AnimationGraphSystem animGraphSys, SystemState state)
        {
            GameDebug.Log(world,AnimSource.ShowLifetime,"Init AnimSourceInAir entity:{0}", entity);

            AnimationGraphHelper.DestroyNode(animGraphSys,state.MainMixer);
            AnimationGraphHelper.DestroyNode(animGraphSys,state.InAirClipNode);
            AnimationGraphHelper.DestroyNode(animGraphSys,state.LandAnticClipNode);
            AnimationGraphHelper.DestroyNode(animGraphSys,state.AimUpDownClipNode);
            AnimationGraphHelper.DestroyNode(animGraphSys,state.AimDelta);
            AnimationGraphHelper.DestroyNode(animGraphSys,state.AdditiveRefPose);
            AnimationGraphHelper.DestroyNode(animGraphSys,state.AimMixer);

            cmdBuffer.RemoveComponent<SystemState>(entity);
        }
    }

    [UpdateInGroup(typeof(AnimSourceUpdateBGroup))]
    [DisableAutoCreation]
    [AlwaysSynchronizeSystem]
    class UpdateSystem : JobComponentSystem
    {
        private EntityQuery m_GlobalGameTimeQuery;

        protected override void OnCreate()
        {
            m_GlobalGameTimeQuery = GetEntityQuery(ComponentType.ReadOnly<GlobalGameTime>());
        }

        protected override JobHandle OnUpdate(JobHandle inputDeps)
        {
            inputDeps.Complete();

            var globalTime = m_GlobalGameTimeQuery.GetSingleton<GlobalGameTime>();
            var deltaTime = globalTime.frameDuration;

            var characterInterpolatedDataFromEntity = GetComponentDataFromEntity<Character.InterpolatedData>(false);
            var characterStateFromEntity = GetComponentDataFromEntity<Character.State>(true);

            Entities
                .ForEach((Entity entity, ref AnimSource.Data animSource,
                    ref Settings settings, ref SystemState state, ref AnimSource.AllowWrite allowWrite) =>
            {
                if (!characterInterpolatedDataFromEntity.HasComponent(animSource.animStateEntity))
                    return;

                var charInterpolatedState = characterInterpolatedDataFromEntity[animSource.animStateEntity];
                var charState = characterStateFromEntity[animSource.animStateEntity];

                if (allowWrite.FirstUpdate)
                {
                    charInterpolatedState.inAirTime = 0;
                    allowWrite.FirstUpdate = false;
                }
                else
                {
                    charInterpolatedState.inAirTime += deltaTime;
                }

                // TODO: Lerp rotation like in jump state
                charInterpolatedState.rotation = charInterpolatedState.aimYaw;

                // Blend in land anticipation when close to ground // TODO: Only do this test when moving downwards
                var nearGround = charState.altitude < settings.landAnticStartHeight;
                var deltaWeight = deltaTime / settings.blendDuration;
                charInterpolatedState.landAnticWeight += nearGround ? deltaWeight : -deltaWeight;
                charInterpolatedState.landAnticWeight = math.clamp(charInterpolatedState.landAnticWeight, 0, 1);

                characterInterpolatedDataFromEntity[animSource.animStateEntity] = charInterpolatedState;
            }).Run();

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

                nodeSet.SendMessage(state.MainMixer, MixerNode.SimulationPorts.RigDefinition, rig);
                nodeSet.SendMessage(state.AimMixer, LayerMixerNode.SimulationPorts.RigDefinition, rig);

                var inAirClipInstance = ClipManager.Instance.GetClipFor(rig, settings.animInAir);
                nodeSet.SendMessage(state.InAirClipNode, ClipPlayerNode.SimulationPorts.ClipInstance, inAirClipInstance);

                var landAnticClipInstance = ClipManager.Instance.GetClipFor(rig, settings.animLandAntic);
                nodeSet.SendMessage(state.LandAnticClipNode, ClipNode.SimulationPorts.ClipInstance, landAnticClipInstance);

                var aimUpDownClipInstance = ClipManager.Instance.GetClipFor(rig, settings.animAimDownToUp);
                nodeSet.SendMessage(state.AimUpDownClipNode, ClipNode.SimulationPorts.ClipInstance, aimUpDownClipInstance);

                var addRefPoseClipInstance = ClipManager.Instance.GetClipFor(rig, settings.AdditiveRefPose);
                nodeSet.SendMessage(state.AdditiveRefPose, ClipNode.SimulationPorts.ClipInstance, addRefPoseClipInstance);

                nodeSet.SendMessage(state.AimDelta, DeltaNode.SimulationPorts.RigDefinition, rig);

                cmdBuffer.AddComponent<AnimSource.HasValidRig>(entity);
            }).Run();

            // Apply state
            Entities
                .WithoutBurst() // Can be removed once NodeSets are Burst-friendly
                .ForEach((Entity entity, ref AnimSource.Data animSource, ref Settings settings, ref SystemState state) =>
            {
                if (!EntityManager.HasComponent<Character.InterpolatedData>(animSource.animStateEntity))
                    return;

                var charInterpolatedState = EntityManager.GetComponentData<Character.InterpolatedData>(animSource.animStateEntity);

                var time = charInterpolatedState.inAirTime % settings.animInAir.Value.Duration;
                nodeSet.SendMessage(state.InAirClipNode, ClipPlayerNode.SimulationPorts.Time, time);
                nodeSet.SetData(state.LandAnticClipNode, ClipNode.KernelPorts.Time, charInterpolatedState.inAirTime);

                nodeSet.SendMessage(state.MainMixer, MixerNode.SimulationPorts.Blend, charInterpolatedState.landAnticWeight);

                nodeSet.SetData(state.AimUpDownClipNode, ClipNode.KernelPorts.Time,
                    charInterpolatedState.aimPitch * state.AimVerticalDuration / 180f);

                // Blend in/out aim for reload
                nodeSet.SendMessage(state.AimMixer, LayerMixerNode.SimulationPorts.WeightInput2,
                    1f - charInterpolatedState.blendOutAim * (1f - settings.aimDuringReloadPitch));
            }).Run();

            cmdBuffer.Playback(EntityManager);
            cmdBuffer.Dispose();

            return default;
        }
    }
}
