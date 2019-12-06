using System;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.NetCode;
using Unity.Sample.Core;
using UnityEngine;


public class AbilityMovement
{
    public enum LocoState
    {
        Stand,
        GroundMove,
        Jump,
        DoubleJump,
        InAir,
        MaxValue
    }

    public const Ability.AbilityTagValue Tag = Ability.AbilityTagValue.Movement;

    [Serializable]
    public struct Settings : IComponentData
    {
        public float playerSpeed;
        public float playerSprintSpeed;
        public float playerAcceleration;
        public float playerFriction;
        public float playerAiracceleration;
        public float playerAirFriction;
        public float playerGravity;// TODO (mogensh) some of these should be moved to global character game config
        public bool easterBunny;
        public float jumpAscentDuration;
        public float jumpAscentHeight;
        public float maxFallVelocity;
    }

    public struct PredictedState : IComponentData
    {
        [GhostDefaultField] 
        public LocoState locoState;
        [GhostDefaultField]
        public int locoStartTick;
        [GhostDefaultField]
        public int jumpCount;
//        public bool sprinting;
        [GhostDefaultField]
        public bool crouching;
        public int lastGroundMoveTick;

        public bool IsOnGround()
        {
            return locoState == LocoState.Stand || locoState == LocoState.GroundMove;
        }
    }

    public struct InterpolatedState : IComponentData
    {
        [GhostDefaultField]
        public LocoState charLocoState;
        [GhostDefaultField]
        public int charLocoTick;
        public LocoState previousCharLocoState;
        [GhostDefaultField]
        public bool crouching;
    }

    [UpdateInGroup(typeof(BehaviourRequestPhase))]
    [DisableAutoCreation]
    [AlwaysSynchronizeSystem]
    public class IdleUpdate : JobComponentSystem
    {
        protected override JobHandle OnUpdate(JobHandle inputDeps)
        {
            inputDeps.Complete();

            Entities
                .ForEach((ref Ability.AbilityStateIdle stateIdle, ref Ability.EnabledAbility enabledAbility, ref InterpolatedState interpolatedState) =>
            {
                stateIdle.requestActive = true;
            }).Run();

            return default;
        }
    }

    [UpdateInGroup(typeof(MovementUpdatePhase))]
    [UpdateBefore(typeof(ActiveUpdate))]
    [DisableAutoCreation]
    [AlwaysSynchronizeSystem]
    public class DrawDebugGraphs : JobComponentSystem
    {
        [ConfigVar(Name = "debug.charactermove", Description = "Show graphs of one character's movement along x, y, z", DefaultValue = "0")]
        public static ConfigVar debugCharacterMove;

        // Debugging graphs to show player movement in 3 axis
        static float[] movehist_x = new float[100];
        static float[] movehist_y = new float[100];
        static float[] movehist_z = new float[100];
        static float lastUsedFrame;

        protected override JobHandle OnUpdate(JobHandle inputDeps)
        {
            inputDeps.Complete();

            if (debugCharacterMove.IntValue == 0)
                return default;

            // TODO (mogensh) find cleaner way to get time
            var globalTime = GetEntityQuery(ComponentType.ReadOnly<GlobalGameTime>()).GetSingleton<GlobalGameTime>();
            var time = globalTime.gameTime;
            var frameCount = UnityEngine.Time.frameCount;

            Entities
                .WithoutBurst() // Captures managed state
                .ForEach((Entity entity, ref Ability.EnabledAbility activeAbility, ref Ability.AbilityStateActive stateActive, ref Settings settings, ref PredictedState predictedState) =>
            {
                if (!EntityManager.HasComponent<Character.PredictedData>(activeAbility.owner))
                    return;
                if (EntityManager.HasComponent<PredictedGhostComponent>(activeAbility.owner) && !GhostPredictionSystemGroup.ShouldPredict(World.GetExistingSystem<GhostPredictionSystemGroup>().PredictingTick, EntityManager.GetComponentData<PredictedGhostComponent>(activeAbility.owner)))
                    return;

                var charPredictedState = EntityManager.GetComponentData<Character.PredictedData>(activeAbility.owner);

                // Only show for one player
                if (lastUsedFrame < frameCount)
                {
                    lastUsedFrame = frameCount;

                    int o = frameCount % movehist_x.Length;
                    movehist_x[o] = charPredictedState.position.x % 10.0f;
                    movehist_y[o] = charPredictedState.position.y % 10.0f;
                    movehist_z[o] = charPredictedState.position.z % 10.0f;

                    DebugOverlay.DrawGraph(4, 4, 10, 5, movehist_x, o, Color.red, 10.0f);
                    DebugOverlay.DrawGraph(4, 12, 10, 5, movehist_y, o, Color.green, 10.0f);
                    DebugOverlay.DrawGraph(4, 20, 10, 5, movehist_z, o, Color.blue, 10.0f);
                }
            }).Run();

            return default;
        }
    }

