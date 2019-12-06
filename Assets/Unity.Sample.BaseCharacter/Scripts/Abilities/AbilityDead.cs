using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using Unity.Jobs;
using Unity.NetCode;
using UnityEngine;

public class AbilityDead
{
    public const Ability.AbilityTagValue Tag = Ability.AbilityTagValue.Dead;

    public struct State : IComponentData
    {
        public bool activated;
    }

    [UpdateInGroup(typeof(BehaviourRequestPhase))]
    [DisableAutoCreation]
    [AlwaysSynchronizeSystem]
    public class Dead_RequestActive : JobComponentSystem
    {
        protected override JobHandle OnUpdate(JobHandle inputDeps)
        {
            inputDeps.Complete();

            var healthStateDataFromEntity = GetComponentDataFromEntity<HealthStateData>(true);
            Entities
                .ForEach((ref Ability.EnabledAbility enabledAbility, ref Ability.AbilityStateIdle stateIdle) =>
            {
                var healthState = healthStateDataFromEntity[enabledAbility.owner];
                if (healthState.health <= 0)
                {
                    stateIdle.requestActive = true;
                }
            }).Run();

            return default;
        }
    }

    [UpdateInGroup(typeof(AbilityUpdatePhase))]
    [DisableAutoCreation]
    [AlwaysSynchronizeSystem]
    public class Dead_Update : JobComponentSystem
    {
        protected override JobHandle OnUpdate(JobHandle inputDeps)
        {
            inputDeps.Complete();

            var barrier = World.GetExistingSystem<AbilityUpdateCommandBufferSystem>();

            var characterPredictedDataFromEntity = GetComponentDataFromEntity<Character.PredictedData>(true);
            var predictedEntityComponentFromEntity = GetComponentDataFromEntity<PredictedGhostComponent>(true);
            var predictingTick = World.GetExistingSystem<GhostPredictionSystemGroup>().PredictingTick;
            var commands = barrier.CreateCommandBuffer();

            Entities
                .ForEach((ref Ability.EnabledAbility activeAbility, ref Ability.AbilityStateActive stateActive, ref State internalState) =>
            {
                if (predictedEntityComponentFromEntity.HasComponent(activeAbility.owner) && !GhostPredictionSystemGroup.ShouldPredict(predictingTick, predictedEntityComponentFromEntity[activeAbility.owner]))
                    return;

                if (!internalState.activated)
                {
                    var charPredictedState = characterPredictedDataFromEntity[activeAbility.owner];
                    charPredictedState.cameraProfile = CameraProfile.ThirdPerson;
                    commands.SetComponent(activeAbility.owner, charPredictedState);
                    internalState.activated = true;
                }

//                GameDebug.Log(World, null, "Dead update");
            }).Run();

            return default;
        }
    }
}
