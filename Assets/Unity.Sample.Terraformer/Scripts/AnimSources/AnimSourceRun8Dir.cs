using System;
using Unity.Animation;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.DataFlowGraph;
using Unity.Sample.Core;
using UnityEngine;
using UnityEngine.Profiling;


public class AnimSourceRun8Dir
{

    public struct Settings : IComponentData
    {
        public BlobAssetReference<BlendTree2DSimpleDirectionnal> RunBlendSpace2D;
        public BlobAssetReference<Clip> RunAimClipRef;
        public BlobAssetReference<Clip> RunAimHorizontalClipRef;
        public BlobAssetReference<Clip> AdditiveRefPose;

        [Range(0f, 90f)] public float MaxHipOffset;
        [Range(0f, 1f)] public float HipDragSpeed;

        public float damping;
        public float maxStep;

        [Range(0f, 1f)]
        public float StateResetWindow;

        [Range(0, 1)]
        public float blendOutAimOnReloadPitch;
    }

    public struct SystemState : ISystemStateComponentData
    {
        public static SystemState Default => new SystemState();

        public NodeHandle<ClipNode> AimVertical;
        public NodeHandle<ClipNode> AimHorizontal;
        public NodeHandle<ClipNode> AdditiveRefPoseA;
        public NodeHandle<ClipNode> AdditiveRefPoseB;

        public NodeHandle<DeltaNode> AimVerticalDelta;
        public NodeHandle<DeltaNode> AimHorizontalDelta;
        public NodeHandle<LayerMixerNode> Mixer;

        public NodeHandle<BlendTree2DNode> BlendTreeNode;

        public float AimVerticalDuration;
        public float AimHorizontalDuration;
        public float RunClipDuration;

        public float CurrentRotationVel;
        public float PlaySpeed;
        public float2 CurrentVelocity;
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
                GameDebug.Log(World,AnimSource.ShowLifetime,"Init Run8Dir entity:{0} state entity:{1}", entity, animSource.animStateEntity);

                var state = SystemState.Default;

                settings.RunBlendSpace2D = BlendTreeEntityStoreHelper.CreateBlendTree2DFromComponents(EntityManager, entity);

                state.BlendTreeNode = AnimationGraphHelper.CreateNode<BlendTree2DNode>(animationGraphSystem, "BlendTreeNode");

                state.AimVertical = AnimationGraphHelper.CreateNode<ClipNode>(animationGraphSystem, "AimVertical");
                state.AimHorizontal = AnimationGraphHelper.CreateNode<ClipNode>(animationGraphSystem, "AimHorizontal");
                state.AdditiveRefPoseA = AnimationGraphHelper.CreateNode<ClipNode>(animationGraphSystem,"AdditiveRefPoseA");
                state.AdditiveRefPoseB = AnimationGraphHelper.CreateNode<ClipNode>(animationGraphSystem, "AdditiveRefPoseB");
                state.AimVerticalDelta = AnimationGraphHelper.CreateNode<DeltaNode>(animationGraphSystem, "AimVerticalDelta");
                state.AimHorizontalDelta = AnimationGraphHelper.CreateNode<DeltaNode>(animationGraphSystem, "AimHorizontalDelta");
                state.Mixer = AnimationGraphHelper.CreateNode<LayerMixerNode>(animationGraphSystem, "Mixer");

                nodeSet.SendMessage(state.Mixer, LayerMixerNode.SimulationPorts.WeightInput0, 1f);
                nodeSet.SendMessage(state.Mixer, LayerMixerNode.SimulationPorts.WeightInput1, 1f);
                nodeSet.SendMessage(state.Mixer, LayerMixerNode.SimulationPorts.WeightInput2, 0f);
                nodeSet.SendMessage(state.Mixer, LayerMixerNode.SimulationPorts.BlendModeInput1, BlendingMode.Additive);
                nodeSet.SendMessage(state.Mixer, LayerMixerNode.SimulationPorts.BlendModeInput2, BlendingMode.Additive);

                nodeSet.Connect(state.BlendTreeNode, BlendTree2DNode.KernelPorts.Output, state.Mixer, LayerMixerNode.KernelPorts.Input0);

