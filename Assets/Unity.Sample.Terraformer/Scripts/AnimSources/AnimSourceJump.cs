using System;
using Unity.Animation;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.DataFlowGraph;
using Unity.Sample.Core;
using UnityEngine;


public class AnimSourceJump
{
    public struct Settings : IComponentData
    {
        public BlobAssetReference<Clip> JumpClip;
        public BlobAssetReference<Clip> JumpAimVerticalClip;
        public BlobAssetReference<Clip> JumpAimHorizontalClip;
        public BlobAssetReference<Clip> AdditiveRefPose;

        public float jumpHeight; // Jump height of character in last frame of animation
        public float aimDuringReloadPitch;
        public float MaxHipOffset;
        public float HipDragSpeed;
    }

    public struct SystemState : ISystemStateComponentData
    {
        public static SystemState Default => new SystemState();

        public NodeHandle<ClipPlayerNode> JumpNode;
        public NodeHandle<DeltaTimeNode>      DeltaTimeNode;

        public NodeHandle<ClipNode> AimVerticalNode;
        public NodeHandle<ClipNode> AimHorizontalNode;
        public NodeHandle<ClipNode> AdditiveRefPoseA;
        public NodeHandle<ClipNode> AdditiveRefPoseB;
        public NodeHandle<DeltaNode> AimVerticalDelta;
        public NodeHandle<DeltaNode> AimHorizontalDelta;
        public NodeHandle<LayerMixerNode> MixerNode;

        public float AimVerticalDuration;
        public float AimHorizontalDuration;
        public float JumpDuration;
        public float CurrentRotationVel;

        public float PlaySpeed;
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

            var nodeSet = m_AnimationGraphSystem.Set;
            var commands = new EntityCommandBuffer(Allocator.TempJob);
            var ownedAbilityBufferFromEntity = GetBufferFromEntity<AbilityOwner.OwnedAbility>(true);

