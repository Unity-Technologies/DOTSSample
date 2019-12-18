using Unity.Collections;
using UnityEngine;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine.Profiling;
using Unity.NetCode;
using Unity.Jobs;
using Unity.Transforms;


[DisableAutoCreation]
[UpdateInGroup(typeof(CharacterUpdateSystemGroup))]
[UpdateBefore(typeof(HandleCharacterSpawn))]
[AlwaysSynchronizeSystem]
public class HandleCharacterDespawn : JobComponentSystem
{
    protected override JobHandle OnUpdate(JobHandle inputDeps)
    {
        inputDeps.Complete();

        var commands = new EntityCommandBuffer(Allocator.TempJob);

        Entities
            .WithNone<Character.InterpolatedData>()
            .WithAll<Character.State>()
            .ForEach((Entity entity) =>
            {
                //GameDebug.Log(World, Character.ShowLifetime, "Handle char despawn. Char:", entity);
                commands.RemoveComponent<Character.State>(entity);
            }).Run();

        commands.Playback(EntityManager);
        commands.Dispose();

        return default;
    }
}

[DisableAutoCreation]
[UpdateInGroup(typeof(CharacterUpdateSystemGroup))]
[AlwaysSynchronizeSystem]
public class HandleCharacterSpawn : JobComponentSystem
{
    protected override JobHandle OnUpdate(JobHandle inputDeps)
    {
        inputDeps.Complete();

        var commands = new EntityCommandBuffer(Allocator.TempJob);
        var heroRegistry = HeroRegistry.GetRegistry(EntityManager);

        Entities
            .WithNone<Character.State>()
            .WithAll<Character.InterpolatedData>()
            .ForEach((Entity entity, ref Character.ReplicatedData characterRepAll, ref HealthStateData healthState) =>
        {
            //GameDebug.Log(World, Character.ShowLifetime, "Handle char spawn. Char:", entity);

            var state = new Character.State();

            ref var heroTypeAsset = ref heroRegistry.Value.Heroes[characterRepAll.heroTypeIndex];

            // Setup health
            healthState.SetMaxHealth(heroTypeAsset.health);

            state.eyeHeight = heroTypeAsset.eyeHeight;

            commands.AddComponent(entity, state);
        }).Run();

        commands.Playback(EntityManager);
        commands.Dispose();

        return default;
    }
}

[DisableAutoCreation]
[UpdateBefore(typeof(MovementUpdatePhase))]
[AlwaysSynchronizeSystem]
public class UpdateTeleportation : JobComponentSystem
{
    protected override JobHandle OnUpdate(JobHandle inputDeps)
    {
        inputDeps.Complete();

        var globalTime = GetEntityQuery(ComponentType.ReadOnly<GlobalGameTime>()).GetSingleton<GlobalGameTime>().gameTime;

        Entities
            .ForEach((ref Character.Settings presentationEntity, ref Character.PredictedData predictedState, ref PlayerControlled.State userCommandComponent) =>
        {
            if (presentationEntity.m_TeleportPending)
            {
                presentationEntity.m_TeleportPending = false;

                predictedState.position = presentationEntity.m_TeleportToPosition;
                predictedState.velocity = math.mul(presentationEntity.m_TeleportToRotation, (float3)Vector3.forward * math.length(predictedState.velocity));

                userCommandComponent.ResetCommand(globalTime.tick, presentationEntity.m_TeleportToRotation.eulerAngles.y, 90);
            }
        }).Run();

        return default;
    }
}

[DisableAutoCreation]
[UpdateBefore(typeof(AnimSourceRootSystemGroup))]
[AlwaysSynchronizeSystem]
public class PrepareCharacterPresentationState : JobComponentSystem
{
    const float k_StopMovePenalty = 0.1f;
//    const float k_StopMovePenalty = 0.075f;

