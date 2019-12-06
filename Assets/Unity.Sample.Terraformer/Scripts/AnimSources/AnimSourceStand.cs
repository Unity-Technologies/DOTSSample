using System;
using Unity.Animation;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.DataFlowGraph;
using Unity.Sample.Core;
using Unity.Mathematics;
using UnityEngine;

public class AnimSourceStand
{
    public struct Settings : IComponentData
    {
        public BlobAssetReference<Clip> StandClip;
        public BlobAssetReference<Clip> TurnLeftClip;
        public BlobAssetReference<Clip> TurnRightClip;
        public BlobAssetReference<Clip> StandAimLeftClip;
        public BlobAssetReference<Clip> StandAimMidClip;
        public BlobAssetReference<Clip> StandAimRightClip;
        public BlobAssetReference<Clip> AdditiveRefPoseClip;

        public float animTurnAngle;
        public float aimTurnLocalThreshold;
        public float turnSpeed;
        public float turnThreshold;
        public float turnTransitionSpeed;
        public float aimDuringReloadPitch;
        public float aimDuringReloadYaw;
    }


    public struct SystemState : ISystemStateComponentData
    {
        public static SystemState Default => new SystemState();

        public NodeHandle<UberClipNode> Idle;
        public NodeHandle<DeltaTimeNode> DeltaTimeNode;
        public NodeHandle<TimeCounterNode> TimeCounterNode;

        public NodeHandle<ClipNode> TurnL;
        public NodeHandle<ClipNode> TurnR;

        public NodeHandle<ClipNode> AimLeft;
        public NodeHandle<ClipNode> AimMid;
        public NodeHandle<ClipNode> AimRight;

        public NodeHandle<ClipNode> AdditiveRefPose;

        public NodeHandle<DeltaNode> AimLeftDelta;
        public NodeHandle<DeltaNode> AimMidDelta;
        public NodeHandle<DeltaNode> AimRightDelta;

        public NodeHandle<MixerNode> MixerLeft;
        public NodeHandle<MixerNode> MixerRight;
        public NodeHandle<LayerMixerNode> AimMixer;

        public float AimDuration;
        public float TurnDuration;
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
                GameDebug.Log(World,AnimSource.ShowLifetime,"Init Stand entity:{0} state entity:{1}", entity, animSource.animStateEntity);

                var state = SystemState.Default;

                state.Idle = AnimationGraphHelper.CreateNode<UberClipNode>(animationGraphSystem, "Idle");
                state.DeltaTimeNode = AnimationGraphHelper.CreateNode<DeltaTimeNode>(animationGraphSystem, "DeltaTimeNode");
                state.TimeCounterNode = AnimationGraphHelper.CreateNode<TimeCounterNode>(animationGraphSystem, "TimeCounterNode");

                state.TurnL = AnimationGraphHelper.CreateNode<ClipNode>(animationGraphSystem, "TurnL");
                state.TurnR = AnimationGraphHelper.CreateNode<ClipNode>(animationGraphSystem, "TurnR");

                state.AimLeft = AnimationGraphHelper.CreateNode<ClipNode>(animationGraphSystem, "AimLeft");
                state.AimMid = AnimationGraphHelper.CreateNode<ClipNode>(animationGraphSystem, "AimMid");
                state.AimRight = AnimationGraphHelper.CreateNode<ClipNode>(animationGraphSystem, "AimRight");

                state.AdditiveRefPose = AnimationGraphHelper.CreateNode<ClipNode>(animationGraphSystem, "AdditiveRefPose");

                state.AimLeftDelta = AnimationGraphHelper.CreateNode<DeltaNode>(animationGraphSystem, "AimLeftDelta");
                state.AimMidDelta = AnimationGraphHelper.CreateNode<DeltaNode>(animationGraphSystem, "AimMidDelta");
                state.AimRightDelta = AnimationGraphHelper.CreateNode<DeltaNode>(animationGraphSystem, "AimRightDelta");

                state.MixerLeft = AnimationGraphHelper.CreateNode<MixerNode>(animationGraphSystem, "MixerLeft");
                state.MixerRight = AnimationGraphHelper.CreateNode<MixerNode>(animationGraphSystem, "MixerRight");
                state.AimMixer = AnimationGraphHelper.CreateNode<LayerMixerNode>(animationGraphSystem, "AimMixer");

