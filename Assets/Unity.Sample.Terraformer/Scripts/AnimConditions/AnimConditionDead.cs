using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using Unity.Jobs;
using UnityEngine;


// TODO (mogensh) when to update these. Does it even make sense to update all conditions in tree ?
public class AnimConditionDead
{
    public struct State : IComponentData
    {
        public static State Default => new State();
        public int Bar;
    }

    [UpdateInGroup(typeof(AnimConditionUpdate))]
    [DisableAutoCreation]
    [AlwaysSynchronizeSystem]
    class Update : JobComponentSystem
    {
        protected override JobHandle OnUpdate(JobHandle inputDeps)
        {
            var AbilityStateActiveFromEntity = GetComponentDataFromEntity<Ability.AbilityStateActive>(true);
            var OwnedAbilityBufferFromEntity = GetBufferFromEntity<AbilityOwner.OwnedAbility>(true);
            Entities
                .WithReadOnly(AbilityStateActiveFromEntity)
                .WithoutBurst() // Captures managed data
                .ForEach((Entity entity, ref DecisionTreeNode.State condition, ref State state) =>
                {
                    condition.isTrue = false;

                    if (!EntityManager.Exists(condition.owner))
                        return;

                    var abilityEntity =
                        Ability.FindAbility(OwnedAbilityBufferFromEntity, condition.owner, AbilityDead.Tag);
                    if (abilityEntity == Entity.Null)
                        return;

                    condition.isTrue = AbilityStateActiveFromEntity.HasComponent(abilityEntity);

//                GameDebug.Log(World,null,"Condition:{0}",condition.isTrue);
                }).Run();
            return default;
        }
    }
}
