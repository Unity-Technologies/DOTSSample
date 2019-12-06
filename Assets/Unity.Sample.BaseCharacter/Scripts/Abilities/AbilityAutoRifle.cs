using System;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.NetCode;
using Unity.Physics;
using Unity.Sample.Core;


public class AbilityAutoRifle
{
    public enum Phase
    {
        Idle,
        Fire,
        Reload,
    }

    public enum ImpactType
    {
        None,
        Environment,
        Character
    }

    [Serializable]
    public struct COFData
    {
        public float min;
        public float max;
        public float shotIncrease;
        public float DecreaseVel;
    }

    public struct Settings : IComponentData
    {
        public static Settings Default => new Settings();

        public float roundsPerSecond;
        public int clipSize;
        public float reloadDuration;

        public COFData COFData;

        public float damage;
        public float damageImpulse;
        public float hitscanRadius;

        public BlobAssetReference<RandomValueList.RandomFloat> RandomList;

        public WeakAssetReference projectileAssetGuid;
    }

    public const Ability.AbilityTagValue Tag = Ability.AbilityTagValue.AutoRifle;

    public struct State : IComponentData
    {
        public int lastHitCheckTick;
        public int teamId;
    }

    public struct PredictedState : IComponentData
    {
        [GhostDefaultField]
        public Phase action;
        [GhostDefaultField]
        public int phaseStartTick;

        [GhostDefaultField]
        public int ammoInClip;
        [GhostDefaultField(1)]
        public float COF;

        public void SetPhase(Phase action, int tick)
        {
            this.action = action;
            this.phaseStartTick = tick;
        }
    }

    public struct InterpolatedState : IComponentData
    {
        [GhostDefaultField]
        public int fireTick;
        [GhostDefaultField(100)]
        public float3 fireEndPos;
        [GhostDefaultField]
        public ImpactType impactType;
        [GhostDefaultField(10)]
        public float3 impactNormal;
    }




    public static void DecreaseCone(ref PredictedState state, ref Settings settings, float deltaTime)
    {
        // Decrease cone
        if (state.action != Phase.Fire)
        {
            state.COF -= settings.COFData.DecreaseVel * deltaTime;
            if (state.COF < settings.COFData.min)
                state.COF = settings.COFData.min;
        }
    }


    public static Phase GetPreferredState(ref PredictedState predictedState, ref Settings settings,
        in Ability.EnabledAbility enabledAbility)
    {
        if (enabledAbility.activeButtonIndex == 1 && predictedState.ammoInClip < settings.clipSize)
        {
            return Phase.Reload;
        }

        var isIdle = predictedState.action == Phase.Idle;
        if (isIdle)
        {
            if (enabledAbility.activeButtonIndex == 0 && predictedState.ammoInClip == 0)
            {
                return Phase.Reload;
            }
        }

        return enabledAbility.activeButtonIndex == 0 ? Phase.Fire : Phase.Idle;
    }

    [UpdateInGroup(typeof(BehaviourRequestPhase))]
    [DisableAutoCreation]
    [AlwaysSynchronizeSystem]
    public class IdleUpdate : JobComponentSystem
    {
        EntityQuery timeQuery;

        protected override void OnCreate()
        {
            timeQuery = GetEntityQuery(ComponentType.ReadOnly<GlobalGameTime>());
        }

        protected override JobHandle OnUpdate(JobHandle inputDeps)
        {
            inputDeps.Complete();

            var globalTime = timeQuery.GetSingleton<GlobalGameTime>().gameTime;
            var predictingTick = World.GetExistingSystem<GhostPredictionSystemGroup>().PredictingTick;
            var characterStateFromEntity = GetComponentDataFromEntity<Character.State>(true);
            var predictedEntityComponentFromEntity = GetComponentDataFromEntity<PredictedGhostComponent>(true);
            Entities
                .ForEach((ref Ability.EnabledAbility enabledAbility, ref Ability.AbilityStateIdle stateIdle, ref PredictedState predictedState,
                    ref Settings settings, ref State state) =>
            {
                if (predictedEntityComponentFromEntity.HasComponent(enabledAbility.owner) && !GhostPredictionSystemGroup.ShouldPredict(predictingTick, predictedEntityComponentFromEntity[enabledAbility.owner]))
                    return;
                // TODO (mogensh) can we find easier way to copy these properties. Do we need to every update?
                var charState = characterStateFromEntity[enabledAbility.owner];
                state.teamId = charState.teamId;

                var request = GetPreferredState(ref predictedState, ref settings, in enabledAbility);
                stateIdle.requestActive = request != Phase.Idle;

                DecreaseCone(ref predictedState, ref settings, globalTime.tickDuration);
            }).Run();

            return default;
        }
    }