                nodeSet.Connect(state.AdditiveRefPoseA, ClipNode.KernelPorts.Output, state.AimVerticalDelta, DeltaNode.KernelPorts.Subtract);
                nodeSet.Connect(state.AdditiveRefPoseB, ClipNode.KernelPorts.Output, state.AimHorizontalDelta, DeltaNode.KernelPorts.Subtract);

                nodeSet.Connect(state.AimVertical, ClipNode.KernelPorts.Output, state.AimVerticalDelta, DeltaNode.KernelPorts.Input);
                nodeSet.Connect(state.AimHorizontal, ClipNode.KernelPorts.Output, state.AimHorizontalDelta, DeltaNode.KernelPorts.Input);

                nodeSet.Connect(state.AimVerticalDelta, DeltaNode.KernelPorts.Output, state.Mixer, LayerMixerNode.KernelPorts.Input1);
                nodeSet.Connect(state.AimHorizontalDelta, DeltaNode.KernelPorts.Output, state.Mixer, LayerMixerNode.KernelPorts.Input2);

                // Load clips and store clip info
                state.AimVerticalDuration = settings.RunAimClipRef.Value.Duration;
                state.AimHorizontalDuration = settings.RunAimClipRef.Value.Duration;

                // All clips are the same length so we can use a static clip duration
                state.RunClipDuration = settings.RunBlendSpace2D.Value.Motions[0].Clip.Value.Duration;

                // Expose input and outputs
                animSource.outputNode = state.Mixer;
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

        static void Deinitialize(World world, EntityCommandBuffer cmdBuffer, Entity entity, AnimationGraphSystem animGraphSys, SystemState state)
        {
            GameDebug.Log(world,AnimSource.ShowLifetime,"Deinit Run8Dir entity:{0}", entity);

            AnimationGraphHelper.DestroyNode(animGraphSys,state.AimVertical);
            AnimationGraphHelper.DestroyNode(animGraphSys,state.AimHorizontal);
            AnimationGraphHelper.DestroyNode(animGraphSys,state.AimVerticalDelta);
            AnimationGraphHelper.DestroyNode(animGraphSys,state.AimHorizontalDelta);
            AnimationGraphHelper.DestroyNode(animGraphSys,state.AdditiveRefPoseA);
            AnimationGraphHelper.DestroyNode(animGraphSys,state.AdditiveRefPoseB);
            AnimationGraphHelper.DestroyNode(animGraphSys,state.Mixer);
            AnimationGraphHelper.DestroyNode(animGraphSys,state.BlendTreeNode);

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

            var nodeSet = World.GetExistingSystem<AnimationGraphSystem>().Set;

            // TODO (mogensh) find cleaner way to get time
            var globalTime = m_GlobalGameTimeQuery.GetSingleton<GlobalGameTime>();
            var time = globalTime.gameTime;
            var deltaTime = globalTime.frameDuration;

            var characterInterpolatedDataFromEntity = GetComponentDataFromEntity<Character.InterpolatedData>(false);
            var abilityInterpolatedStateFromEntity = GetComponentDataFromEntity<AbilityMovement.InterpolatedState>(true);
            var ownedAbilityBufferFromEntity = GetBufferFromEntity<AbilityOwner.OwnedAbility>(true);

            // Update state
            Entities
                .ForEach((Entity entity, ref AnimSource.Data animSource,
                    ref Settings settings, ref SystemState state, ref AnimSource.AllowWrite allowWrite) =>
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
                        charInterpolatedState.locomotionPhase += state.PlaySpeed * (ticksSincePreviousGroundMove - 1f);
                    }

                    // Reset the phase and position in blend space if appropriate
                    var timeSincePreviousGroundMove = ticksSincePreviousGroundMove / (float)time.tickRate;
                    if (abilityMovement.previousCharLocoState != AbilityMovement.LocoState.GroundMove && timeSincePreviousGroundMove >  settings.StateResetWindow)
                    {
//                    Debug.Log("Reset movement run! (Ticks since: " + ticksSincePreviousGroundMove + " Time since: " + timeSincePreviousGroundMove + ")");
                        charInterpolatedState.locomotionPhase = 0f;
                        charInterpolatedState.moveAngleLocal = CalculateMoveAngleLocal(charInterpolatedState.aimYaw, charInterpolatedState.moveYaw);

//                        if (m_settings.useVariableMoveSpeed)
//                        {
//                            charInterpolatedState.locomotionVector = AngleToPosition(animState.moveAngleLocal) * charState.velocity.magnitude;
//                        }
//                        else
//                        {
                            charInterpolatedState.locomotionVector = AngleToPosition(charInterpolatedState.moveAngleLocal);
//                        }
                        state.CurrentVelocity = Vector2.zero;

                        allowWrite.FirstUpdate = false;
                    }
                }