    protected override JobHandle OnUpdate(JobHandle inputDeps)
    {
        inputDeps.Complete();

        var globalTime = GetEntityQuery(ComponentType.ReadOnly<GlobalGameTime>()).GetSingleton<GlobalGameTime>();
        var deltaTime = globalTime.frameDuration;
        var time = globalTime.gameTime;

        Profiler.BeginSample("CharacterSystemShared.UpdatePresentationState");

        var PredictingTick = World.GetExistingSystem<GhostPredictionSystemGroup>().PredictingTick;
        var movementPredictedStateFromEntity = GetComponentDataFromEntity<AbilityMovement.PredictedState>(false);
        var movementInterpolatedStateFromEntity = GetComponentDataFromEntity<AbilityMovement.InterpolatedState>(false);
        var abilityActionFromEntity = GetComponentDataFromEntity<Ability.AbilityAction>(true);
        var ownedAbilityBufferFromEntity = GetBufferFromEntity<AbilityOwner.OwnedAbility>(true);

        Entities
            .ForEach((Entity entity, ref PredictedGhostComponent predicted, ref Character.State charState, ref Character.PredictedData charPredictedState,
                ref Character.InterpolatedData animState, ref PlayerControlled.State controlledState, ref AimData.Data aimData) =>
        {
            if (!GhostPredictionSystemGroup.ShouldPredict(PredictingTick, predicted))
                return;

            var userCommand = controlledState.command;

            // TODO (mogens) dont query for ability entity every frame
            var abilityMovementEntity = Ability.FindAbility(ownedAbilityBufferFromEntity, entity, AbilityMovement.Tag);
            if (abilityMovementEntity == Entity.Null)
            {
                //GameDebug.LogError("Ability_Movement not found");
                return;
            }
            if (!movementPredictedStateFromEntity.HasComponent(abilityMovementEntity))
            {
                //GameDebug.LogError("Ability_Movement ability does not have PredictedState");
                return;
            }
            var abilityMovementPredict = movementPredictedStateFromEntity[abilityMovementEntity];
            var abilityMovementInterp = movementInterpolatedStateFromEntity[abilityMovementEntity];

            // TODO: Move this into the network
            animState.Position = charPredictedState.position;
            //            animState.charLocoTick = charPredictedState.locoStartTick;
            animState.sprinting = charPredictedState.sprinting;


            var activeActionAbility = Ability.FindActiveAction(ownedAbilityBufferFromEntity, entity);
            if (activeActionAbility != Entity.Null)
            {
                var activeAction = abilityActionFromEntity[activeActionAbility];
                animState.charAction = activeAction.action;
                animState.charActionTick = activeAction.actionStartTick;
            }
            else
            {
                // TODO (mogensh) this should not be necessary, but seems abilities loose AbilityAction component before their AbilityAction is applied when going to idle
                animState.charAction = Ability.AbilityAction.Action.None;
            }


            //            // Update aim // TODO (mogensh) we should pass lookat pos directly
            //            var aimRefPoint = animState.position + new float3(0, 1.4f, 0);
            //            var aimDir = math.normalize(aimData.AimRefPoint - aimRefPoint);
            //            var down = new float3(0,-1,0);
            //            var pitch = math.degrees(math.acos(math.dot(down, aimDir)));
            //            animState.aimPitch =  pitch;
            //
            //
            //            var groundDir = math.abs(aimDir.y) < 1
            //                ? math.normalize(new float3(aimDir.x, 0, aimDir.z))
            //                : new float3(1, 0, 0);
            //            var axisZ = new float3(0, 0, 1);
            //            var groundDirCross = math.cross(groundDir, axisZ);
            //            var yaw = math.degrees(math.acos(math.dot(axisZ, groundDir)));
            //            animState.aimYaw = groundDirCross.y < 0 ? yaw : -yaw;

            // TODO: (sunek) Have anim sources use userCmd.look/camera based aim where appropriate
            animState.aimYaw = userCommand.lookYaw;
            animState.aimPitch = userCommand.lookPitch;

            //            Debug.DrawLine(aimRefPoint, aimData.AimRefPoint, Color.gray);
            //            DebugDraw.Sphere(aimData.AimRefPoint, 0.2f, Color.grey);
            //            Debug.DrawLine(aimData.CameraAxisPos, aimData.CharacterAimPoint, Color.blue);
            //            DebugDraw.Sphere(aimData.CharacterAimPoint, 0.3f, aimData.CameraAimPointVisible ? Color.blue : Color.red);
            //
            //            var aimDebugRot = quaternion.Euler(math.radians(-animState.aimPitch + 90f), math.radians(animState.aimYaw), 0f);
            //            Debug.DrawLine(aimRefPoint, aimRefPoint + math.mul(aimDebugRot, Vector3.forward), Color.blue);
            //            Debug.DrawLine(aimRefPoint, aimRefPoint + math.mul(aimDebugRot, Vector3.up), Color.green);
            //            Debug.DrawLine(aimRefPoint, aimRefPoint + math.mul(aimDebugRot, Vector3.right), Color.red);

            abilityMovementInterp.previousCharLocoState = abilityMovementInterp.charLocoState;

            // Add small buffer between GroundMove and Stand, to reduce animation noise when there are gaps in between
            // input keypresses

            // TODO: Penalty should likely not happen in this mapping (+ if you set stop penalty to a long window, issues become obvious)
            // TODO: Possibly move to Ability Movement
            if (abilityMovementPredict.locoState == AbilityMovement.LocoState.Stand
                && abilityMovementInterp.charLocoState == AbilityMovement.LocoState.GroundMove
                && time.DurationSinceTick(abilityMovementPredict.lastGroundMoveTick) < k_StopMovePenalty)
            {
                abilityMovementInterp.charLocoState = AbilityMovement.LocoState.GroundMove;
            }
            else
            {
                abilityMovementInterp.charLocoState = abilityMovementPredict.locoState;
            }

            var groundMoveVec = MathHelper.ProjectOnPlane(charPredictedState.velocity, Vector3.up);
            animState.moveYaw = Vector3.Angle(Vector3.forward, groundMoveVec);
            var cross = Vector3.Cross(Vector3.forward, groundMoveVec);
            if (cross.y < 0)
                animState.moveYaw = 360 - animState.moveYaw;

            animState.damageTick = charPredictedState.damageTick;
            var damageDirOnPlane = MathHelper.ProjectOnPlane(charPredictedState.damageDirection, Vector3.up);
            animState.damageDirection = Vector3.SignedAngle(Vector3.forward, damageDirOnPlane, Vector3.up);

            // Set lastGroundMoveTick
            if (abilityMovementPredict.locoState == AbilityMovement.LocoState.GroundMove)
            {
                abilityMovementPredict.lastGroundMoveTick = time.tick;
            }

            // Set anim state before anim state ctrl is running
            movementInterpolatedStateFromEntity[abilityMovementEntity] = abilityMovementInterp;
            movementPredictedStateFromEntity[abilityMovementEntity] = abilityMovementPredict;
        }).Run();

        Profiler.EndSample();

        return default;
    }
}

