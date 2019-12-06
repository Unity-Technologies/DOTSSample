using System;
using Unity.Animation;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.DataFlowGraph;
using Unity.Mathematics;
using Unity.Sample.Core;
using UnityEngine;

public class AnimSourceSprint
{
    [Serializable]
    public struct Settings : IComponentData
    {
        public BlobAssetReference<BlendTree1D> LocoBlendTreeAsset;
        public BlobAssetReference<Clip> animAimDownToUp;
        public BlobAssetReference<Clip> additiveRefPose;
        public float changeDirSpeed;
        public float stateResetWindow;
    }

    public struct SystemState : ISystemStateComponentData
    {
        public static SystemState Default => new SystemState();

        public float PlaySpeed;
        public float RunClipDuration;
        public float AimClipDuration;

        public NodeHandle<ClipNode> AdditiveRefPose;
        public NodeHandle<BlendTree1DNode> BlendNode;
        public NodeHandle<ClipNode> AimClipNode;
        public NodeHandle<LayerMixerNode> AimMixerNode;
        public NodeHandle<DeltaNode> AimDeltaNode;
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
                GameDebug.Log(World,AnimSource.ShowLifetime,"InitSystem Sprint entity:{0} state entity:{1}", entity, animSource.animStateEntity);

                settings.LocoBlendTreeAsset = BlendTreeEntityStoreHelper.CreateBlendTree1DFromComponents(EntityManager, entity);

                var state = SystemState.Default;

                state.BlendNode = AnimationGraphHelper.CreateNode<BlendTree1DNode>(animationGraphSystem, "BlendNode");

                state.AimClipNode = AnimationGraphHelper.CreateNode<ClipNode>(animationGraphSystem, "AimClipNode");
                state.AimDeltaNode = AnimationGraphHelper.CreateNode<DeltaNode>(animationGraphSystem, "AimDeltaNode");
                state.AdditiveRefPose = AnimationGraphHelper.CreateNode<ClipNode>(animationGraphSystem, "AdditiveRefPose");
                state.AimMixerNode = AnimationGraphHelper.CreateNode<LayerMixerNode>(animationGraphSystem, "AimMixerNode");

                nodeSet.Connect(state.BlendNode, BlendTree1DNode.KernelPorts.Output, state.AimMixerNode, LayerMixerNode.KernelPorts.Input0);
                nodeSet.Connect(state.AimClipNode, ClipNode.KernelPorts.Output, state.AimDeltaNode, DeltaNode.KernelPorts.Input);
                nodeSet.Connect(state.AdditiveRefPose, ClipNode.KernelPorts.Output, state.AimDeltaNode, DeltaNode.KernelPorts.Subtract);
                nodeSet.Connect(state.AimDeltaNode, DeltaNode.KernelPorts.Output, state.AimMixerNode, LayerMixerNode.KernelPorts.Input1);

                nodeSet.SendMessage(state.AimMixerNode, LayerMixerNode.SimulationPorts.WeightInput0, 1f);
                nodeSet.SendMessage(state.AimMixerNode, LayerMixerNode.SimulationPorts.WeightInput1, 1f);
                nodeSet.SendMessage(state.AimMixerNode, LayerMixerNode.SimulationPorts.BlendModeInput0, BlendingMode.Override);
                nodeSet.SendMessage(state.AimMixerNode, LayerMixerNode.SimulationPorts.BlendModeInput1, BlendingMode.Additive);

                // Load clips and store clip info
                state.AimClipDuration = settings.animAimDownToUp.Value.Duration;

                // All clips are the same length so we can use a static clip duration
                state.RunClipDuration = settings.LocoBlendTreeAsset.Value.Motions[0].Clip.Value.Duration;

                // Expose input and outputs
                animSource.outputNode = state.AimMixerNode;
                animSource.outputPortID = (OutputPortID)LayerMixerNode.KernelPorts.Output;