                charInterpolatedState.lastGroundMoveTick = time.tick;

                // Update rotation
//                var hipDragSpeed = settings.HipDragSpeed;
//                var maxHipOffset = settings.MaxHipOffset;

//                charInterpolatedState.rotation = Mathf.SmoothDampAngle(charInterpolatedState.rotation,
//                    charInterpolatedState.aimYaw, ref state.CurrentRotationVel, hipDragSpeed * -1 + 1, 1000f, deltaTime);

                // Clamp the hip offset
//                var delta = Mathf.DeltaAngle(charInterpolatedState.aimYaw, charInterpolatedState.rotation);
//                if (Mathf.Abs(delta) > maxHipOffset)
//                {
//                    charInterpolatedState.rotation = charInterpolatedState.aimYaw + Mathf.Clamp(delta, -maxHipOffset, maxHipOffset);
//                }

                // Get new local move angle
//                var angle = Mathf.DeltaAngle(charInterpolatedState.aimYaw, charInterpolatedState.moveYaw);
                charInterpolatedState.rotation = charInterpolatedState.aimYaw;
                charInterpolatedState.moveAngleLocal = CalculateMoveAngleLocal(charInterpolatedState.rotation, charInterpolatedState.moveYaw);

                var targetBlend = AngleToPosition(charInterpolatedState.moveAngleLocal);
//                if (m_settings.useVariableMoveSpeed) // Experimental
//                {
//                    targetBlend = AngleToPosition(animState.moveAngleLocal) * charState.velocity.magnitude;
//                }

                charInterpolatedState.locomotionVector = MathHelper.SmoothDamp(charInterpolatedState.locomotionVector, targetBlend,
                    ref state.CurrentVelocity, settings.damping, settings.maxStep, deltaTime);

                /*
                // If the clip duration of the blend space is variable, you would need something like this. Since
                // bursting with node sets is currently not possible, you should defer message sending till out of the job.
                var blendParamX = new Parameter {
                    Id = settings.RunBlendSpace2D.Value.BlendParameterX,
                    Value = charInterpolatedState.locomotionVector.x
                };

                var blendParamY = new Parameter {
                    Id = settings.RunBlendSpace2D.Value.BlendParameterY,
                    Value = charInterpolatedState.locomotionVector.y
                };

                nodeSet.SendMessage(state.BlendTreeNode, BlendTree2DNode.SimulationPorts.Parameter, blendParamX);
                nodeSet.SendMessage(state.BlendTreeNode, BlendTree2DNode.SimulationPorts.Parameter, blendParamY);

                state.BlendParamSet = true;

                var f = nodeSet.GetFunctionality(state.BlendTreeNode);
                var duration = f.GetDuration(state.BlendTreeNode);
                */

                // Update phase
                state.PlaySpeed = 1f / state.RunClipDuration * deltaTime;
                charInterpolatedState.locomotionPhase += state.PlaySpeed;