    [UpdateInGroup(typeof(MovementUpdatePhase))]
    [DisableAutoCreation]
    [AlwaysSynchronizeSystem]
    public class ActiveUpdate : JobComponentSystem
    {
        readonly int m_platformLayer;
        public static int m_charCollisionALayer;
        public static int m_charCollisionBLayer;

        public ActiveUpdate()
        {
            m_platformLayer = LayerMask.NameToLayer("Platform");
            m_charCollisionALayer = LayerMask.NameToLayer("CharCollisionA");
            m_charCollisionBLayer = LayerMask.NameToLayer("CharCollisionB");
        }

        struct UpdateJob
        {
            public GameTime time;
            public int charCollisionALayer;
            public int charCollisionBLayer;
            public ComponentDataFromEntity<PlayerControlled.State> playerControlledStateFromEntity;
            public ComponentDataFromEntity<Character.PredictedData> characterPredictedDataFromEntity;
            public ComponentDataFromEntity<CharacterControllerMoveQuery> characterStartPositionFromEntity;
            public ComponentDataFromEntity<CharacterControllerGroundSupportData> characterGroundDataFromEntity;
            public ComponentDataFromEntity<CharacterControllerVelocity> characterVelocityFromEntity;
            public ComponentDataFromEntity<HealthStateData> healthStateFromEntity;
            [ReadOnly] public ComponentDataFromEntity<PredictedGhostComponent> predictedFromEntity;
            public uint PredictingTick;