            // Handle created entities
            Entities
                .WithoutBurst() // Can be removed once NodeSets are Burst-friendly
                .WithNone<SystemState>()
                .ForEach((Entity entity, ref AnimSource.Data animSource, ref Settings settings) =>
            {
//                GameDebug.Log("Init Run");

                var state = SystemState.Default;

                var abilityMovementEntity = Ability.FindAbility(ownedAbilityBufferFromEntity, animSource.animStateEntity, AbilityMovement.Tag);
                if (abilityMovementEntity == Entity.Null)
                    return;

                var abilityMovementSettings = EntityManager.GetComponentData<AbilityMovement.Settings>(abilityMovementEntity);

                state.JumpNode = AnimationGraphHelper.CreateNode<ClipPlayerNode>(m_AnimationGraphSystem,"JumpNode");
                state.DeltaTimeNode = AnimationGraphHelper.CreateNode<DeltaTimeNode>(m_AnimationGraphSystem,"DeltaTimeNode");

                state.AimVerticalNode = AnimationGraphHelper.CreateNode<ClipNode>(m_AnimationGraphSystem,"AimVerticalNode");
                state.AimHorizontalNode = AnimationGraphHelper.CreateNode<ClipNode>(m_AnimationGraphSystem,"AimHorizontalNode");
                state.AdditiveRefPoseA = AnimationGraphHelper.CreateNode<ClipNode>(m_AnimationGraphSystem,"AdditiveRefPoseA");
                state.AdditiveRefPoseB = AnimationGraphHelper.CreateNode<ClipNode>(m_AnimationGraphSystem,"AdditiveRefPoseB");
                state.AimVerticalDelta = AnimationGraphHelper.CreateNode<DeltaNode>(m_AnimationGraphSystem,"AimVerticalDelta");
                state.AimHorizontalDelta = AnimationGraphHelper.CreateNode<DeltaNode>(m_AnimationGraphSystem,"AimHorizontalDelta");

                state.MixerNode = AnimationGraphHelper.CreateNode<LayerMixerNode>(m_AnimationGraphSystem,"MixerNode");

                nodeSet.SendMessage(state.JumpNode, ClipPlayerNode.SimulationPorts.Speed, 1.0f);

                nodeSet.SendMessage(state.MixerNode, LayerMixerNode.SimulationPorts.WeightInput0, 1f);
                nodeSet.SendMessage(state.MixerNode, LayerMixerNode.SimulationPorts.WeightInput1, 1f);
                nodeSet.SendMessage(state.MixerNode, LayerMixerNode.SimulationPorts.WeightInput2, 1f);
                nodeSet.SendMessage(state.MixerNode, LayerMixerNode.SimulationPorts.BlendModeInput1,
                    BlendingMode.Additive);
                nodeSet.SendMessage(state.MixerNode, LayerMixerNode.SimulationPorts.BlendModeInput2,
                    BlendingMode.Additive);

                nodeSet.Connect(state.DeltaTimeNode, DeltaTimeNode.KernelPorts.DeltaTime, state.JumpNode, ClipPlayerNode.KernelPorts.DeltaTime);
                nodeSet.Connect(state.JumpNode, ClipPlayerNode.KernelPorts.Output, state.MixerNode, LayerMixerNode.KernelPorts.Input0);

                nodeSet.Connect(state.AdditiveRefPoseA, ClipNode.KernelPorts.Output, state.AimVerticalDelta, DeltaNode.KernelPorts.Subtract);
                nodeSet.Connect(state.AdditiveRefPoseB, ClipNode.KernelPorts.Output, state.AimHorizontalDelta, DeltaNode.KernelPorts.Subtract);

                nodeSet.Connect(state.AimVerticalNode, ClipNode.KernelPorts.Output, state.AimVerticalDelta, DeltaNode.KernelPorts.Input);
                nodeSet.Connect(state.AimHorizontalNode, ClipNode.KernelPorts.Output, state.AimHorizontalDelta, DeltaNode.KernelPorts.Input);

                nodeSet.Connect(state.AimVerticalDelta, DeltaNode.KernelPorts.Output, state.MixerNode, LayerMixerNode.KernelPorts.Input1);
                nodeSet.Connect(state.AimHorizontalDelta, DeltaNode.KernelPorts.Output, state.MixerNode, LayerMixerNode.KernelPorts.Input2);

                // Store clip info
                state.AimVerticalDuration  = settings.JumpAimVerticalClip.Value.Duration;
                state.AimHorizontalDuration = settings.JumpAimHorizontalClip.Value.Duration;
                state.JumpDuration = settings.JumpClip.Value.Duration;

                // Adjust play speed so vertical velocity in animation is matched with character velocity (so feet doesnt penetrate ground)
                var animJumpVel = settings.jumpHeight / settings.JumpClip.Value.Duration;
                var characterJumpVel = abilityMovementSettings.jumpAscentHeight / abilityMovementSettings.jumpAscentDuration;

                if (characterJumpVel > 0f && animJumpVel > 0f)
                {
                    state.PlaySpeed = characterJumpVel / animJumpVel;
                }
                else
                {
                    GameDebug.LogWarning("Cannot set jump anim speed, values need to be more than 0");
                }

                // Expose input and outputs
                animSource.outputNode = state.MixerNode;
                animSource.outputPortID = (OutputPortID)LayerMixerNode.KernelPorts.Output;

                commands.AddComponent(entity, state);
            }).Run();

            // Handled deleted entities
            var animationGraphSystem = m_AnimationGraphSystem;
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
            AnimationGraphHelper.DestroyNode(animGraphSys,state.JumpNode);
            AnimationGraphHelper.DestroyNode(animGraphSys,state.DeltaTimeNode);
            AnimationGraphHelper.DestroyNode(animGraphSys,state.AimVerticalNode);
            AnimationGraphHelper.DestroyNode(animGraphSys,state.AimHorizontalNode);
            AnimationGraphHelper.DestroyNode(animGraphSys,state.AimVerticalDelta);
            AnimationGraphHelper.DestroyNode(animGraphSys,state.AimHorizontalDelta);
            AnimationGraphHelper.DestroyNode(animGraphSys,state.AdditiveRefPoseA);
            AnimationGraphHelper.DestroyNode(animGraphSys,state.AdditiveRefPoseB);
            AnimationGraphHelper.DestroyNode(animGraphSys,state.MixerNode);

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

            // TODO (mogensh) find cleaner way to get time
            var globalTime = m_GlobalGameTimeQuery.GetSingleton<GlobalGameTime>();
            var deltaTime = globalTime.frameDuration;

            var characterInterpolatedDataFromEntity = GetComponentDataFromEntity<Character.InterpolatedData>(false);

