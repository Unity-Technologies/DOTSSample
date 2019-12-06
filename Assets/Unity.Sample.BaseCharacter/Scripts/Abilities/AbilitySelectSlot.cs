using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using Unity.Jobs;
using Unity.NetCode;
using UnityEngine;


public class AbilitySelectSlot
{
    public enum Phase
    {
        Idle,
        Active,
    }

    public const Ability.AbilityTagValue Tag = Ability.AbilityTagValue.SelectSlot;

    public struct Settings : IComponentData
    {
        public float changeDuration;
    }

    public struct State : IComponentData
    {
        public sbyte requestedSlot;
        public Phase phase;
        public int phaseStartTick;
    }


    [UpdateInGroup(typeof(BehaviourRequestPhase))]
    [DisableAutoCreation]
    [AlwaysSynchronizeSystem]
    public class RequestActive : JobComponentSystem
    {
        protected override JobHandle OnUpdate(JobHandle inputDeps)
        {
            inputDeps.Complete();

            Entities
                .ForEach((ref Ability.EnabledAbility enabledAbility, ref Ability.AbilityStateIdle stateIdle, ref State state) =>
            {
                stateIdle.requestActive = enabledAbility.activeButtonIndex != -1;

                if (stateIdle.requestActive)
                {
                    state.requestedSlot = (sbyte)enabledAbility.activeButtonIndex;
                    state.phase = Phase.Idle;
                }
            }).Run();

            return default;
        }
    }

    [UpdateInGroup(typeof(AbilityUpdatePhase))]
    [DisableAutoCreation]
    [AlwaysSynchronizeSystem]
    class ActiveUpdate : JobComponentSystem
    {
        protected override JobHandle OnUpdate(JobHandle inputDeps)
        {
            inputDeps.Complete();

            var barrier = World.GetExistingSystem<AbilityUpdateCommandBufferSystem>();

            var PredictingTick = World.GetExistingSystem<GhostPredictionSystemGroup>().PredictingTick;
            var time = GetEntityQuery(ComponentType.ReadOnly<GlobalGameTime>()).GetSingleton<GlobalGameTime>().gameTime;
            var inventoryStateFromEntity = GetComponentDataFromEntity<Inventory.State>(true);
            var commands = barrier.CreateCommandBuffer();

            Entities
                .ForEach((Entity entity, ref PredictedGhostComponent predictedEntity, ref Ability.EnabledAbility enabledAbility, ref Ability.AbilityStateActive stateActive, ref State state) =>
            {
                if (!GhostPredictionSystemGroup.ShouldPredict(PredictingTick, predictedEntity))
                    return;

                switch (state.phase)
                {
                    case Phase.Idle:
                        {
                            state.phase = Phase.Active;
                            state.phaseStartTick = time.tick;
                            break;
                        }

                    case Phase.Active:
                        {
                            if (time.DurationSinceTick(state.phaseStartTick) > 0.5f)
                            {
                                //GameDebug.Log("SelectSlot Done");

                                stateActive.requestCooldown = true;
                                state.phase = Phase.Idle;
                                state.phaseStartTick = time.tick;

                                // TODO (mogensh) this should only be set on server. How to enforce this?
                                var inventory = inventoryStateFromEntity[enabledAbility.owner];
                                inventory.activeSlot = state.requestedSlot;
                                commands.SetComponent(enabledAbility.owner, inventory);
                            }
                            break;
                        }
                }
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

            Entities
                .ForEach((ref State state, ref Ability.AbilityStateCooldown cooldownState) =>
            {
                cooldownState.requestIdle = true;
            }).Run();

            return default;
        }
    }
}