            public void Execute(ref Ability.EnabledAbility activeAbility, ref Ability.AbilityStateActive stateActive, ref Settings settings, ref PredictedState predictedState)
            {
                if (!characterStartPositionFromEntity.HasComponent(activeAbility.owner))
                    return;
                if (predictedFromEntity.Exists(activeAbility.owner) && !GhostPredictionSystemGroup.ShouldPredict(PredictingTick, predictedFromEntity[activeAbility.owner]))
                    return;
                
                // TODO (mogensh) hack to disable input when dead. Ability should not be running, but as this is only ability that handles move request and gravity we keep it running and disable unput
                var healthState = healthStateFromEntity[activeAbility.owner];
                var alive = healthState.health > 0;
                var command = alive ? playerControlledStateFromEntity[activeAbility.owner].command : UserCommand.defaultCommand;

                var charPredictedState = characterPredictedDataFromEntity[activeAbility.owner];
                var startPosition = characterStartPositionFromEntity[activeAbility.owner];
                var groundState = characterGroundDataFromEntity[activeAbility.owner];

                var newPhase = LocoState.MaxValue;

                var phaseDuration = time.DurationSinceTick(predictedState.locoStartTick);

                var isOnGround = groundState.SupportedState != CharacterControllerUtilities.CharacterSupportState.Unsupported;
                var isMoveWanted = command.moveMagnitude != 0.0f;

                // Ground movement
                if (isOnGround)
                {
                    if (isMoveWanted)
                    {
                        newPhase = LocoState.GroundMove;
                    }
                    else
                    {
                        newPhase = LocoState.Stand;
                    }
                }

                // Jump
                if (isOnGround)
                    predictedState.jumpCount = 0;

                if (command.buttons.IsSet(UserCommand.Button.Jump) && isOnGround)
                {
                    predictedState.jumpCount = 1;
                    newPhase = LocoState.Jump;
                }

                if (command.buttons.IsSet(UserCommand.Button.Jump) && predictedState.locoState == LocoState.InAir && predictedState.jumpCount < 2)
                {
                    predictedState.jumpCount = predictedState.jumpCount + 1;
                    charPredictedState.velocity.y = 0;
                    newPhase = LocoState.DoubleJump;
                }

                if (predictedState.locoState == LocoState.Jump || predictedState.locoState == LocoState.DoubleJump)
                {
                    if (phaseDuration >= settings.jumpAscentDuration)
                    {
                        newPhase = LocoState.InAir;
                    }
                }

                // Set phase start tick if phase has changed
                if (newPhase != LocoState.MaxValue && newPhase != predictedState.locoState)
                {
                    predictedState.locoState = newPhase;
                    predictedState.locoStartTick = time.tick;
                }

                // Apply damange impulse from previus frame
                if (time.tick == charPredictedState.damageTick + 1)
                {
                    var inAirFactor = 1f;
                    if (!predictedState.IsOnGround())
                    {
                        inAirFactor = settings.playerAirFriction/settings.playerFriction;
                    }

//                    GameDebug.Log("Pushback:" + charPredictedState.damageImpulse + " factor:" + inAirFactor);


                    charPredictedState.velocity += charPredictedState.damageDirection * charPredictedState.damageImpulse*inAirFactor; 
                    predictedState.locoState = LocoState.InAir;
                    predictedState.locoStartTick = time.tick;
                }

                //                // Simple adjust of height while on platform
                //                if (predictedState.locoState == Character.CharacterPredictedData.LocoState.Stand &&
                //                    character.groundCollider != null &&
                //                    character.groundCollider.gameObject.layer == m_platformLayer)
                //                {
                //                    if (character.altitude < moveQuery.settings.skinWidth - 0.01f)
                //                    {
                //                        var platform = character.groundCollider;
                //                        var posY = platform.transform.position.y + moveQuery.settings.skinWidth;
                //                        predictedState.position.y = posY;
                //                    }
                //                }

                // Calculate movement and move character
                var newVelocity = CalculateVelocity(time, ref settings, ref predictedState, ref charPredictedState, command, out var followGround, out var checkSupport);

                // Setup movement query
                startPosition.StartPosition = charPredictedState.position;
                startPosition.FollowGround = followGround;
                startPosition.CheckSupport = checkSupport;

                var velocity = characterVelocityFromEntity[activeAbility.owner];

                charPredictedState.velocity = velocity.Velocity; // redundancy
                velocity.Velocity = newVelocity;

                characterStartPositionFromEntity[activeAbility.owner] = startPosition;
                characterVelocityFromEntity[activeAbility.owner] = velocity;
                characterPredictedDataFromEntity[activeAbility.owner] = charPredictedState;
            }

            float3 CalculateVelocity(GameTime gameTime, ref Settings settings, ref PredictedState predictedState, ref Character.PredictedData charPredicted, UserCommand command, out bool followGround, out bool checkSupport)
            {
                var velocity = charPredicted.velocity;
                switch (predictedState.locoState)
                {

                    case LocoState.Jump:
                    case LocoState.DoubleJump:
                        {
                            // In jump we overwrite velocity y component with linear movement up
                            velocity = CalculateGroundVelocity(velocity, ref command, settings.easterBunny, settings.playerSpeed, settings.playerAirFriction, settings.playerAiracceleration, gameTime.tickDuration);
                            velocity.y = settings.jumpAscentHeight / settings.jumpAscentDuration;
                            followGround = false;
                            checkSupport = false;
                            return velocity;
                        }

                    case LocoState.InAir:
                        {
                            var gravity = settings.playerGravity;
                            velocity += (float3)Vector3.down * gravity * gameTime.tickDuration;
                            velocity = CalculateGroundVelocity(velocity, ref command, settings.easterBunny, settings.playerSpeed, settings.playerAirFriction, settings.playerAiracceleration, gameTime.tickDuration);

                            if (velocity.y < -settings.maxFallVelocity)
                                velocity.y = -settings.maxFallVelocity;

                            followGround = false;
                            checkSupport = true;
                            return velocity;
                        }
                }

                {
                    var playerSpeed = charPredicted.sprinting ? settings.playerSprintSpeed : settings.playerSpeed;

                    velocity.y = 0;
                    velocity = CalculateGroundVelocity(velocity, ref command, settings.easterBunny, playerSpeed, settings.playerFriction, settings.playerAcceleration, gameTime.tickDuration);

                    followGround = true;
                    checkSupport = true;
                    return velocity;
                }
            }

