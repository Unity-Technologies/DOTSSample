using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using Unity.Jobs;
using UnityEngine;


public class AnimConditionMovement
{
    public struct State : IComponentData
    {
        public static State Default => new State();

        public AbilityMovement.LocoState requiredLocoState;
    }

    [UpdateInGroup(typeof(AnimConditionUpdate))]
    [DisableAutoCreation]
    [AlwaysSynchronizeSystem]
    class Update : JobComponentSystem
    {
        protected override JobHandle OnUpdate(JobHandle inputDeps)
        {
            var AbilityMovementInterpolatedStateFromEntity =
                GetComponentDataFromEntity<AbilityMovement.InterpolatedState>(true);
            var OwnedAbilityBufferFromEntity = GetBufferFromEntity<AbilityOwner.OwnedAbility>(true);
            Entities
                .WithReadOnly(AbilityMovementInterpolatedStateFromEntity)
                .WithoutBurst() // Captures managed data
                .ForEach((Entity entity, ref DecisionTreeNode.State condition, ref State state) =>
                {
                    condition.isTrue = false;

                    if (!EntityManager.Exists(condition.owner))
                        return;

                    var abilityEntity =
                        Ability.FindAbility(OwnedAbilityBufferFromEntity, condition.owner, AbilityMovement.Tag);
                    if (abilityEntity == Entity.Null)
                        return;

                    var ability = AbilityMovementInterpolatedStateFromEntity[abilityEntity];
                    if (ability.charLocoState != state.requiredLocoState)
                        return;

                    condition.isTrue = true;
                }).Run();
            return default;
        }
    }
}