    [UpdateInGroup(typeof(AbilityUpdatePhase))]
    [DisableAutoCreation]
    [AlwaysSynchronizeSystem]
    class ActiveUpdate : JobComponentSystem
    {
        struct UpdateJob
        {
            [ReadOnly] public GameTime time;
            [ReadOnly] public WorldId worldId;
            [ReadOnly] public ComponentDataFromEntity<AimData.Data> aimDataFromEntity;
            [ReadOnly] public ComponentDataFromEntity<PlayerControlled.State> playerControlledStateFromEntity;
            [ReadOnly] public ComponentDataFromEntity<HitCollider.Owner> hitColliderOwnerFromEntity;
            [ReadOnly] public ComponentDataFromEntity<HitColliderOwner.State> hitColliderOwnerStateFromEntity;
            [ReadOnly] public ComponentDataFromEntity<Ability.EnabledAbility> enabledEntityFromEntity;    // TODO (mogensh) HACK until we get support for more component types in IJobForEachWithEntity
            [ReadOnly] public CollisionHistoryBuffer collisionHistoryBuffer;
            public EntityCommandBuffer commands;
            public NativeMultiHashMap<Entity, DamageEvent>.ParallelWriter damageEvents;

            public void Execute(Entity entity,
                ref Ability.AbilityStateActive stateActive, ref PredictedState predictedState, ref InterpolatedState interpState,
                ref Settings settings, ref State internalState, ref Ability.AbilityAction abilityAction)
            {
                var enabledAbility = enabledEntityFromEntity[entity];

                var aimData = aimDataFromEntity[enabledAbility.owner];
                var playerCtrlState = playerControlledStateFromEntity[enabledAbility.owner];

                switch (predictedState.action)
                {
                    case Phase.Idle:
                        {
                            var request = GetPreferredState(ref predictedState, ref settings, in enabledAbility);
                            if (request == Phase.Reload)
                            {
                                EnterReloadingPhase(worldId, ref predictedState, ref abilityAction, time.tick);
                                break;
                            }

                            if (request == Phase.Fire)
                            {
                                EnterFiringPhase(worldId, ref abilityAction, ref predictedState, ref internalState, ref settings, ref playerCtrlState, enabledAbility.owner, ref aimData, ref interpState);
                                break;
                            }

                            // No requested state, so ability is done
                            stateActive.requestCooldown = true;
                            break;
                        }
                    case Phase.Fire:
                        {
                            var fireDuration = 1.0f / settings.roundsPerSecond;
                            var phaseDuration = time.DurationSinceTick(predictedState.phaseStartTick);
                            if (phaseDuration > fireDuration)
                            {
                                var request = GetPreferredState(ref predictedState, ref settings, in enabledAbility);
                                if (request == Phase.Fire && predictedState.ammoInClip > 0)
                                    EnterFiringPhase(worldId, ref abilityAction, ref predictedState,
                                        ref internalState, ref settings, ref playerCtrlState, enabledAbility.owner, ref aimData, ref interpState);
                                else
                                    EnterIdlePhase(worldId, ref stateActive, ref predictedState, ref abilityAction);
                            }

                            break;
                        }
                    case Phase.Reload:
                        {
                            var phaseDuration = time.DurationSinceTick(predictedState.phaseStartTick);
                            if (phaseDuration > settings.reloadDuration)
                            {
                                var neededInClip = settings.clipSize - predictedState.ammoInClip;
                                predictedState.ammoInClip += neededInClip;

                                EnterIdlePhase(worldId, ref stateActive, ref predictedState, ref abilityAction);
                            }

                            break;
                        }
                }

                DecreaseCone(ref predictedState, ref settings, time.tickDuration);
            }