                nodeSet.SendMessage(state.AimMixer, LayerMixerNode.SimulationPorts.WeightInput0, 1f);
                nodeSet.SendMessage(state.AimMixer, LayerMixerNode.SimulationPorts.BlendModeInput1, BlendingMode.Additive);
                nodeSet.SendMessage(state.AimMixer, LayerMixerNode.SimulationPorts.BlendModeInput2, BlendingMode.Additive);
                nodeSet.SendMessage(state.AimMixer, LayerMixerNode.SimulationPorts.BlendModeInput3, BlendingMode.Additive);

                nodeSet.SendMessage(state.Idle, UberClipNode.SimulationPorts.Configuration, new ClipConfiguration { Mask = (int)ClipConfigurationMask.LoopTime });

                // TODO: Convert to using cascade mixer or blend space?
                nodeSet.Connect(state.DeltaTimeNode, DeltaTimeNode.KernelPorts.DeltaTime, state.TimeCounterNode, TimeCounterNode.KernelPorts.DeltaTime);
                nodeSet.Connect(state.TimeCounterNode, TimeCounterNode.KernelPorts.Time, state.Idle, UberClipNode.KernelPorts.Time);

                nodeSet.Connect(state.TurnL, ClipNode.KernelPorts.Output, state.MixerLeft , MixerNode.KernelPorts.Input0);
                nodeSet.Connect(state.Idle, UberClipNode.KernelPorts.Output, state.MixerLeft , MixerNode.KernelPorts.Input1);

                nodeSet.Connect(state.MixerLeft , MixerNode.KernelPorts.Output, state.MixerRight, MixerNode.KernelPorts.Input0);
                nodeSet.Connect(state.TurnR, ClipNode.KernelPorts.Output, state.MixerRight, MixerNode.KernelPorts.Input1);

                nodeSet.Connect(state.MixerRight, MixerNode.KernelPorts.Output, state.AimMixer, LayerMixerNode.KernelPorts.Input0);

                nodeSet.Connect(state.AimLeft, ClipNode.KernelPorts.Output, state.AimLeftDelta, DeltaNode.KernelPorts.Input);
                nodeSet.Connect(state.AimMid, ClipNode.KernelPorts.Output, state.AimMidDelta, DeltaNode.KernelPorts.Input);
                nodeSet.Connect(state.AimRight, ClipNode.KernelPorts.Output, state.AimRightDelta, DeltaNode.KernelPorts.Input);

                nodeSet.Connect(state.AdditiveRefPose, ClipNode.KernelPorts.Output, state.AimLeftDelta, DeltaNode.KernelPorts.Subtract);
                nodeSet.Connect(state.AdditiveRefPose, ClipNode.KernelPorts.Output, state.AimMidDelta, DeltaNode.KernelPorts.Subtract);
                nodeSet.Connect(state.AdditiveRefPose, ClipNode.KernelPorts.Output, state.AimRightDelta, DeltaNode.KernelPorts.Subtract);

                nodeSet.Connect(state.AimLeftDelta, DeltaNode.KernelPorts.Output, state.AimMixer, LayerMixerNode.KernelPorts.Input1);
                nodeSet.Connect(state.AimMidDelta, DeltaNode.KernelPorts.Output, state.AimMixer, LayerMixerNode.KernelPorts.Input2);
                nodeSet.Connect(state.AimRightDelta, DeltaNode.KernelPorts.Output, state.AimMixer, LayerMixerNode.KernelPorts.Input3);

                // TODO: Use pr. clip values (turn direction)?
                state.TurnDuration = settings.TurnLeftClip.Value.Duration;
                state.AimDuration = settings.StandAimLeftClip.Value.Duration;

                // Setup transition data
                var numMixerPorts = 3;
                for (var i = 0; i < numMixerPorts; i++)
                {
                    var transitionWeights = EntityManager.GetBuffer<SimpleTransition.PortWeights>(entity);
                    transitionWeights.Add(new SimpleTransition.PortWeights { Value = 1f });
                }

                // Expose input and outputs
                animSource.outputNode = state.AimMixer;
                animSource.outputPortID = (OutputPortID)LayerMixerNode.KernelPorts.Output;

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

