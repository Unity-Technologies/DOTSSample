using Unity.Entities;
using UnityEngine;

public class AbilitySprintAuthoring : MonoBehaviour, IConvertGameObjectToEntity
{
    public AbilitySprint.Settings settings;

    public void Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
    {
        dstManager.AddComponentData(entity, new Ability.AbilityTag { Value = Ability.AbilityTagValue.Sprint });
        dstManager.AddComponentData(entity, new AbilitySprint.PredictedState());
        dstManager.AddComponentData(entity, settings);

#if UNITY_EDITOR
        dstManager.SetName(entity,name);
#endif
    }
}
