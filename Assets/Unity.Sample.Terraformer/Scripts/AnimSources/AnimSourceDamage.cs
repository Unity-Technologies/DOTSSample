using Unity.Animation;
using Unity.Collections;
using Unity.Entities;
using Unity.DataFlowGraph;
using Unity.Jobs;
using Unity.Sample.Core;
using UnityEngine;


public class AnimSourceDamage
{
    public struct Settings : IComponentData
    {
        public float Blend;
        public BlobAssetReference<BlendTree1D> BlendTree;
        public BlobAssetReference<Clip> AdditiveRefPose;
    }


//    public struct Dots
    public struct SystemState : ISystemStateComponentData
    {
        public static SystemState Default => new SystemState();
        public NodeHandle<LayerMixerNode> MixerNode;
        public NodeHandle<BlendTree1DNode> BlendTreeNode;
        public NodeHandle<ClipNode> AdditiveRefPoseNode;
        public NodeHandle<DeltaNode> AdditiveDeltaNode;
        public NodeHandle<DeltaTimeNode> DeltaTimeNode;
        public NodeHandle<TimeCounterNode> TimeCounterNode;
        public NodeHandle<TimeLoopNode> TimeLoopNode;
        public NodeHandle<FloatRcpSimNode> FloatRcpSimNode;

        public int LastReactionTick;
        public bool ReactionAnimPlaying;
        public float ReactionAnimDuration;
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
                GameDebug.Log(World,AnimSource.ShowLifetime,"Init AnimSourceDamage entity:{0}", entity);

                var state = SystemState.Default;

                settings.BlendTree = BlendTreeEntityStoreHelper.CreateBlendTree1DFromComponents(EntityManager, entity);;

                // Create nodes
                state.BlendTreeNode = AnimationGraphHelper.CreateNode<BlendTree1DNode>(m_AnimationGraphSystem,"BlendTreeNode");
                state.MixerNode = AnimationGraphHelper.CreateNode<LayerMixerNode>(m_AnimationGraphSystem,"MixerNode");
                state.AdditiveRefPoseNode = AnimationGraphHelper.CreateNode<ClipNode>(m_AnimationGraphSystem,"AdditiveRefPoseNode");
                state.AdditiveDeltaNode = AnimationGraphHelper.CreateNode<DeltaNode>(m_AnimationGraphSystem,"AdditiveDeltaNode");
                state.DeltaTimeNode = AnimationGraphHelper.CreateNode<DeltaTimeNode>(m_AnimationGraphSystem,"DeltaTimeNode");
                state.TimeCounterNode = AnimationGraphHelper.CreateNode<TimeCounterNode>(m_AnimationGraphSystem,"TimeCounterNode");
                state.TimeLoopNode = AnimationGraphHelper.CreateNode<TimeLoopNode>(m_AnimationGraphSystem,"TimeLoopNode");
                state.FloatRcpSimNode = AnimationGraphHelper.CreateNode<FloatRcpSimNode>(m_AnimationGraphSystem,"FloatRcpSimNode");

                nodeSet.Connect(state.AdditiveRefPoseNode, ClipNode.KernelPorts.Output, state.AdditiveDeltaNode, DeltaNode.KernelPorts.Subtract);
                nodeSet.Connect(state.BlendTreeNode, BlendTree1DNode.KernelPorts.Output, state.AdditiveDeltaNode, DeltaNode.KernelPorts.Input);
                nodeSet.Connect(state.AdditiveDeltaNode, DeltaNode.KernelPorts.Output, state.MixerNode, LayerMixerNode.KernelPorts.Input1);

                nodeSet.Connect(state.DeltaTimeNode, DeltaTimeNode.KernelPorts.DeltaTime, state.TimeCounterNode, TimeCounterNode.KernelPorts.DeltaTime);
                nodeSet.Connect(state.TimeCounterNode, TimeCounterNode.KernelPorts.Time, state.TimeLoopNode, TimeLoopNode.KernelPorts.InputTime);
                nodeSet.Connect(state.TimeLoopNode, TimeLoopNode.KernelPorts.OutputTime, state.BlendTreeNode, BlendTree1DNode.KernelPorts.NormalizedTime);
                nodeSet.Connect(state.FloatRcpSimNode, FloatRcpSimNode.SimulationPorts.Output, state.TimeCounterNode, TimeCounterNode.SimulationPorts.Speed);
                nodeSet.Connect(state.BlendTreeNode, BlendTree1DNode.SimulationPorts.Duration, state.FloatRcpSimNode, FloatRcpSimNode.SimulationPorts.Input);

                nodeSet.SendMessage(state.TimeLoopNode, TimeLoopNode.SimulationPorts.Duration, 1.0F);

                nodeSet.SendMessage(state.MixerNode, LayerMixerNode.SimulationPorts.WeightInput0, 1f);
                nodeSet.SendMessage(state.MixerNode, LayerMixerNode.SimulationPorts.WeightInput1, 0f);