        static void Deinitialize(World world, EntityCommandBuffer cmdBuffer, Entity entity, AnimationGraphSystem animGraphSys, SystemState state)
        {
            GameDebug.Log(world,AnimSource.ShowLifetime,"Deinit Stand entity:{0}", entity);

            AnimationGraphHelper.DestroyNode(animGraphSys,state.Idle);
            AnimationGraphHelper.DestroyNode(animGraphSys,state.DeltaTimeNode);
            AnimationGraphHelper.DestroyNode(animGraphSys,state.TimeCounterNode);

            AnimationGraphHelper.DestroyNode(animGraphSys,state.TurnL);
            AnimationGraphHelper.DestroyNode(animGraphSys,state.TurnR);

            AnimationGraphHelper.DestroyNode(animGraphSys,state.AimLeft);
            AnimationGraphHelper.DestroyNode(animGraphSys,state.AimMid);
            AnimationGraphHelper.DestroyNode(animGraphSys,state.AimRight);

            AnimationGraphHelper.DestroyNode(animGraphSys,state.AdditiveRefPose);

            AnimationGraphHelper.DestroyNode(animGraphSys,state.AimLeftDelta);
            AnimationGraphHelper.DestroyNode(animGraphSys,state.AimMidDelta);
            AnimationGraphHelper.DestroyNode(animGraphSys,state.AimRightDelta);

            AnimationGraphHelper.DestroyNode(animGraphSys,state.MixerLeft);
            AnimationGraphHelper.DestroyNode(animGraphSys,state.MixerRight);
            AnimationGraphHelper.DestroyNode(animGraphSys,state.AimMixer);

            cmdBuffer.RemoveComponent(entity, typeof(SystemState));
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

            // TODO (mogensh) find cleaner way to get time
            var globalTime = GetEntityQuery(ComponentType.ReadOnly<GlobalGameTime>()).GetSingleton<GlobalGameTime>();
            var deltaTime = globalTime.frameDuration;

            var characterInterpolatedDataFromEntity = GetComponentDataFromEntity<Character.InterpolatedData>(false);

            // Update state
            Entities
                .ForEach((Entity entity, ref AnimSource.Data animSource, ref Settings settings, ref SystemState state,
                    ref AnimSource.AllowWrite allowWrite) =>
            {
                if (!characterInterpolatedDataFromEntity.HasComponent(animSource.animStateEntity))
                {
                    //GameDebug.LogWarning(World,"AnimSource does not have Character.InterpolatedData components. Has it been deleted?");
                    return;
                }

                var charInterpolatedState = characterInterpolatedDataFromEntity[animSource.animStateEntity];

                if (allowWrite.FirstUpdate)
                {
                    charInterpolatedState.turnDirection = 0;
                    charInterpolatedState.rotation = charInterpolatedState.aimYaw;
                    charInterpolatedState.turnStartAngle = charInterpolatedState.aimYaw;
                    allowWrite.FirstUpdate = false;
                }

                var aimYawLocal = MathHelper.DeltaAngle(charInterpolatedState.rotation, charInterpolatedState.aimYaw);
                var absAimYawLocal = math.abs(aimYawLocal);

                // Non turning update
                if (charInterpolatedState.turnDirection == 0)
                {
                    // Test for local yaw angle exeding threshold so we need to turn
                    if (absAimYawLocal > settings.aimTurnLocalThreshold) // TODO: Document why we need local vs non local turn tolerances?
                    {
                        charInterpolatedState.turnStartAngle = charInterpolatedState.rotation;
                        charInterpolatedState.turnDirection = (short)(aimYawLocal >= 0F ? 1F : -1F);
                    }
                }
                else
                {
                    var rotateAngleRemaining = MathHelper.DeltaAngle(charInterpolatedState.rotation,
                        charInterpolatedState.turnStartAngle) + settings.animTurnAngle * charInterpolatedState.turnDirection;

                    if (rotateAngleRemaining * charInterpolatedState.turnDirection <= 0)
                    {
                        charInterpolatedState.turnDirection = 0;
                    }
                    else
                    {
                        var turnSpeed = settings.turnSpeed;
                        if (absAimYawLocal > settings.turnThreshold)
                        {
                            var factor = 1.0f - (180 - absAimYawLocal) / settings.turnThreshold;
                            turnSpeed = turnSpeed + factor * 300;
                        }

                        var deltaAngle = deltaTime * turnSpeed;
                        var absAngleRemaining = math.abs(rotateAngleRemaining);
                        if (deltaAngle > absAngleRemaining)
                        {
                            deltaAngle = absAngleRemaining;
                        }

                        // TODO: Is regular sign that returns 0 for value 0 ok here?
                        var sign = (rotateAngleRemaining >= 0F ? 1F : -1F);

                        charInterpolatedState.rotation += sign * deltaAngle;
                        while (charInterpolatedState.rotation > 360.0f)
                            charInterpolatedState.rotation -= 360.0f;
                        while (charInterpolatedState.rotation < 0.0f)
                            charInterpolatedState.rotation += 360.0f;
                    }
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

            var globalTime = GetEntityQuery(ComponentType.ReadOnly<GlobalGameTime>()).GetSingleton<GlobalGameTime>();
            var deltaTime = globalTime.frameDuration;

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

                var standClipInstance = ClipManager.Instance.GetClipFor(rig, settings.StandClip);
                var turnLeftClipInstance = ClipManager.Instance.GetClipFor(rig, settings.TurnLeftClip);
                var turnRightClipInstance = ClipManager.Instance.GetClipFor(rig, settings.TurnRightClip);
                var standAimLeftClipInstance = ClipManager.Instance.GetClipFor(rig, settings.StandAimLeftClip);
                var standAimMidClipInstance = ClipManager.Instance.GetClipFor(rig, settings.StandAimMidClip);
                var standAimRightClipInstance = ClipManager.Instance.GetClipFor(rig, settings.StandAimRightClip);
                var addRefPoseClipInstance = ClipManager.Instance.GetClipFor(rig, settings.AdditiveRefPoseClip);

                nodeSet.SendMessage(state.Idle, UberClipNode.SimulationPorts.ClipInstance, standClipInstance);
                nodeSet.SendMessage(state.TurnL, ClipNode.SimulationPorts.ClipInstance, turnLeftClipInstance);
                nodeSet.SendMessage(state.TurnR, ClipNode.SimulationPorts.ClipInstance, turnRightClipInstance);
                nodeSet.SendMessage(state.AimLeft, ClipNode.SimulationPorts.ClipInstance, standAimLeftClipInstance);
                nodeSet.SendMessage(state.AimMid, ClipNode.SimulationPorts.ClipInstance, standAimMidClipInstance);
                nodeSet.SendMessage(state.AimRight, ClipNode.SimulationPorts.ClipInstance, standAimRightClipInstance);
                nodeSet.SendMessage(state.AdditiveRefPose, ClipNode.SimulationPorts.ClipInstance, addRefPoseClipInstance);

                nodeSet.SendMessage(state.AimLeftDelta, DeltaNode.SimulationPorts.RigDefinition, rig);
                nodeSet.SendMessage(state.AimMidDelta, DeltaNode.SimulationPorts.RigDefinition, rig);
                nodeSet.SendMessage(state.AimRightDelta, DeltaNode.SimulationPorts.RigDefinition, rig);
                nodeSet.SendMessage(state.MixerLeft, MixerNode.SimulationPorts.RigDefinition, rig);
                nodeSet.SendMessage(state.MixerRight, MixerNode.SimulationPorts.RigDefinition, rig);
                nodeSet.SendMessage(state.AimMixer, LayerMixerNode.SimulationPorts.RigDefinition, rig);

                cmdBuffer.AddComponent<AnimSource.HasValidRig>(entity);
            }).Run();

            // Apply state
            Entities
                .WithoutBurst() // Can be removed once NodeSets are Burst-friendly
                .ForEach((Entity entity, ref AnimSource.Data animSource, ref Settings settings, ref SystemState state) =>
            {
                if (!EntityManager.HasComponent<Character.InterpolatedData>(animSource.animStateEntity))
                {
                    return;
                }

                var charInterpolatedState = EntityManager.GetComponentData<Character.InterpolatedData>(animSource.animStateEntity);

                // Handle turning
                float rotateAngleRemaining = 0f;
                if (charInterpolatedState.turnDirection != 0)
                    rotateAngleRemaining = MathHelper.DeltaAngle(charInterpolatedState.rotation,
                        charInterpolatedState.turnStartAngle) + settings.animTurnAngle * charInterpolatedState.turnDirection;

                var portWeights = EntityManager.GetBuffer<SimpleTransition.PortWeights>(entity).AsNativeArray();

                if (charInterpolatedState.turnDirection == 0)
                {
                    SimpleTransition.Update((int)LocoMixerPort.Idle, settings.turnTransitionSpeed, deltaTime, ref portWeights);
                }
                else
                {
                    var fraction = 1f - math.abs(rotateAngleRemaining / settings.animTurnAngle);
                    var anim = (charInterpolatedState.turnDirection == -1) ? state.TurnL : state.TurnR;
                    var mixerPort = (charInterpolatedState.turnDirection == -1) ? (int)LocoMixerPort.TurnL : (int)LocoMixerPort.TurnR;

                    SimpleTransition.Update(mixerPort, settings.turnTransitionSpeed, deltaTime, ref portWeights);

                    nodeSet.SetData(anim, ClipNode.KernelPorts.Time,  state.TurnDuration * fraction);

                    // Reset the time of the idle, so it's reset when we transition back
                    if (portWeights[(int)LocoMixerPort.Idle].Value < 0.01f)
                        nodeSet.SendMessage(state.TimeCounterNode, TimeCounterNode.SimulationPorts.Time, 0f);
                }

                var idleWeight = portWeights[(int)LocoMixerPort.Idle].Value;
                nodeSet.SendMessage(state.MixerLeft, MixerNode.SimulationPorts.Blend, idleWeight);

                var turnRightWeight = portWeights[(int)LocoMixerPort.TurnR].Value;
                nodeSet.SendMessage(state.MixerRight, MixerNode.SimulationPorts.Blend, turnRightWeight);

                // Handle Aim
                var aimMultiplier = 1f - charInterpolatedState.blendOutAim;
                float aimPitchFraction = charInterpolatedState.aimPitch / 180.0f;

//                var aimPitchMult = Mathf.Lerp(m_template.blendOutAimOnReloadPitch, 1f, aimMultiplier);
                var aimPitchMult = math.lerp(settings.aimDuringReloadPitch, 1f, aimMultiplier);
                aimPitchFraction = math.lerp(0.5f, aimPitchFraction, aimPitchMult);
                float aimTime = aimPitchFraction * state.AimDuration;
                nodeSet.SetData(state.AimLeft, ClipNode.KernelPorts.Time, aimTime);
                nodeSet.SetData(state.AimMid, ClipNode.KernelPorts.Time, aimTime);
                nodeSet.SetData(state.AimRight, ClipNode.KernelPorts.Time, aimTime);

                float aimYawLocal = MathHelper.DeltaAngle(charInterpolatedState.rotation, charInterpolatedState.aimYaw);
//                float aimYawFraction = Mathf.Abs(aimYawLocal / settings.aimYawAngle);
                float aimYawFraction = math.abs(aimYawLocal / 90f);

                var aimYawMult = math.lerp(settings.aimDuringReloadYaw, 1f, aimMultiplier);
                aimYawFraction = math.lerp(0.0f, aimYawFraction, aimYawMult);

                nodeSet.SendMessage(state.AimMixer, LayerMixerNode.SimulationPorts.WeightInput2, 1.0f - aimYawFraction);
                if (aimYawLocal < 0)
                {
                    nodeSet.SendMessage(state.AimMixer, LayerMixerNode.SimulationPorts.WeightInput1, aimYawFraction);
                    nodeSet.SendMessage(state.AimMixer, LayerMixerNode.SimulationPorts.WeightInput3, 0.0f);
                }
                else
                {
                    nodeSet.SendMessage(state.AimMixer, LayerMixerNode.SimulationPorts.WeightInput1, 0.0f);
                    nodeSet.SendMessage(state.AimMixer, LayerMixerNode.SimulationPorts.WeightInput3, aimYawFraction);
                }
            }).Run();

            cmdBuffer.Playback(EntityManager);
            cmdBuffer.Dispose();

            return default;
        }
    }


    public struct SimpleTransition
    {
        public struct PortWeights : IBufferElementData
        {
            public float Value;
        }

        public static void Update(int activePort, float blendVelocity, float deltaTime, ref NativeArray<PortWeights> portWeights)
        {
            // Update current state weight
            float weight = portWeights[activePort].Value;
            if (weight != 1.0f)
            {
                weight = math.clamp(weight + blendVelocity * deltaTime, 0, 1);
                portWeights[activePort] = new PortWeights { Value = weight };
            }

            // Adjust weight of other states and ensure total weight is 1
            float weightLeft = 1.0f - weight;
            float totalWeight = 0;

            for (var i = 0; i < portWeights.Length; i++)
            {
                if (i == activePort)
                    continue;

                totalWeight += portWeights[i].Value;
            }

            if (totalWeight == 0)
                return;

            float fraction = weightLeft / totalWeight;
            for (int i = 0; i < portWeights.Length; i++)
            {
                if (i == activePort)
                    continue;

                float w = portWeights[i].Value;
                w = w * fraction;
                portWeights[i] = new PortWeights { Value = w };
            }
        }
    }

    enum LocoMixerPort
    {
        TurnL,
        Idle,
        TurnR,
        Count
    }
}
