using System;
using Unity.Animation;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Physics.Systems;
using Unity.DataFlowGraph;
using Unity.Sample.Core;
using UnityEngine;


public class AnimSourceStandIK
{
    [ConfigVar(Name = "char.standik", DefaultValue = "1", Description = "Enable stand foot ik")]
    public static ConfigVar useFootIk;

    [ConfigVar(Name = "debug.char.standik", DefaultValue = "0", Description = "Debug foot ik raycast")]
    public static ConfigVar debugStandIk;


    [Serializable]
    public struct PlaySettings
    {
        [Range(0f, 2f)]
        public float weight;
        public float playSpeed;
    }

    [Serializable]
    public struct BoneReferences : IComponentData
    {
        public int HipsIndex;
        public int LeftToeIndex;
        public int RightToeIndex;
        public int LeftFootIkBoneIndex;
        public int RightFootIkBoneIndex;
    }

    public struct Settings : IComponentData
    {
        public float animTurnAngle; // Total turn in turn anim
        public StandIkNode.Settings FootIk;
        public BoneReferences boneReferences;
        public BlobAssetReference<RigDefinition> rigReference;
    }

    public struct SystemState : ISystemStateComponentData
    {
        public static SystemState Default => new SystemState();
        public NodeHandle<StandIkNode> StandIkNode;
        public StandPhase StandPhase;
        public float3 LeftFootPos; // TODO: Use float3 instead?
        public float3 RightFootPos;

        public Unity.Physics.RaycastHit LeftHit;
        public Unity.Physics.RaycastHit RightHit;

        public bool LeftHitSuccess;
        public bool RightHitSuccess;

        public int Mask;
        public float2 TurnStartOffset;
        public float2 TurnEndOffset;

        public float3 TurnStartNormalLeft;
        public float3 TurnStartNormalRight;
        public float3 TurnEndNormalLeft;
        public float3 TurnEndNormalRight;
        public FootFalls LeftTurnFootFalls;
        public FootFalls RightTurnFootFalls;

        public BoneReferences currentRigBoneIdx;
    }

    public enum StandPhase
    {
        Moving,
        Standing,
        Turning,
        TurnStart,
        TurnEnd
    }

    public struct FootFalls
    {
        public float leftFootUp;
        public float leftFootDown;
        public float rightFootUp;
        public float rightFootDown;
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
            var animationGraphSystem = m_AnimationGraphSystem;

