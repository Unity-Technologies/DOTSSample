using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using Unity.NetCode;
using UnityEngine;


public class AbilityMelee
{
    public enum Phase
    {
        Idle,
        Punch,
        Hold,
    }

    public const Ability.AbilityTagValue Tag = Ability.AbilityTagValue.Melee;

    public struct State : IComponentData
    {
        public int lastHitCheckTick;
        public int rayQueryId;
    }

    [Serializable]
    public struct Settings : IComponentData
    {
        public UserCommand.Button activateButton;

        public float damage;
        public float damageDist;
        public float damageRadius;
        public float damageImpulse;
        public float impactTime;
        public int punchPerSecond;

        public Ability.AbilityAction.Action punchAction;
    }

    public struct PredictedState : IComponentData
    {
        [GhostDefaultField]
        public Phase phase;
        [GhostDefaultField]
        public int phaseStartTick;
    }

    public struct InterpolatedState : IComponentData
    {
        [GhostDefaultField]
        public int impactTick;
    }
}

//
//[UpdateInGroup(typeof(BehaviourRequestPhase))]
//public class Melee_RequestActive : ComponentSystem
//{
//    protected override void OnUpdate()
//    {
//        Entities.WithAll<AbilityStateIdle>().ForEach((Entity entity, ref EnabledAbility activeAbility,
//            ref AbilityControl abilityCtrl, ref AbilityStateIdle stateIdle, ref Ability_Melee.Settings settings) =>
//            {
//                var command = EntityManager.GetComponentData<PlayerControlled.State>(activeAbility.owner).command;
//                stateIdle.requestActive = command.buttons.IsSet(settings.activateButton);
//            });
//    }
//}
//
//
//[UpdateInGroup(typeof(AbilityUpdatePhase))]
//public class Melee_Update : ComponentSystem
//{
//    protected override void OnUpdate()
//    {
//        // TODO (mogensh) find cleaner way to get time
//        var globalTime = GetEntityQuery(ComponentType.ReadOnly<GlobalGameTime>()).GetSingleton<GlobalGameTime>();
//        var time = globalTime.gameTime;
//
//        Entities.ForEach((Entity entity, ref EnabledAbility activeAbility,
//            ref AbilityControl abilityCtrl, ref Ability_Melee.PredictedState predictedState,
//            ref Ability_Melee.State localState, ref Ability_Melee.Settings settings) =>
//            {
//                if (!abilityCtrl.active)
//                {
//                    if (predictedState.phase != Ability_Melee.Phase.Idle)
//                        predictedState.SetPhase(Ability_Melee.Phase.Idle, time.tick);
//                    EntityManager.SetComponentData(entity, predictedState);
//                    return;
//                }
//
//                switch (predictedState.phase)
//                {
//                    case Ability_Melee.Phase.Idle:
//                        {
//                            var charPredictedState = EntityManager.GetComponentData<Character.PredictedData>(activeAbility.owner);
//
//                            abilityCtrl.behaviorState = AbilityControl.State.Active;
//                            predictedState.SetPhase(Ability_Melee.Phase.Punch, time.tick);
//                            charPredictedState.SetAction(settings.punchAction, time.tick);
//
//                            EntityManager.SetComponentData(entity, abilityCtrl);
//                            EntityManager.SetComponentData(entity, predictedState);
//                            EntityManager.SetComponentData(activeAbility.owner, charPredictedState);
//
//                            break;
//                        }
//                    case Ability_Melee.Phase.Punch:
//                        {
//                            var phaseDuration = time.DurationSinceTick(predictedState.phaseStartTick);
//                            if (phaseDuration >= settings.impactTime)
//                            {
//                                var charState = EntityManager.GetComponentData<Character.State>(activeAbility.owner);
//                                var charPredictedState = EntityManager.GetComponentData<Character.PredictedData>(activeAbility.owner);
//                                var command = EntityManager.GetComponentData<PlayerControlled.State>(activeAbility.owner).command;
//                                var viewDir = command.lookDir;
//                                var eyePos = charPredictedState.position + Vector3.up * charState.eyeHeight;
//
//                                predictedState.SetPhase(Ability_Melee.Phase.Hold, time.tick);
//
//                                var queryReciever = World.GetExistingSystem<RaySphereQueryReciever>();
//                                localState.rayQueryId = queryReciever.RegisterQuery(new RaySphereQueryReciever.Query()
//                                {
//                                    origin = eyePos,
//                                    direction = viewDir,
//                                    distance = settings.damageDist,
//                                    ExcludeOwner = activeAbility.owner,
//                                    hitCollisionTestTick = command.renderTick,
//                                    radius = settings.damageRadius,
//                                    mask = ~0U,
//                                });
//
//                                EntityManager.SetComponentData(entity, localState);
//                                EntityManager.SetComponentData(entity, predictedState);
//
//                                break;
//                            }
//                            break;
//                        }
//                    case Ability_Melee.Phase.Hold:
//                        {
//                            var holdEndDuration = 1.0f / settings.punchPerSecond - settings.impactTime;
//                            var phaseDuration = time.DurationSinceTick(predictedState.phaseStartTick);
//                            if (phaseDuration > holdEndDuration)
//                            {
//                                var charPredictedState = EntityManager.GetComponentData<Character.PredictedData>(activeAbility.owner);
//
//                                abilityCtrl.behaviorState = AbilityControl.State.Idle;
//                                predictedState.SetPhase(Ability_Melee.Phase.Idle, time.tick);
//                                charPredictedState.SetAction(Character.PredictedData.Action.None, time.tick);
//
//                                EntityManager.SetComponentData(entity, abilityCtrl);
//                                EntityManager.SetComponentData(entity, predictedState);
//                                EntityManager.SetComponentData(activeAbility.owner, charPredictedState);
//
//                                break;
//                            }
//
//                            break;
//                        }
//                }
//            });
//    }
//}
//
//[UpdateInGroup(typeof(AbilityResolvePhase))]
//public class Melee_HandleCollision : ComponentSystem
//{
//    protected override void OnUpdate()
//    {
//        // TODO (mogensh) find cleaner way to get time
//        var globalTime = GetEntityQuery(ComponentType.ReadOnly<GlobalGameTime>()).GetSingleton<GlobalGameTime>();
//        var time = globalTime.gameTime;
//
//        Entities.ForEach((Entity entity, ref EnabledAbility activeAbility,
//            ref AbilityControl abilityCtrl, ref Ability_Melee.State localState,
//            ref Ability_Melee.Settings settings) =>
//            {
//                if (localState.rayQueryId == -1)
//                    return;
//
//                var queryReciever = World.GetExistingSystem<RaySphereQueryReciever>();
//
//                RaySphereQueryReciever.Query query;
//                RaySphereQueryReciever.QueryResult queryResult;
//                queryReciever.GetResult(localState.rayQueryId, out query, out queryResult);
//                localState.rayQueryId = -1;
//
//                if (queryResult.hitCollisionOwner != Entity.Null)
//                {
//                    var charAbility = EntityManager.GetComponentData<EnabledAbility>(entity);
//
//                    var damageEventBuffer = EntityManager.GetBuffer<DamageEvent>(queryResult.hitCollisionOwner);
//                    DamageEvent.AddEvent(damageEventBuffer, charAbility.owner, settings.damage, query.direction, settings.damageImpulse);
//
//                    var interpolatedState = EntityManager.GetComponentData<Ability_Melee.InterpolatedState>(entity);
//                    interpolatedState.impactTick = time.tick;
//                    PostUpdateCommands.SetComponent(entity, interpolatedState);
//                }
//
//                EntityManager.SetComponentData(entity, localState);
//            });
//    }
//}