                nodeSet.SendMessage(state.MixerNode, LayerMixerNode.SimulationPorts.BlendModeInput0, BlendingMode.Override);
                nodeSet.SendMessage(state.MixerNode, LayerMixerNode.SimulationPorts.BlendModeInput1, BlendingMode.Additive);

                // Load clips and store clip info
                var blendSpaceClip = settings.BlendTree.Value.Motions[0].Clip;
                state.ReactionAnimDuration = blendSpaceClip.Value.Duration;

                // Declare anim source inputs and outputs
                animSource.inputNode = state.MixerNode;
                animSource.inputPortID = (InputPortID)LayerMixerNode.KernelPorts.Input0;
                animSource.outputNode = state.MixerNode;
                animSource.outputPortID = (OutputPortID)LayerMixerNode.KernelPorts.Output;

                // Add state
                commands.AddComponent(entity, state);
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

        static void Deinitialize(World world, EntityCommandBuffer cmdBuffer, Entity entity, AnimationGraphSystem animGraphSys, SystemState state)
        {
            GameDebug.Log(world,AnimSource.ShowLifetime,"Deinit AnimSourceDamage entity:{0}", entity);

            AnimationGraphHelper.DestroyNode(animGraphSys,state.BlendTreeNode);
            AnimationGraphHelper.DestroyNode(animGraphSys,state.MixerNode);
            AnimationGraphHelper.DestroyNode(animGraphSys,state.AdditiveDeltaNode);
            AnimationGraphHelper.DestroyNode(animGraphSys,state.AdditiveRefPoseNode);
            AnimationGraphHelper.DestroyNode(animGraphSys,state.DeltaTimeNode);
            AnimationGraphHelper.DestroyNode(animGraphSys,state.TimeCounterNode);
            AnimationGraphHelper.DestroyNode(animGraphSys,state.TimeLoopNode);
            AnimationGraphHelper.DestroyNode(animGraphSys,state.FloatRcpSimNode);

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

            var globalTime = m_GlobalGameTimeQuery.GetSingleton<GlobalGameTime>();
            var time = globalTime.gameTime;

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

                var addRefPoseClipInstance = ClipManager.Instance.GetClipFor(rig, settings.AdditiveRefPose);
                nodeSet.SendMessage(state.AdditiveRefPoseNode, ClipNode.SimulationPorts.ClipInstance, addRefPoseClipInstance);
                nodeSet.SendMessage(state.MixerNode, LayerMixerNode.SimulationPorts.RigDefinition, rig);
                nodeSet.SendMessage(state.AdditiveDeltaNode, DeltaNode.SimulationPorts.RigDefinition, rig);
                nodeSet.SendMessage(state.BlendTreeNode, BlendTree1DNode.SimulationPorts.RigDefinition, rig);
                nodeSet.SendMessage(state.BlendTreeNode, BlendTree1DNode.SimulationPorts.BlendTree, settings.BlendTree);

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

                if (charInterpolatedState.damageTick > state.LastReactionTick)
                {
                    // Handle first update
                    if (state.LastReactionTick == -1)
                    {
                        state.LastReactionTick = charInterpolatedState.damageTick;
                        return;
                    }

                    state.ReactionAnimPlaying = true;
                    state.LastReactionTick = charInterpolatedState.damageTick;
                    nodeSet.SendMessage(state.MixerNode, LayerMixerNode.SimulationPorts.WeightInput1, 0.66f);

                    var angle = MathHelper.SignedAngle(charInterpolatedState.aimYaw, charInterpolatedState.damageDirection);

                    // Blend the animations based on impact angle
                    var blendParameter = new Parameter {
                        Id = settings.BlendTree.Value.BlendParameter,
                        Value = angle
                    };

                    nodeSet.SendMessage(state.BlendTreeNode, BlendTree1DNode.SimulationPorts.Parameter, blendParameter);

                    var f = nodeSet.GetFunctionality(state.BlendTreeNode);
                    state.ReactionAnimDuration = f.GetDuration(state.BlendTreeNode);

                    // Reset the phase of the animation
                    nodeSet.SendMessage(state.TimeCounterNode, TimeCounterNode.SimulationPorts.Time, 0f);
                }
                else if (state.ReactionAnimPlaying)
                {
                    var timeSinceLastDamage = (time.tick - state.LastReactionTick) / (float)time.tickRate;
                    // TODO: Use duration from blend space
                    if (timeSinceLastDamage > state.ReactionAnimDuration)
                    {
                        state.ReactionAnimPlaying = false;
                        nodeSet.SendMessage(state.MixerNode, LayerMixerNode.SimulationPorts.WeightInput1, 0f);
                    }
                }
            }).Run();

            cmdBuffer.Playback(EntityManager);
            cmdBuffer.Dispose();

            return default;
        }
    }
}