            void EnterReloadingPhase(WorldId world, ref PredictedState predictedState, ref Ability.AbilityAction abilityAction, int tick)
            {
                //GameDebug.Log(world, ShowDebug, "EnterReloadingPhase");

                predictedState.SetPhase(Phase.Reload, tick);
                abilityAction.SetAction(Ability.AbilityAction.Action.Reloading, tick);
            }

            void EnterIdlePhase(WorldId world, ref Ability.AbilityStateActive stateActive,
                ref PredictedState predictedState, ref Ability.AbilityAction abilityAction)
            {
                //GameDebug.Log(world, ShowDebug, "EnterIdlePhase");

                stateActive.requestCooldown = true;
                predictedState.SetPhase(Phase.Idle, time.tick);
                abilityAction.SetAction(Ability.AbilityAction.Action.None, time.tick);
            }

            void EnterFiringPhase(WorldId world,
                ref Ability.AbilityAction abilityAction, ref PredictedState predictedState,
                ref State state, ref Settings settings,
                ref PlayerControlled.State userCmd, Entity ownerEntity, ref AimData.Data aimData,
                ref InterpolatedState interpState)
            {
                //GameDebug.Log(world, ShowDebug, "EnterFiringPhase");

                predictedState.SetPhase(Phase.Fire, time.tick);
                predictedState.ammoInClip -= 1;
                abilityAction.SetAction(Ability.AbilityAction.Action.PrimaryFire, time.tick);

                // Only fire shot once for each tick (so it does not fire again when re-predicting)
                if (time.tick > state.lastHitCheckTick)
                {
                    state.lastHitCheckTick = time.tick;

                    //var aimDir = (float3)userCmd.command.LookDir;
                    var aimDir = math.normalize(aimData.CharacterAimPoint - aimData.CameraAxisPos);

                    var cross = math.cross(new float3(0, 1, 0), aimDir);
                    var cofAngle = math.radians(predictedState.COF) * 0.5f;
                    var direction = math.mul(quaternion.AxisAngle(cross, cofAngle), aimDir);

                    var rndRollAngle =
                        settings.RandomList.Value.Values[time.tick % settings.RandomList.Value.Values.Length] *
                        math.PI * 2f;

//                    GameDebug.Log(world,null,"Rollangle:" + rndRollAngle);

                    var rndRot = quaternion.AxisAngle(aimDir, rndRollAngle);

                    direction = math.mul(rndRot, direction);

                    predictedState.COF += settings.COFData.shotIncrease;
                    if (predictedState.COF > settings.COFData.max)
                        predictedState.COF = settings.COFData.max;

//                    GameDebug.Log(worldId,null,"BANG Tick:{0} RenderTick:{1}" ,time.tick, userCmd.command.renderTick );

                    interpState.fireTick = time.tick;

                    var startPos = aimData.CameraAxisPos;

                    var projectile = false;
                    if (projectile)
                    {
//                        var endPos = startPos + direction * 100;
//                        ProjectileRequest.Create(commands, userCmd.command.renderTick,
//                            settings.projectileAssetGuid, ownerEntity, -1, startPos, endPos);
//                        interpState.fireEndPos = endPos;
                    }
                    else
                    {
                        const int distance = 100;
                        var query = new HitCollisionQuery.ProjectileQuery
                        {
                            ColliderOwnerFromEntity = hitColliderOwnerFromEntity,
                            ColliderOwnerStateFromEntity = hitColliderOwnerStateFromEntity,
                            EnvironmentFilter = 1u << 0,
                            HitColliderFilter = 1u << 1,
                            HitColliderOwnerFlagFilter = ~(1U << state.teamId),    // TODO (mogensh) we need better way to handle team id and its mapping to hitcollider flags. We should also use team specific Unity.Physics categories for hitcolliders
                            ExcludedOwner = ownerEntity,
                            Start = startPos,
                            End = (float3)startPos + direction*distance,
                            Radius = settings.hitscanRadius
                        };

                        CollisionWorld collWorld;
                        collisionHistoryBuffer.GetCollisionWorldFromTick(userCmd.command.renderTick, out collWorld);

                        var result = new HitCollisionQuery.ProjectileQueryResult();
                        HitCollisionQuery.Query(in collWorld, in query, ref result);

                        if (result.Hit)
                        {
                            //GameDebug.Log(worldId,null,"BANG Hit");

                            var hitColliderOwner = result.ColliderOwner != Entity.Null;
                            if (hitColliderOwner)
                            {
                                var damageEvent = new DamageEvent
                                {
                                    Target = result.ColliderOwner,
                                    Instigator = ownerEntity,
                                    Damage = settings.damage,
                                    Direction = math.normalize(query.End - query.Start),
                                    Impulse = settings.damageImpulse
                                };
                                damageEvents.Add(result.ColliderOwner, damageEvent);
                            }

                            interpState.fireEndPos = result.Position;
                            interpState.impactType = hitColliderOwner ? ImpactType.Character : ImpactType.Environment;
                            interpState.impactNormal = result.Normal;
                        }
                        else
                        {
                            interpState.fireEndPos = query.End;
                        }
                    }
                }
            }
        }