            Entities
                .WithoutBurst() // Can be removed once NodeSets are Burst-friendly
                .WithNone<SystemState>()
                .ForEach((Entity entity, ref AnimSource.Data animSource, ref Settings settings) =>
            {
                var state = SystemState.Default;

                state.StandIkNode = AnimationGraphHelper.CreateNode<StandIkNode>(animationGraphSystem, "StandIkNode");
                nodeSet.SetData(state.StandIkNode, StandIkNode.KernelPorts.Weight, 1f);

                // TODO: Hardcode values for now. Next step, generate from events (when they arrive) or evaluate curves?
                state.LeftTurnFootFalls = new FootFalls
                {
                    leftFootUp = 0.3015f,
                    leftFootDown = 0.4659f,
                    rightFootUp = 0.0925f,
                    rightFootDown = 0.2957f
                };

                state.RightTurnFootFalls = new FootFalls
                {
                    leftFootUp = 0.1202f,
                    leftFootDown = 0.2635f,
                    rightFootUp = 0.2706f,
                    rightFootDown = 0.4499f
                };

                // Set collision mask
                var defaultLayer = LayerMask.NameToLayer("Default");
                var playerLayer = LayerMask.NameToLayer("collision_player");
                var platformLayer = LayerMask.NameToLayer("Platform");

                state.Mask = 1 << defaultLayer | 1 << playerLayer | 1 << platformLayer;

                // Expose input and outputs
                animSource.inputNode = state.StandIkNode;
                animSource.inputPortID = (InputPortID)StandIkNode.KernelPorts.Input;
                animSource.outputNode = state.StandIkNode;
                animSource.outputPortID = (OutputPortID)StandIkNode.KernelPorts.Output;

                commands.AddComponent(entity, state);
            }).Run();

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
            AnimationGraphHelper.DestroyNode(animGraphSys,state.StandIkNode);
            cmdBuffer.RemoveComponent(entity, typeof(SystemState));
        }
    }

    [UpdateInGroup(typeof(AnimSourceUpdateCGroup))]
    [DisableAutoCreation]
    [AlwaysSynchronizeSystem]
    class UpdateSystem : JobComponentSystem
    {
        protected override JobHandle OnUpdate(JobHandle inputDeps)
        {
            inputDeps.Complete();

            var physicsWorld = World.GetExistingSystem<BuildPhysicsWorld>().PhysicsWorld;
            bool useFootIkValue = useFootIk.IntValue > 0;

            var characterInterpolatedDataFromEntity = GetComponentDataFromEntity<Character.InterpolatedData>(false);
            var characterPredictedDataFromEntity = GetComponentDataFromEntity<Character.PredictedData>(true);

            Entities
                .ForEach((Entity entity, ref AnimSource.Data animSource, ref Settings settings, ref SystemState state,
                    ref AnimSource.AllowWrite allowWrite) =>
            {
                if (!characterInterpolatedDataFromEntity.HasComponent(animSource.animStateEntity))
                {
                    //GameDebug.LogWarning(World,"AnimSource does not have Character.InterpolatedData components. Has it been deleted?");
                    return;
                }

                var animState = characterInterpolatedDataFromEntity[animSource.animStateEntity];
                var predictedState = characterPredictedDataFromEntity[animSource.animStateEntity];

                // Foot IK update
                if (settings.FootIk.enabled == 1 && useFootIkValue)
                {
                    // Figure out stand state
                    if (math.length(predictedState.velocity) < 0.001f && allowWrite.FirstUpdate)
                        state.StandPhase = StandPhase.Moving;
                    else if (math.length(predictedState.velocity) > 0.001f)
                        state.StandPhase = StandPhase.Moving;
                    else if (animState.turnDirection != 0 && state.StandPhase != StandPhase.TurnStart && state.StandPhase != StandPhase.Turning)
                        state.StandPhase = StandPhase.TurnStart;
                    else if (animState.turnDirection != 0)
                        state.StandPhase = StandPhase.Turning;
                    else if (animState.turnDirection == 0 && state.StandPhase == StandPhase.Turning)
                        state.StandPhase = StandPhase.TurnEnd;
                    else
                        state.StandPhase = StandPhase.Standing;

                    // Update foot position
                    if (state.StandPhase == StandPhase.Moving || allowWrite.FirstUpdate)
                    {
                        var rotation = quaternion.Euler(0f, math.radians(animState.rotation), 0f);
                        state.LeftFootPos = math.mul(rotation , settings.FootIk.leftToeStandPos) + animState.Position;
                        state.RightFootPos = math.mul(rotation, settings.FootIk.rightToeStandPos) + animState.Position;
                    }
                    else if (state.StandPhase == StandPhase.TurnStart)
                    {
                        // Predict foot placement after turn
                        // TODO: Convert all angles to radians in convert
                        var predictedRotation = quaternion.Euler(0f, math.radians(animState.turnStartAngle + settings.animTurnAngle * animState.turnDirection), 0f);
                        state.LeftFootPos = math.mul(predictedRotation, settings.FootIk.leftToeStandPos) + animState.Position;
                        state.RightFootPos = math.mul(predictedRotation, settings.FootIk.rightToeStandPos) + animState.Position;
                    }

                    // Do raycasts
                    var upVector = new float3(0f, 1f, 0f);
                    var rayEmitOffset = upVector * settings.FootIk.emitRayOffset;

                    if (state.StandPhase == StandPhase.Moving || state.StandPhase == StandPhase.TurnStart)
                    {
                        Entity hitEntity;
                        var maxRayDistance = settings.FootIk.emitRayOffset + settings.FootIk.maxRayDistance;
						var downVector = new float3(0f, -1f, 0f);
                        state.LeftHitSuccess = Raycast(state.LeftFootPos + rayEmitOffset, state.LeftFootPos + downVector * maxRayDistance, out state.LeftHit, out hitEntity, physicsWorld);
                        state.RightHitSuccess = Raycast(state.RightFootPos + rayEmitOffset, state.RightFootPos + downVector * maxRayDistance, out state.RightHit, out hitEntity, physicsWorld);

//                        if (hitEntity != Entity.Null)
//                        {
//                            GameDebug.Log("Collided with entity: " + EntityManager.GetName(hitEntity));
//                        }
                    }

                    // Update foot offsets
                    if (allowWrite.FirstUpdate)
                    {
                        animState.footIkOffset = float2.zero;
                        animState.footIkNormalLeft = float3.zero;
                        animState.footIkNormalRight = float3.zero;
                        animState.footIkWeight = 0.0f;
                    }

                    if (state.StandPhase == StandPhase.Moving || state.StandPhase == StandPhase.TurnEnd)
                    {
                        animState.footIkOffset = GetClampedOffset(state, settings);
                        animState.footIkNormalLeft = state.LeftHit.SurfaceNormal;
                        animState.footIkNormalRight = state.RightHit.SurfaceNormal;

                        state.TurnStartOffset.x = animState.footIkOffset.x;
                        state.TurnStartOffset.y = animState.footIkOffset.y;
                        state.TurnStartNormalLeft = state.LeftHit.SurfaceNormal;
                        state.TurnStartNormalRight = state.RightHit.SurfaceNormal;
                    }

                    else if (state.StandPhase == StandPhase.TurnStart)
                    {
                        state.TurnEndOffset = GetClampedOffset(state, settings);
                        state.TurnEndNormalLeft = state.LeftHit.SurfaceNormal;
                        state.TurnEndNormalRight = state.RightHit.SurfaceNormal;
                    }

                    if (state.StandPhase == StandPhase.TurnStart || state.StandPhase == StandPhase.Turning)
                    {
                        var absAngleRemaining = 90 - math.abs(MathHelper.DeltaAngle(animState.rotation, animState.turnStartAngle));
                        var turnFraction = (-absAngleRemaining + settings.animTurnAngle) / settings.animTurnAngle;
                        var footFalls = animState.turnDirection == -1 ? state.LeftTurnFootFalls : state.RightTurnFootFalls;

                        var leftFootFraction = GetFootFraction(turnFraction, footFalls.leftFootUp, footFalls.leftFootDown);
                        animState.footIkOffset.x = math.lerp(state.TurnStartOffset.x, state.TurnEndOffset.x, leftFootFraction);
                        animState.footIkNormalLeft = math.lerp(state.TurnStartNormalLeft, state.TurnEndNormalLeft, leftFootFraction);

                        var rightFootFraction = GetFootFraction(turnFraction, footFalls.rightFootUp, footFalls.rightFootDown);
                        animState.footIkOffset.y = math.lerp(state.TurnStartOffset.y, state.TurnEndOffset.y, rightFootFraction);
                        animState.footIkNormalRight = math.lerp(state.TurnStartNormalRight, state.TurnEndNormalRight, rightFootFraction);
                    }

                    animState.footIkWeight = math.clamp(animState.footIkWeight + (1 - settings.FootIk.enterStateEaseIn), 0f, 1f);
                }

#if UNITY_EDITOR
                DebugSceneView(animState, settings, state);
#endif

                DebugApplyPresentation();
                allowWrite.FirstUpdate = false;

                characterInterpolatedDataFromEntity[animSource.animStateEntity] = animState;
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

            // Handle rig change
            Entities
                .WithNone<AnimSource.HasValidRig>()
                .WithoutBurst() // Can be removed once NodeSets are Burst-friendly
                .ForEach((Entity entity, ref AnimSource.Data animSource, ref Settings settings, ref SystemState state) =>
            {
                if (!EntityManager.HasComponent<Character.InterpolatedData>(animSource.animStateEntity))
                {
                    GameDebug.LogWarning(World, "AnimSource does not have Character.InterpolatedData components. Has it been deleted?");
                    return;
                }

                if (!EntityManager.HasComponent<SharedRigDefinition>(animSource.animStateEntity))
                    return;

                var sharedRigDef = EntityManager.GetSharedComponentData<SharedRigDefinition>(animSource.animStateEntity);
                var rig = sharedRigDef.Value;

                nodeSet.SendMessage(state.StandIkNode, StandIkNode.SimulationPorts.RigDefinition, rig);

                // Remap rig indexes
                BlobAssetReference<AnimationAssetDatabase.RigMap> rigMap;
                AnimationAssetDatabase.GetOrCreateRigMapping(World, settings.rigReference, rig, out rigMap);

                state.currentRigBoneIdx.HipsIndex = rigMap.Value.BoneMap[settings.boneReferences.HipsIndex];
                state.currentRigBoneIdx.LeftToeIndex = rigMap.Value.BoneMap[settings.boneReferences.LeftToeIndex];
                state.currentRigBoneIdx.RightToeIndex = rigMap.Value.BoneMap[settings.boneReferences.RightToeIndex];
                state.currentRigBoneIdx.LeftFootIkBoneIndex = rigMap.Value.BoneMap[settings.boneReferences.LeftFootIkBoneIndex];
                state.currentRigBoneIdx.RightFootIkBoneIndex = rigMap.Value.BoneMap[settings.boneReferences.RightFootIkBoneIndex];

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

                var animState = EntityManager.GetComponentData<Character.InterpolatedData>(animSource.animStateEntity);

//                GameDebug.Log("Foot IK Weight: " + animState.footIkWeight);

                // Rotate the foot normals into character space
                var footIkNormalLeft = math.mul(quaternion.Euler(0f, math.radians(-animState.rotation), 0f), animState.footIkNormalLeft);
                var footIkNormalRight = math.mul(quaternion.Euler(0f, math.radians(-animState.rotation), 0f), animState.footIkNormalRight);

                var data = new StandIkNode.StandIkData
                {
                    Settings = settings.FootIk,

                    LeftToeIdx = state.currentRigBoneIdx.LeftToeIndex,
                    RightToeIdx = state.currentRigBoneIdx.RightToeIndex,
                    LeftFootIkIdx = state.currentRigBoneIdx.LeftFootIkBoneIndex,
                    RightFootIkIdx = state.currentRigBoneIdx.RightFootIkBoneIndex,
                    HipsIdx = state.currentRigBoneIdx.HipsIndex,
                    ikOffset = animState.footIkOffset,
                    normalLeftFoot = footIkNormalLeft,
                    normalRightFoot = footIkNormalRight,
                    Weight = animState.footIkWeight,
                };

                nodeSet.SendMessage(state.StandIkNode, StandIkNode.SimulationPorts.StandIkSetup, in data);
            }).Run();

            cmdBuffer.Playback(EntityManager);
            cmdBuffer.Dispose();

            return default;
        }
    }

    static float2 GetClampedOffset(SystemState state, Settings settings)
    {
        var leftOffset = 0.0f;
        var rightOffset = 0.0f;

        if (state.LeftHitSuccess)
        {
            leftOffset = math.clamp(state.LeftHit.Position.y - state.LeftFootPos.y + settings.FootIk.leftToeStandPos.y, -settings.FootIk.maxStepSize, settings.FootIk.maxStepSize);
        }

        if (state.RightHitSuccess)
        {
            rightOffset = math.clamp(state.RightHit.Position.y - state.RightFootPos.y + settings.FootIk.rightToeStandPos.y, -settings.FootIk.maxStepSize, settings.FootIk.maxStepSize);
        }

        var stepMag = math.abs(leftOffset - rightOffset);

        if (stepMag > settings.FootIk.maxStepSize)
        {
            leftOffset = (leftOffset / stepMag) * settings.FootIk.maxStepSize;
            rightOffset = (rightOffset / stepMag) * settings.FootIk.maxStepSize;
        }

        return new float2(leftOffset, rightOffset);
    }

    static float GetFootFraction(float turnFraction, float footUp, float footDown)
    {
        if (turnFraction <= footUp)
        {
            return 0f;
        }

        if (turnFraction < footDown)
        {
            return (turnFraction - footUp) / (footDown - footUp);
        }

        return 1f;
    }

    static void DebugSceneView(Character.InterpolatedData animState, Settings settings, SystemState state)
    {
        if (settings.FootIk.debugIdlePos == 1)
        {
            var rotation = quaternion.Euler(0f, math.radians(animState.rotation), 0f);
            var leftIdlePos = math.mul(rotation, settings.FootIk.leftToeStandPos) + animState.Position;
            var rightIdlePos = math.mul(rotation, settings.FootIk.rightToeStandPos) + animState.Position;

            DebugDraw.Sphere(leftIdlePos, 0.01f, Color.green);
            DebugDraw.Sphere(leftIdlePos, 0.04f, Color.green);
            DebugDraw.Sphere(rightIdlePos, 0.01f, Color.red);
            DebugDraw.Sphere(rightIdlePos, 0.04f, Color.red);
        }

        if (settings.FootIk.debugRayCast == 1)
        {
            DebugDraw.Sphere(state.LeftFootPos, 0.025f, Color.yellow);
            DebugDraw.Sphere(state.RightFootPos, 0.025f, Color.yellow);

            DebugDraw.Sphere(state.LeftHit.Position, 0.015f);
            DebugDraw.Sphere(state.RightHit.Position, 0.015f);

            UnityEngine.Debug.DrawLine(state.LeftHit.Position, state.LeftHit.Position + state.LeftHit.SurfaceNormal, Color.green);
            UnityEngine.Debug.DrawLine(state.RightHit.Position, state.RightHit.Position + state.RightHit.SurfaceNormal, Color.red);
        }
    }

    // TODO: Into separate system?
    static void DebugApplyPresentation()
    {
//        if (debugStandIk.IntValue > 0)
//        {
//            var charIndex = s_Instances.IndexOf(this);
//            var lineIndex = charIndex * 3 + 1;
//
//            var color = s_DebugColors[charIndex % s_DebugColors.Length];
//            var leftHitString = "Char " + charIndex + " - Left XForm hit:  Nothing";
//            if (m_LeftHitSuccess)
//                leftHitString = "Char " + charIndex + " - Left XForm hit:  " + m_LeftHit.transform.name;
//
//            DebugOverlay.Write(color, 2, lineIndex, leftHitString);
//            GameDebug.Log(leftHitString);
//
//            var rightHitString = "Char " + charIndex + " - Right XForm hit: Nothing";
//            if (m_RightHitSuccess)
//                rightHitString = "Char " + charIndex + " - Right XForm hit: " + m_RightHit.transform.name;
//
//            DebugOverlay.Write(color, 2, lineIndex + 1, rightHitString);
//            GameDebug.Log(rightHitString);
//        }
    }

    // TODO: Ray cast into job, possibly batch?
    // TODO: Case multiple points on foot, possibly shape cast
    static public bool Raycast(float3 RayFrom, float3 RayTo, out Unity.Physics.RaycastHit hit, out Entity entity,
        PhysicsWorld physicsWorld)
    {
        var collisionWorld = physicsWorld.CollisionWorld;

        var filter = CollisionFilter.Default;
        filter.CollidesWith = 1 << 0;
        RaycastInput input = new RaycastInput()
        {
            Start = RayFrom,
            End = RayTo,
            Filter = filter,
        };

        var success = collisionWorld.CastRay(input, out hit);
        if (success)
        {
            entity = physicsWorld.Bodies[hit.RigidBodyIndex].Entity;
        }
        else
        {
            entity = Entity.Null;
        }

        return success;
    }
}