[UpdateInGroup(typeof(MovementResolvePhase))]
[UpdateAfter(typeof(AbilityMovement.HandleCollision))]
[DisableAutoCreation]
[AlwaysSynchronizeSystem]
public class GroundTest : JobComponentSystem
{
    protected override void OnCreate()
    {
        base.OnCreate();
        m_defaultLayer = LayerMask.NameToLayer("Default");
        m_playerLayer = LayerMask.NameToLayer("collision_player");
        m_platformLayer = LayerMask.NameToLayer("Platform");

        var mask = (uint)1 << m_defaultLayer | (uint)1 << m_playerLayer | (uint)1 << m_platformLayer;
        m_filter = new Unity.Physics.CollisionFilter { BelongsTo = mask, CollidesWith = mask, GroupIndex = 0 };

        m_buildPhysicsWorld = World.GetExistingSystem<Unity.Physics.Systems.BuildPhysicsWorld>();
        m_exportPhysicsWorld = World.GetOrCreateSystem<Unity.Physics.Systems.ExportPhysicsWorld>();
    }

    protected override JobHandle OnUpdate(JobHandle inputDeps)
    {
        inputDeps.Complete();
        m_exportPhysicsWorld.FinalJobHandle.Complete();

        var physicsWorld = m_buildPhysicsWorld.PhysicsWorld;
        var filter = m_filter;
        var PredictingTick = World.GetExistingSystem<GhostPredictionSystemGroup>().PredictingTick;

        Entities
            .ForEach((ref Character.State character, ref Character.PredictedData charPredictedState, ref PredictedGhostComponent predictionData) =>
        {
            if (!GhostPredictionSystemGroup.ShouldPredict(PredictingTick, predictionData))
                return;
            var colliWorld = physicsWorld.CollisionWorld;
            var startOffset = 1f;
            var distance = 3f;

            var origin = new float3(charPredictedState.position) + new float3(0, startOffset, 0);
            var castInput = new Unity.Physics.RaycastInput
            {
                Start = origin,
                End = origin - new float3(0, distance, 0),
                Filter = filter
            };
            var closestHit = new Unity.Physics.RaycastHit();
            if (colliWorld.CastRay(castInput, out closestHit))
                character.altitude = closestHit.Fraction * distance - startOffset;
            else
                character.altitude = distance - startOffset;
        }).Run();

        return default;
    }

    int m_defaultLayer;
    int m_playerLayer;
    int m_platformLayer;
    Unity.Physics.CollisionFilter m_filter;
    Unity.Physics.Systems.BuildPhysicsWorld m_buildPhysicsWorld;
    Unity.Physics.Systems.ExportPhysicsWorld m_exportPhysicsWorld;
}

[DisableAutoCreation]
[AlwaysSynchronizeSystem]
[UpdateAfter(typeof(PrepareCharacterPresentationState))]
[UpdateAfter(typeof(AnimSourceRootSystemGroup))]
public class ApplyRootTransform : JobComponentSystem
{
    protected override JobHandle OnUpdate(JobHandle inputDeps)
    {
        Entities
            .WithAll<AnimSourceController.OutputNode>()
            .ForEach((Entity entity, ref Translation translation, ref Rotation rotation, in Character.InterpolatedData charInterpolatedState) =>
            {
                translation = new Translation
                {
                    Value = charInterpolatedState.Position,
                };
                rotation = new Rotation
                {
                    Value = quaternion.Euler(0, math.radians(charInterpolatedState.rotation),0),
                };
            }).Run();

        return default;
    }
}