            // Update state
            Entities
                .ForEach((Entity entity, ref AnimSource.Data animSource, ref Settings settings, ref SystemState state, ref AnimSource.AllowWrite allowWrite) =>
            {
                if (!characterInterpolatedDataFromEntity.HasComponent(animSource.animStateEntity))
                    return;

                var charInterpolatedState = characterInterpolatedDataFromEntity[animSource.animStateEntity];

                if (allowWrite.FirstUpdate)
                {
                    charInterpolatedState.jumpTime = 0;
                    allowWrite.FirstUpdate = false;
                }
                else
                {
                    charInterpolatedState.jumpTime += state.PlaySpeed * deltaTime;
                }

                // Update rotation
                var hipDragSpeed = settings.HipDragSpeed;
                var maxHipOffset = settings.MaxHipOffset;

                // TODO: Make sure to test the behavior of this
                charInterpolatedState.rotation = MathHelper.SmoothDampAngle(charInterpolatedState.rotation,
                    charInterpolatedState.aimYaw, ref state.CurrentRotationVel, hipDragSpeed * -1 + 1, 1000f, deltaTime);

                // Clamp the hip offset
                var delta = MathHelper.DeltaAngle(charInterpolatedState.aimYaw, charInterpolatedState.rotation);

                if (math.abs(delta) > maxHipOffset)
                {
                    charInterpolatedState.rotation = charInterpolatedState.aimYaw + math.clamp(delta, -maxHipOffset, maxHipOffset);
                }

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

                var jumpClipInstance = ClipManager.Instance.GetClipFor(rig, settings.JumpClip);
                var runVerticalAimClipInstance = ClipManager.Instance.GetClipFor(rig, settings.JumpAimVerticalClip);
                var runAimHorizontalClipInstance = ClipManager.Instance.GetClipFor(rig, settings.JumpAimHorizontalClip);
                var addRefPoseClipInstance = ClipManager.Instance.GetClipFor(rig, settings.AdditiveRefPose);

                nodeSet.SendMessage(state.JumpNode, ClipPlayerNode.SimulationPorts.ClipInstance, jumpClipInstance);
                nodeSet.SendMessage(state.AimVerticalNode, ClipNode.SimulationPorts.ClipInstance, runVerticalAimClipInstance);
                nodeSet.SendMessage(state.AimHorizontalNode, ClipNode.SimulationPorts.ClipInstance, runAimHorizontalClipInstance);

                nodeSet.SendMessage(state.AdditiveRefPoseA, ClipNode.SimulationPorts.ClipInstance, addRefPoseClipInstance);
                nodeSet.SendMessage(state.AdditiveRefPoseB, ClipNode.SimulationPorts.ClipInstance, addRefPoseClipInstance);

                nodeSet.SendMessage(state.MixerNode, LayerMixerNode.SimulationPorts.RigDefinition, rig);
                nodeSet.SendMessage(state.JumpNode, ClipPlayerNode.SimulationPorts.Speed, state.PlaySpeed);

                nodeSet.SendMessage(state.AimVerticalDelta, DeltaNode.SimulationPorts.RigDefinition, rig);
                nodeSet.SendMessage(state.AimHorizontalDelta, DeltaNode.SimulationPorts.RigDefinition, rig);

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

                nodeSet.SetData(state.AimVerticalNode, ClipNode.KernelPorts.Time,
                    charInterpolatedState.aimPitch * state.AimVerticalDuration / 180f);
                var timeFactor = state.AimHorizontalDuration / 180f;
                var time = (90f + MathHelper.DeltaAngle(charInterpolatedState.aimYaw, charInterpolatedState.rotation)) * timeFactor;

                time = math.max(0f, time);
                nodeSet.SetData(state.AimHorizontalNode, ClipNode.KernelPorts.Time, time);

                // Blend in/out aim for reload
                nodeSet.SendMessage(state.MixerNode, LayerMixerNode.SimulationPorts.WeightInput2,
                    1f - charInterpolatedState.blendOutAim * (1f - settings.aimDuringReloadPitch));

                nodeSet.SendMessage(state.JumpNode, ClipPlayerNode.SimulationPorts.Time, charInterpolatedState.jumpTime);
            }).Run();

            cmdBuffer.Playback(EntityManager);
            cmdBuffer.Dispose();

            return default;
        }
    }
}

