using System;
using Unity.Animation;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.DataFlowGraph;
using Unity.Sample.Core;
using UnityEngine;


public class AnimSourceSquash
{
    [Serializable]
    public struct PlaySettings
    {
        [Range(0f, 2f)]
        public float weight;
        public float playSpeed;
    }

    [Serializable]
    public struct Settings : IComponentData
    {
        public BlobAssetReference<Clip> ClipRef;

        public PlaySettings stop;
        public PlaySettings start;

        public PlaySettings doubleJump;

        public PlaySettings landMin;
        public float landMinFallSpeed;

        public PlaySettings landMax;
        public float landMaxFallSpeed;

        public PlaySettings changeDir;
        [Range(0f, 180f)]
        public float dirChangeMinAngle;
        public float dirChangeTimePenalty;
    }

    public struct SystemState : ISystemStateComponentData
    {
        public static SystemState Default => new SystemState();

        public NodeHandle<ClipNode> SquashClip;
        public NodeHandle<ClipNode> BasePoseClip;
        public NodeHandle<DeltaNode> SquashClipDelta;
        public NodeHandle<LayerMixerNode> MixerNode;

        public float SquashClipDuration;
        public AbilityMovement.LocoState PrevLocoState;
        public float PlaySpeed;
        public float PrevMoveAngle;
        public int LastDirChangeTick;

        public float DebugLastFrameVel;
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

