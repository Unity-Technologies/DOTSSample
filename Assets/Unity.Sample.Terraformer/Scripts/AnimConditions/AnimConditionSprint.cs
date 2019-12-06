using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using Unity.Jobs;
using UnityEngine;


public class AnimConditionSprint
{
    public struct State : IComponentData
    {
        public static State Default => new State();

        public bool notanemptycomponent;
    }

    [UpdateInGroup(typeof(AnimConditionUpdate))]
    [DisableAutoCreation]
    [AlwaysSynchronizeSystem]
    class Update : JobComponentSystem
    {
        protected override JobHandle OnUpdate(JobHandle inputDeps)
        {
            var AbilitySprintPredictedState = GetComponentDataFromEntity<AbilitySprint.PredictedState>(true);
            var OwnedAbilityBufferFromEntity = GetBufferFromEntity<AbilityOwner.OwnedAbility>(true);
            Entities
                .WithReadOnly(AbilitySprintPredictedState)
                .WithoutBurst() // captures managed data
                .ForEach((Entity entity, ref DecisionTreeNode.State condition, ref State state) =>
                {
                    condition.isTrue = false;

                    if (!EntityManager.Exists(condition.owner))
                        return;

                    var abilityEntity = Ability.FindAbility(OwnedAbilityBufferFromEntity, condition.owner, AbilitySprint.Tag);
                    if (abilityEntity == Entity.Null)
                        return;

                    var ability = AbilitySprintPredictedState[abilityEntity];
                    if (ability.active != 1)
                        return;

                    condition.isTrue = true;
                }).Run();
            return default;
        }
    }
}
