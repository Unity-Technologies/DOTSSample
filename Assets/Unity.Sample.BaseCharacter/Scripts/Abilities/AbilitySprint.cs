using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using Unity.Jobs;
using Unity.NetCode;
using Unity.Sample.Core;
using UnityEngine;


public class AbilitySprint
{
    public const Ability.AbilityTagValue Tag = Ability.AbilityTagValue.Sprint;

    [Serializable]
    public struct Settings : IComponentData
    {
        public UserCommand.Button activateButton;
        public float stopDelay;
    }

    public struct PredictedState : IComponentData
    {
        [GhostDefaultField]
        public int active;
        [GhostDefaultField]
        public int terminating;
        [GhostDefaultField]
        public int terminateStartTick;

#if UNITY_EDITOR
        public bool VerifyPrediction(ref PredictedState state)
        {
            return true;
        }

        public override string ToString()
        {
            return "Sprint.State active:" + active + " terminating:" + terminating;
        }

#endif
    }

    [UpdateInGroup(typeof(BehaviourRequestPhase))]
    [DisableAutoCreation]
    [AlwaysSynchronizeSystem]
    public class IdleStateUpdate : JobComponentSystem
    {
        protected override JobHandle OnUpdate(JobHandle inputDeps)
        {
            inputDeps.Complete();

            var playerControlledStateFromEntity = GetComponentDataFromEntity<PlayerControlled.State>(true);
            Entities
                .ForEach((ref Ability.EnabledAbility activeAbility, ref Ability.AbilityStateIdle stateIdle, ref Settings settings) =>
            {
                var command = playerControlledStateFromEntity[activeAbility.owner].command;
                stateIdle.requestActive = activeAbility.activeButtonIndex == 0 && SprintAllowed(in command);
            }).Run();

            return default;
        }
    }

    static bool SprintAllowed(in UserCommand cmd)
    {
        var sprintAllowed = cmd.moveMagnitude > 0 && (cmd.moveYaw < 90.0f || cmd.moveYaw > 270);
        return sprintAllowed;
    }

    [UpdateInGroup(typeof(AbilityUpdatePhase))]
    [DisableAutoCreation]
    [AlwaysSynchronizeSystem]
    public class ActiveStateUpdate : JobComponentSystem
    {
        protected override JobHandle OnUpdate(JobHandle inputDeps)
        {
            inputDeps.Complete();

            var barrier = World.GetExistingSystem<AbilityUpdateCommandBufferSystem>();

            var time = GetEntityQuery(ComponentType.ReadOnly<GlobalGameTime>()).GetSingleton<GlobalGameTime>().gameTime;
            var characterPredictedDataFromEntity = GetComponentDataFromEntity<Character.PredictedData>(true);
            var playerControlledStateFromEntity = GetComponentDataFromEntity<PlayerControlled.State>(true);
            var abilityRequestDeactivateFromEntity = GetComponentDataFromEntity<Ability.AbilityRequestDeactivate>(true);
            var commands = barrier.CreateCommandBuffer();
            Entities
                .ForEach((Entity entity, ref Ability.EnabledAbility activeAbility, ref Ability.AbilityStateActive stateActive, ref PredictedState predictedState, ref Settings settings) =>
            {
                var charPredictedState = characterPredictedDataFromEntity[activeAbility.owner];

                var command = playerControlledStateFromEntity[activeAbility.owner].command;
                var sprintAllowed = SprintAllowed(in command);
                
                var sprintRequested = sprintAllowed && activeAbility.activeButtonIndex == 0;
                if (sprintRequested && predictedState.active == 0)
                {
                    predictedState.active = 1;
                    predictedState.terminating = 0;
                }

                var deactivateRequested = abilityRequestDeactivateFromEntity.HasComponent(entity);
                var startTerminate = !sprintAllowed || deactivateRequested;
                if (startTerminate && predictedState.active == 1 && predictedState.terminating == 0)
                {
                    predictedState.terminating = 1;
                    predictedState.terminateStartTick = time.tick;
                }

                if (predictedState.terminating == 1 && time.DurationSinceTick(predictedState.terminateStartTick) >
                    settings.stopDelay)
                {
                    predictedState.active = 0;
                    stateActive.requestCooldown = true;
                }

                charPredictedState.sprinting = predictedState.active == 1 && predictedState.terminating == 0;

                commands.SetComponent(activeAbility.owner, charPredictedState);
            }).Run();

            return default;
        }
    }

    [UpdateInGroup(typeof(AbilityUpdatePhase))]
    [DisableAutoCreation]
    [AlwaysSynchronizeSystem]
    public class CooldownStateUpdate : JobComponentSystem
    {
        protected override JobHandle OnUpdate(JobHandle inputDeps)
        {
            inputDeps.Complete();

            Entities
                .ForEach((ref Ability.EnabledAbility activeAbility, ref Ability.AbilityStateCooldown stateCooldown, ref PredictedState predictedState) =>
            {
                predictedState.active = 0;
                stateCooldown.requestIdle = true;
            }).Run();

            return default;
        }
    }
}