            Entities
                .WithoutBurst() // Can be removed once NodeSets are Burst-friendly
                .WithNone<SystemState>()
                .ForEach((Entity entity, ref AnimSource.Data animSource, ref Settings settings) =>
            {
                GameDebug.Log(World,AnimSource.ShowLifetime,"InitSystem Squash entity:{0} state entity:{1}", entity, animSource.animStateEntity);

                var state = SystemState.Default;

                state.SquashClip = AnimationGraphHelper.CreateNode<ClipNode>(animationGraphSystem, "SquashClip");
                state.BasePoseClip = AnimationGraphHelper.CreateNode<ClipNode>(animationGraphSystem, "BasePoseClip");
                state.SquashClipDelta = AnimationGraphHelper.CreateNode<DeltaNode>(animationGraphSystem, "SquashClipDelta");

                state.MixerNode = AnimationGraphHelper.CreateNode<LayerMixerNode>(animationGraphSystem, "MixerNode");
                nodeSet.SendMessage(state.MixerNode, LayerMixerNode.SimulationPorts.BlendModeInput0, BlendingMode.Override);
                nodeSet.SendMessage(state.MixerNode, LayerMixerNode.SimulationPorts.WeightInput0, 1f);
                nodeSet.SendMessage(state.MixerNode, LayerMixerNode.SimulationPorts.BlendModeInput1, BlendingMode.Additive);
                nodeSet.SendMessage(state.MixerNode, LayerMixerNode.SimulationPorts.WeightInput1, 1f);

                nodeSet.Connect(state.BasePoseClip , ClipNode.KernelPorts.Output, state.SquashClipDelta, DeltaNode.KernelPorts.Subtract);
                nodeSet.Connect(state.SquashClip , ClipNode.KernelPorts.Output, state.SquashClipDelta, DeltaNode.KernelPorts.Input);
                nodeSet.Connect(state.SquashClipDelta , DeltaNode.KernelPorts.Output, state.MixerNode, LayerMixerNode.KernelPorts.Input1);

                // Load clips and store clip info
                state.SquashClipDuration = settings.ClipRef.Value.Duration;

                // Expose input and outputs
                animSource.inputNode = state.MixerNode;
                animSource.inputPortID = (InputPortID)LayerMixerNode.KernelPorts.Input0;
                animSource.outputNode = state.MixerNode;
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
            GameDebug.Log(world,AnimSource.ShowLifetime,"Deinit Sprint entity:{0}", entity);

            if (state.SquashClip != default && animGraphSys.Set.Exists(state.SquashClip))
                AnimationGraphHelper.DestroyNode(animGraphSys,state.SquashClip);

            if (state.BasePoseClip != default && animGraphSys.Set.Exists(state.BasePoseClip))
                AnimationGraphHelper.DestroyNode(animGraphSys,state.BasePoseClip);

            if (state.SquashClipDelta != default && animGraphSys.Set.Exists(state.SquashClipDelta))
                AnimationGraphHelper.DestroyNode(animGraphSys,state.SquashClipDelta);

            if (state.MixerNode != default && animGraphSys.Set.Exists(state.MixerNode))
                AnimationGraphHelper.DestroyNode(animGraphSys,state.MixerNode);

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

            // TODO (mogensh) find cleaner way to get time
            var globalTime = GetEntityQuery(ComponentType.ReadOnly<GlobalGameTime>()).GetSingleton<GlobalGameTime>();
            var time = globalTime.gameTime;
            var deltaTime = globalTime.frameDuration;

            var characterInterpolatedDataFromEntity = GetComponentDataFromEntity<Character.InterpolatedData>(false);
            var characterPredictedDataFromEntity = GetComponentDataFromEntity<Character.PredictedData>(true);
            var abilityInterpolatedStateFromEntity = GetComponentDataFromEntity<AbilityMovement.InterpolatedState>(true);
            var ownedAbilityBufferFromEntity = GetBufferFromEntity<AbilityOwner.OwnedAbility>(true);

            Entities
                .WithAll<AnimSource.AllowWrite>()
                .ForEach((Entity entity, ref AnimSource.Data animSource, ref Settings settings, ref SystemState state) =>
            {
                if (!characterInterpolatedDataFromEntity.HasComponent(animSource.animStateEntity))
                {
                    //GameDebug.LogWarning(World,"AnimSource does not have Character.InterpolatedData components. Has it been deleted?");
                    return;
                }

                var charInterpolatedState = characterInterpolatedDataFromEntity[animSource.animStateEntity];
                var charPredictedState = characterPredictedDataFromEntity[animSource.animStateEntity];

                // TODO (mogens) dont query for ability entity every frame
                var abilityMovementEntity = Ability.FindAbility(ownedAbilityBufferFromEntity, animSource.animStateEntity, AbilityMovement.Tag);
                if (abilityMovementEntity == Entity.Null)
                    return;

                var abilityMovement = abilityInterpolatedStateFromEntity[abilityMovementEntity];

                var timeToSquash = math.abs(charInterpolatedState.squashTime) < 0.001f || charInterpolatedState.squashTime >= state.SquashClipDuration;
                var moveAngleLocal = CalculateMoveAngleLocal(charInterpolatedState.rotation, charInterpolatedState.moveYaw);

                var vel = -charPredictedState.velocity.y;
                var mag = math.length(new float3(charPredictedState.velocity.x, charPredictedState.velocity.y, charPredictedState.velocity.z));
                vel = (vel + mag) * 0.5f;

                if (state.PrevLocoState != abilityMovement.charLocoState)
                {
                    // Double jump
                    if (abilityMovement.charLocoState == AbilityMovement.LocoState.DoubleJump)
                    {
                        charInterpolatedState.squashTime = 0;
                        charInterpolatedState.squashWeight = settings.doubleJump.weight;
                        state.PlaySpeed = settings.doubleJump.playSpeed;
                    }

                    // Landing
                    else if (state.PrevLocoState == AbilityMovement.LocoState.InAir)
                    {
                        charInterpolatedState.squashTime = 0;

                        var smoothedVel = vel + state.DebugLastFrameVel * 0.5f;
                        var t = smoothedVel < settings.landMinFallSpeed ? 0 :
                            smoothedVel > settings.landMaxFallSpeed ? 1 :
                            (smoothedVel - settings.landMinFallSpeed) / (settings.landMaxFallSpeed - settings.landMinFallSpeed);

                        charInterpolatedState.squashWeight = math.lerp(settings.landMin.weight, settings.landMax.weight, t);
                        state.PlaySpeed = math.smoothstep(settings.landMin.playSpeed, settings.landMax.playSpeed, t);
                    }

                    // Stopping
                    else if (timeToSquash && abilityMovement.charLocoState == AbilityMovement.LocoState.Stand)
                    {
                        //GameDebug.Log("Stopping!");
                        charInterpolatedState.squashTime = 0;
                        charInterpolatedState.squashWeight = settings.stop.weight;
                        state.PlaySpeed = settings.stop.playSpeed;
                    }

                    // Start Moving
                    else if (timeToSquash && abilityMovement.charLocoState == AbilityMovement.LocoState.GroundMove)
                    {
                        //GameDebug.Log("Starting!");
                        charInterpolatedState.squashTime = 0;
                        charInterpolatedState.squashWeight = settings.start.weight;
                        state.PlaySpeed = settings.start.playSpeed;
                    }
                }

                // Direction change
                else if (abilityMovement.charLocoState == AbilityMovement.LocoState.GroundMove &&
                    math.abs(MathHelper.DeltaAngle(moveAngleLocal, state.PrevMoveAngle)) > settings.dirChangeMinAngle)
                {
                    if (timeToSquash && time.DurationSinceTick(state.LastDirChangeTick) > settings.dirChangeTimePenalty)
                    {
                        charInterpolatedState.squashTime = 0;
                        charInterpolatedState.squashWeight = settings.changeDir.weight;
                        state.PlaySpeed = settings.changeDir.playSpeed;
                    }

                    state.LastDirChangeTick = time.tick;
                }

                if (charInterpolatedState.squashWeight > 0)
                {
                    charInterpolatedState.squashTime += state.PlaySpeed * deltaTime;
                    if (charInterpolatedState.squashTime > state.SquashClipDuration)
                        charInterpolatedState.squashWeight = 0.0f;
                }

                state.PrevLocoState = abilityMovement.charLocoState;
                state.PrevMoveAngle = moveAngleLocal;

                charInterpolatedState.squashTime = math.min(state.SquashClipDuration, charInterpolatedState.squashTime);
                characterInterpolatedDataFromEntity[animSource.animStateEntity] = charInterpolatedState;

                state.DebugLastFrameVel = vel;
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

            var cmdBuffer = new EntityCommandBuffer(Allocator.TempJob);

            // Handle rig change
            Entities
                .WithNone<AnimSource.HasValidRig>()
                .WithoutBurst() // Can be removed once NodeSets are Burst-friendly
                .ForEach((Entity entity, ref AnimSource.Data animSource, ref Settings settings, ref SystemState state) =>
            {
                if (!EntityManager.HasComponent<SharedRigDefinition>(animSource.animStateEntity))
                    return;

                var sharedRigDef = EntityManager.GetSharedComponentData<SharedRigDefinition>(animSource.animStateEntity);
                var rig = sharedRigDef.Value;

                var clipInstance = ClipManager.Instance.GetClipFor(rig, settings.ClipRef);

                nodeSet.SendMessage(state.SquashClip, ClipNode.SimulationPorts.ClipInstance, clipInstance);
                nodeSet.SendMessage(state.BasePoseClip, ClipNode.SimulationPorts.ClipInstance, clipInstance);
                nodeSet.SendMessage(state.SquashClipDelta, DeltaNode.SimulationPorts.RigDefinition, rig);
                nodeSet.SendMessage(state.MixerNode, LayerMixerNode.SimulationPorts.RigDefinition, rig);

                cmdBuffer.AddComponent<AnimSource.HasValidRig>(entity);
            }).Run();

            Entities
                .WithoutBurst() // Can be removed once NodeSets are Burst-friendly
                .ForEach((Entity entity, ref AnimSource.Data animSource, ref Settings settings, ref SystemState state) =>
            {
                if (!EntityManager.HasComponent<Character.InterpolatedData>(animSource.animStateEntity))
                {
                    GameDebug.LogWarning(World,"AnimSource does not have Character.InterpolatedData components. Has it been deleted?");
                    return;
                }

                var charInterpolatedState = EntityManager.GetComponentData<Character.InterpolatedData>(animSource.animStateEntity);

                nodeSet.SetData(state.SquashClip, ClipNode.KernelPorts.Time, charInterpolatedState.squashTime);
                nodeSet.SendMessage(state.MixerNode, LayerMixerNode.SimulationPorts.WeightInput1, charInterpolatedState.squashWeight);
            }).Run();

            cmdBuffer.Playback(EntityManager);
            cmdBuffer.Dispose();

            return default;
        }
    }
}