            Vector3 CalculateGroundVelocity(Vector3 velocity, ref UserCommand command, bool easterBunny, float playerSpeed, float friction, float acceleration, float deltaTime)
            {
                var moveYawRotation = Quaternion.Euler(0, command.lookYaw + command.moveYaw, 0);
                var moveVec = moveYawRotation * Vector3.forward * command.moveMagnitude;

                // Applying friction
                var groundVelocity = new Vector3(velocity.x, 0, velocity.z);
                var groundSpeed = groundVelocity.magnitude;
                var frictionSpeed = Mathf.Max(groundSpeed, 1.0f) * deltaTime * friction;
                var newGroundSpeed = groundSpeed - frictionSpeed;
                if (newGroundSpeed < 0)
                    newGroundSpeed = 0;
                if (groundSpeed > 0)
                    groundVelocity *= (newGroundSpeed / groundSpeed);

                // Doing actual movement (q2 style)
                var wantedGroundVelocity = moveVec * playerSpeed;
                var wantedGroundDir = wantedGroundVelocity.normalized;
                var currentSpeed = Vector3.Dot(wantedGroundDir, groundVelocity);
                var wantedSpeed = playerSpeed * command.moveMagnitude;
                var deltaSpeed = wantedSpeed - currentSpeed;
                if (deltaSpeed > 0.0f)
                {
                    var accel = deltaTime * acceleration * playerSpeed;
                    var speed_adjustment = Mathf.Clamp(accel, 0.0f, deltaSpeed) * wantedGroundDir;
                    groundVelocity += speed_adjustment;
                }

                if (!easterBunny)
                {
                    newGroundSpeed = groundVelocity.magnitude;
                    if (newGroundSpeed > playerSpeed)
                        groundVelocity *= playerSpeed / newGroundSpeed;
                }

                velocity.x = groundVelocity.x;
                velocity.z = groundVelocity.z;

                return velocity;
            }
        }

        protected override JobHandle OnUpdate(JobHandle inputDeps)
        {
            inputDeps.Complete();

            var updateJob = new UpdateJob
            {
                time = GetEntityQuery(ComponentType.ReadOnly<GlobalGameTime>()).GetSingleton<GlobalGameTime>().gameTime,
                charCollisionALayer = m_charCollisionALayer,
                charCollisionBLayer = m_charCollisionBLayer,
                playerControlledStateFromEntity = GetComponentDataFromEntity<PlayerControlled.State>(true),
                characterPredictedDataFromEntity = GetComponentDataFromEntity<Character.PredictedData>(false),
                characterStartPositionFromEntity = GetComponentDataFromEntity<CharacterControllerMoveQuery>(false),
                characterGroundDataFromEntity = GetComponentDataFromEntity<CharacterControllerGroundSupportData>(true),
                characterVelocityFromEntity = GetComponentDataFromEntity<CharacterControllerVelocity>(false),
                healthStateFromEntity = GetComponentDataFromEntity<HealthStateData>(true),
                predictedFromEntity = GetComponentDataFromEntity<PredictedGhostComponent>(false),
                PredictingTick = World.GetExistingSystem<GhostPredictionSystemGroup>().PredictingTick
            };

            Entities
                .ForEach((ref Ability.EnabledAbility activeAbility, ref Ability.AbilityStateActive stateActive, ref Settings settings, ref PredictedState predictedState) =>
            {
                updateJob.Execute(ref activeAbility, ref stateActive, ref settings, ref predictedState);
            }).Run();

            return default;
        }
    }