        protected override JobHandle OnUpdate(JobHandle inputDeps)
        {
            inputDeps.Complete();

            var gameTime = GetEntityQuery(ComponentType.ReadOnly<GlobalGameTime>()).GetSingleton<GlobalGameTime>().gameTime;

            var damageManager = World.GetExistingSystem<DamageManager>();
            if (damageManager == null)
            {
                GameDebug.LogError("Could not find DamageManager system");
                return default;
            }

            var physWorldHist = World.GetExistingSystem<PhysicsWorldHistory>();
            if (physWorldHist == null)
                return default;

            var barrier = World.GetExistingSystem<AbilityUpdateCommandBufferSystem>();

            JobHandle damageDeps;
            var updateJob = new UpdateJob
            {
                time = gameTime,
                worldId = World,
                aimDataFromEntity = GetComponentDataFromEntity<AimData.Data>(true),
                playerControlledStateFromEntity = GetComponentDataFromEntity<PlayerControlled.State>(true),
                hitColliderOwnerFromEntity = GetComponentDataFromEntity<HitCollider.Owner>(true),
                hitColliderOwnerStateFromEntity = GetComponentDataFromEntity<HitColliderOwner.State>(true),
                enabledEntityFromEntity = GetComponentDataFromEntity<Ability.EnabledAbility>(true),
                damageEvents = damageManager.GetDamageBufferWriter(out damageDeps),
                commands = barrier.CreateCommandBuffer(),
                collisionHistoryBuffer = physWorldHist.CollisionHistory,
            };
            damageDeps.Complete();

            Entities
                .WithoutBurst()    // TODO (mogensh) Disabled as we have crash and a useable callstack would be nice
                .ForEach((Entity entity,
                    ref Ability.AbilityStateActive stateActive, ref PredictedState predictedState, ref InterpolatedState interpState,
                    ref Settings settings, ref State internalState, ref Ability.AbilityAction abilityAction) =>
            {
                updateJob.Execute(entity, ref stateActive, ref predictedState, ref interpState, ref settings, ref internalState, ref abilityAction);
            }).Run();

            return default;
        }
    }

    [UpdateInGroup(typeof(AbilityUpdatePhase))]
    [DisableAutoCreation]
    [AlwaysSynchronizeSystem]
    class CooldownUpdate : JobComponentSystem
    {
        protected override JobHandle OnUpdate(JobHandle inputDeps)
        {
            inputDeps.Complete();

            var time = GetEntityQuery(ComponentType.ReadOnly<GlobalGameTime>()).GetSingleton<GlobalGameTime>().gameTime;

            Entities
                .ForEach((ref Ability.EnabledAbility enabledAbility,
                    ref Ability.AbilityStateCooldown stateCooldown, ref PredictedState predictedState,
                    ref Settings settings, ref State internalState) =>
            {
                DecreaseCone(ref predictedState, ref settings, time.tickDuration);

                stateCooldown.requestIdle = true;
            }).Run();

            return default;
        }
    }
}
