using Unity.Entities;
using UnityEngine;

public class AbilityMeleeAuthoring : MonoBehaviour, IConvertGameObjectToEntity
{
    public AbilityMelee.Settings settings;

    public void Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
    {
        // Ability components
        dstManager.AddComponentData(entity, new Ability.AbilityTag { Value = Ability.AbilityTagValue.Melee });
        dstManager.AddComponentData(entity, settings);
        var localState = new AbilityMelee.State
        {
            rayQueryId = -1,
        };
        dstManager.AddComponentData(entity, localState);
        dstManager.AddComponentData(entity, new AbilityMelee.PredictedState());
        dstManager.AddComponentData(entity, new AbilityMelee.InterpolatedState());
        dstManager.AddComponentData(entity, Ability.AbilityAction.Default );
    }
}