    [UpdateInGroup(typeof(MovementResolvePhase))]
    [DisableAutoCreation]
    [AlwaysSynchronizeSystem]
    public class HandleCollision : JobComponentSystem
    {
        protected override JobHandle OnUpdate(JobHandle inputDeps)
        {
            inputDeps.Complete();

            var time = GetEntityQuery(ComponentType.ReadOnly<GlobalGameTime>()).GetSingleton<GlobalGameTime>().gameTime;
            var playerControlledStateFromEntity = GetComponentDataFromEntity<PlayerControlled.State>(true);
            var characterPredictedDataFromEntity = GetComponentDataFromEntity<Character.PredictedData>(false);
            var characterMoveResultFromEntity = GetComponentDataFromEntity<CharacterControllerMoveResult>(true);
            var characterVelocityFromEntity = GetComponentDataFromEntity<CharacterControllerVelocity>(true);
            var characterGroundStateFromEntity = GetComponentDataFromEntity<CharacterControllerGroundSupportData>(true);
            var predictedFromEntity = GetComponentDataFromEntity<PredictedGhostComponent>(false);
            var PredictingTick = World.GetExistingSystem<GhostPredictionSystemGroup>().PredictingTick;

            Entities
                .ForEach((ref Ability.EnabledAbility activeAbility, ref Ability.AbilityStateActive stateActive, ref Settings settings, ref InterpolatedState interpState, ref PredictedState predictedState) =>
            {
                if (!characterMoveResultFromEntity.HasComponent(activeAbility.owner))
                    return;
                if (predictedFromEntity.Exists(activeAbility.owner) && !GhostPredictionSystemGroup.ShouldPredict(PredictingTick, predictedFromEntity[activeAbility.owner]))
                    return;
                
                var charPredictedState = characterPredictedDataFromEntity[activeAbility.owner];
                var query = characterMoveResultFromEntity[activeAbility.owner];
                var velocity = characterVelocityFromEntity[activeAbility.owner];
                var groundData = characterGroundStateFromEntity[activeAbility.owner];
                var command = playerControlledStateFromEntity[activeAbility.owner].command;

                // Check for ground change (hitting ground or leaving ground) 
                var isOnGround = predictedState.IsOnGround();
                var newIsOnGround = groundData.SupportedState != CharacterControllerUtilities.CharacterSupportState.Unsupported;
                if (isOnGround != newIsOnGround)
                {
                    if (newIsOnGround)
                    {
                        if (command.moveMagnitude != 0.0f)
                        {
                            predictedState.locoState = LocoState.GroundMove;
                        }
                        else
                        {
                            predictedState.locoState = LocoState.Stand;
                        }
                    }
                    else
                    {
                        predictedState.locoState = LocoState.InAir;
                    }

                    predictedState.locoStartTick = time.tick;
                }


                // Manually calculate resulting velocity as characterController.velocity is linked to Time.deltaTime
                var newPos = query.MoveResult;
                var newVelocity = velocity.Velocity;

                charPredictedState.velocity = newVelocity;
                charPredictedState.position = newPos;

                characterPredictedDataFromEntity[activeAbility.owner] = charPredictedState;

                // Update interpolated state
                interpState.previousCharLocoState = interpState.charLocoState;

                // TODO: Had to disable this to make stop penalty feature in character system shared work
                //                    interpState.charLocoState = predictedState.locoState;
                interpState.charLocoTick = predictedState.locoStartTick;
                interpState.crouching = predictedState.crouching;
            }).Run();

            return default;
        }
    }

    [UpdateInGroup(typeof(MovementUpdatePhase))]
    [DisableAutoCreation]
    [AlwaysSynchronizeSystem]
    public class CooldownUpdate : JobComponentSystem
    {
        protected override JobHandle OnUpdate(JobHandle inputDeps)
        {
            inputDeps.Complete();

            Entities
                .ForEach((ref Settings settings, ref Ability.AbilityStateCooldown stateCooldown) =>
            {
                stateCooldown.requestIdle = true;
            }).Run();

            return default;
        }
    }
}