                characterInterpolatedDataFromEntity[animSource.animStateEntity] = charInterpolatedState;
            }).Run();

            return default;
        }

        static float CalculateMoveAngleLocal(float rotation, float moveYaw)
        {
            // Get new local move angle
            var moveAngleLocal = MathHelper.DeltaAngle(rotation, moveYaw);

            // We cant blend running sideways and running backwards so in range 90->135 we snap to either sideways or backwards
            var absMoveAngle = math.abs(moveAngleLocal);
            if (absMoveAngle > 90 && absMoveAngle < 135)
            {
                var sign = math.sign(moveAngleLocal);
                moveAngleLocal = absMoveAngle > 112.5f ? sign * 135.0f : sign * 90.0f;
            }
            return moveAngleLocal;
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

            Profiler.BeginSample("HandleRigChange");

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

                var runVerticalAimClipInstance = ClipManager.Instance.GetClipFor(rig, settings.RunAimClipRef);
                var runAimHorizontalClipInstance = ClipManager.Instance.GetClipFor(rig, settings.RunAimHorizontalClipRef);
                var addRefPoseClipInstance = ClipManager.Instance.GetClipFor(rig, settings.AdditiveRefPose);

                nodeSet.SendMessage(state.AimVertical, ClipNode.SimulationPorts.ClipInstance, runVerticalAimClipInstance);
                nodeSet.SendMessage(state.AimHorizontal, ClipNode.SimulationPorts.ClipInstance, runAimHorizontalClipInstance);

                // TODO: Do we need multiple, or can we have a diamond shape?
                nodeSet.SendMessage(state.AdditiveRefPoseA, ClipNode.SimulationPorts.ClipInstance, addRefPoseClipInstance);
                nodeSet.SendMessage(state.AdditiveRefPoseB, ClipNode.SimulationPorts.ClipInstance, addRefPoseClipInstance);

                nodeSet.SendMessage(state.Mixer, LayerMixerNode.SimulationPorts.RigDefinition, rig);
                nodeSet.SendMessage(state.BlendTreeNode, BlendTree2DNode.SimulationPorts.RigDefinition, rig);
                nodeSet.SendMessage(state.BlendTreeNode, BlendTree2DNode.SimulationPorts.BlendTree, settings.RunBlendSpace2D);

                nodeSet.SendMessage(state.AimVerticalDelta, DeltaNode.SimulationPorts.RigDefinition, rig);
                nodeSet.SendMessage(state.AimHorizontalDelta, DeltaNode.SimulationPorts.RigDefinition, rig);


                commands.AddComponent<AnimSource.HasValidRig>(entity);
            }).Run();

            Profiler.EndSample();

            Profiler.BeginSample("Apply state");
            // Apply state
            Entities
                .WithoutBurst() // Can be removed once NodeSets are Burst-friendly
                .ForEach((Entity entity, ref AnimSource.Data animSource, ref Settings settings, ref SystemState state) =>
            {
                if (!EntityManager.HasComponent<Character.InterpolatedData>(animSource.animStateEntity))
                    return;

                var charInterpolatedState = EntityManager.GetComponentData<Character.InterpolatedData>(animSource.animStateEntity);

                nodeSet.SetData(state.AimVertical, ClipNode.KernelPorts.Time,
                    charInterpolatedState.aimPitch * state.AimVerticalDuration / 180f);

                var timeFactor = state.AimHorizontalDuration / 180f;
                var time = (90f + MathHelper.DeltaAngle(charInterpolatedState.aimYaw, charInterpolatedState.rotation)) * timeFactor;
                time = math.max(0f, time);
                nodeSet.SetData(state.AimHorizontal, ClipNode.KernelPorts.Time, time);

                // Blend in/out aim for reload
                nodeSet.SendMessage(state.Mixer, LayerMixerNode.SimulationPorts.WeightInput1,
                    1f - charInterpolatedState.blendOutAim * settings.blendOutAimOnReloadPitch);

                // Set the blend param and phase of the blend tree
                var blendParamX = new Parameter {
                    Id = settings.RunBlendSpace2D.Value.BlendParameterX,
                    Value = charInterpolatedState.locomotionVector.x
                };

                var blendParamY = new Parameter {
                    Id = settings.RunBlendSpace2D.Value.BlendParameterY,
                    Value = charInterpolatedState.locomotionVector.y
                };

                nodeSet.SendMessage(state.BlendTreeNode, BlendTree2DNode.SimulationPorts.Parameter, blendParamX);
                nodeSet.SendMessage(state.BlendTreeNode, BlendTree2DNode.SimulationPorts.Parameter, blendParamY);

                // TODO: Remove move fmod if/when blendtree uses loopable clip node
                var phase = math.fmod(charInterpolatedState.locomotionPhase, 1.0f);
                nodeSet.SetData(state.BlendTreeNode, BlendTree2DNode.KernelPorts.NormalizedTime, phase);
            }).Run();

            Profiler.EndSample();

            commands.Playback(EntityManager);
            commands.Dispose();

            return default;
        }
    }

    static Vector2 AngleToPosition(float angle)
    {
        var forward = new float3(0f, 0f, 1f);
        var up = new float3(0f, 1f, 0f);
        var dir3D = math.mul(quaternion.AxisAngle(up, math.radians(angle)), forward);
        return new Vector2(dir3D.x, dir3D.z);
    }
}