                commands.AddComponent(entity, state);
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

        static void Deinitialize(World world,EntityCommandBuffer cmdBuffer, Entity entity, AnimationGraphSystem animGraphSys, SystemState state)
        {
            GameDebug.Log(world,AnimSource.ShowLifetime,"Deinit Sprint entity:{0}", entity);

            AnimationGraphHelper.DestroyNode(animGraphSys,state.BlendNode);
            AnimationGraphHelper.DestroyNode(animGraphSys,state.AimClipNode);
            AnimationGraphHelper.DestroyNode(animGraphSys,state.AimMixerNode);
            AnimationGraphHelper.DestroyNode(animGraphSys,state.AimDeltaNode);
			AnimationGraphHelper.DestroyNode(animGraphSys,state.AdditiveRefPose);

            cmdBuffer.RemoveComponent<SystemState>(entity);
        }
    }

    [UpdateInGroup(typeof(AnimSourceUpdateBGroup))]
    [DisableAutoCreation]
    [AlwaysSynchronizeSystem]
    class UpdateSystem : JobComponentSystem
    {
        protected override JobHandle OnUpdate(JobHandle inputDeps)
        {
            inputDeps.Complete();

            var characterInterpolatedDataFromEntity = GetComponentDataFromEntity<Character.InterpolatedData>(false);
            var abilityInterpolatedStateFromEntity = GetComponentDataFromEntity<AbilityMovement.InterpolatedState>(true);
            var ownedAbilityBufferFromEntity = GetBufferFromEntity<AbilityOwner.OwnedAbility>(true);

            // TODO (mogensh) find cleaner way to get time
            var globalTime = GetEntityQuery(ComponentType.ReadOnly<GlobalGameTime>()).GetSingleton<GlobalGameTime>();
            var time = globalTime.gameTime;
            var deltaTime = globalTime.frameDuration;

            // Update state
            Entities
                .ForEach((Entity entity, ref AnimSource.Data animSource, ref Settings settings, ref SystemState state,
                    ref AnimSource.AllowWrite allowWrite) =>
            {
                if (!characterInterpolatedDataFromEntity.HasComponent(animSource.animStateEntity))
                    return;

                var charInterpolatedState = characterInterpolatedDataFromEntity[animSource.animStateEntity];

                var abilityMovementEntity = Ability.FindAbility(ownedAbilityBufferFromEntity, animSource.animStateEntity, AbilityMovement.Tag);
                if (abilityMovementEntity == Entity.Null)
                    return;

                var abilityMovement = abilityInterpolatedStateFromEntity[abilityMovementEntity];

                if (allowWrite.FirstUpdate)
                {
                    // Do phase projection for time not spent in state
                    var ticksSincePreviousGroundMove = time.tick - charInterpolatedState.lastGroundMoveTick;
                    if (ticksSincePreviousGroundMove > 1)
                    {
                        // TODO: This relies on the state entity being persistent. If it is not, find another way
                        charInterpolatedState.locomotionPhase += state.PlaySpeed * (ticksSincePreviousGroundMove - 1f);
                    }

                    // Reset the phase if appropriate
                    var timeSincePreviousGroundMove = ticksSincePreviousGroundMove / (float)time.tickRate;
                    if (abilityMovement.previousCharLocoState != AbilityMovement.LocoState.GroundMove &&
                        timeSincePreviousGroundMove >  settings.stateResetWindow)
                    {
//                    Debug.Log("Reset movement sprint! (Ticks since: " + ticksSincePreviousGroundMove + " Time since: " + timeSincePreviousGroundMove + ")");
                        charInterpolatedState.locomotionPhase = 0f;
                    }

                    allowWrite.FirstUpdate = false;
                }

                charInterpolatedState.lastGroundMoveTick = time.tick;


                charInterpolatedState.rotation = charInterpolatedState.aimYaw;
                var moveAngleLocal = MathHelper.DeltaAngle(charInterpolatedState.rotation, charInterpolatedState.moveYaw);

                // Damp local move angle
                var speed = settings.changeDirSpeed * 1000f;
                var deltaAngle = deltaTime * speed;
                var diff = math.abs(MathHelper.DeltaAngle(charInterpolatedState.moveAngleLocal, moveAngleLocal));
                var t = deltaAngle >= diff ? 1.0f : deltaAngle / diff;

                var dampedAngle = MathHelper.LerpAngle(charInterpolatedState.moveAngleLocal + 180, moveAngleLocal + 180, t);

                while (dampedAngle > 360) dampedAngle -= 360;
                while (dampedAngle < 0) dampedAngle += 360;
                charInterpolatedState.moveAngleLocal = dampedAngle - 180;

                /*
                // If the clip duration of the blend space is variable, you would need something like this. Since
                bursting with node sets is currently not possible, you should defer message sending till out of the job.

                var blendParameter = new Parameter
                {
                    Id = settings.LocoBlendTreeAsset.Value.BlendParameter,
                    Value = charInterpolatedState.moveAngleLocal
                };

                nodeSet.SendMessage(state.BlendNode, BlendTree1DNode.SimulationPorts.Parameter, blendParameter);
                var f = nodeSet.GetFunctionality(state.BlendNode);
                var duration = f.GetDuration(state.BlendNode);
                */

                // Update phase
                state.PlaySpeed = 1f / state.RunClipDuration * deltaTime;
                charInterpolatedState.locomotionPhase += state.PlaySpeed;

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
                if (!EntityManager.HasComponent<SharedRigDefinition>(animSource.animStateEntity))
                    return;

                var sharedRigDef = EntityManager.GetSharedComponentData<SharedRigDefinition>(animSource.animStateEntity);
                var rig = sharedRigDef.Value;

                var aimClipInstance = ClipManager.Instance.GetClipFor(rig, settings.animAimDownToUp);
                var addRefPoseClipInstance = ClipManager.Instance.GetClipFor(rig, settings.additiveRefPose);

                nodeSet.SendMessage(state.AimClipNode, ClipNode.SimulationPorts.ClipInstance, aimClipInstance);
                nodeSet.SendMessage(state.AdditiveRefPose, ClipNode.SimulationPorts.ClipInstance, addRefPoseClipInstance);
                nodeSet.SendMessage(state.BlendNode, BlendTree1DNode.SimulationPorts.RigDefinition, rig);
                nodeSet.SendMessage(state.BlendNode, BlendTree1DNode.SimulationPorts.BlendTree, settings.LocoBlendTreeAsset);
                nodeSet.SendMessage(state.AimMixerNode, LayerMixerNode.SimulationPorts.RigDefinition, rig);
                nodeSet.SendMessage(state.AimDeltaNode, DeltaNode.SimulationPorts.RigDefinition, rig);

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

                // Blend the animations based on move direction
                var blendParameter = new Parameter {
                    Id = settings.LocoBlendTreeAsset.Value.BlendParameter,
                    Value = charInterpolatedState.moveAngleLocal
                };

                nodeSet.SendMessage(state.BlendNode, BlendTree1DNode.SimulationPorts.Parameter, blendParameter);

                // Set the phase of the animation
                // TODO: Remove fmod once/if blendtree uses loopable clip node
                var phase = math.fmod(charInterpolatedState.locomotionPhase, 1.0f);
                nodeSet.SetData(state.BlendNode, BlendTree1DNode.KernelPorts.NormalizedTime, phase);

                nodeSet.SetData(state.AimClipNode, ClipNode.KernelPorts.Time,
                    charInterpolatedState.aimPitch * state.AimClipDuration / 180f);
            }).Run();

            cmdBuffer.Playback(EntityManager);
            cmdBuffer.Dispose();

            return default;
        }
    }
}

