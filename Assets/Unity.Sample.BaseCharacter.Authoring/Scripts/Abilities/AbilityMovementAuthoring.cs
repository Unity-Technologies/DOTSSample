using Unity.Entities;
using UnityEngine;

public class AbilityMovementAuthoring : MonoBehaviour, IConvertGameObjectToEntity
{
    public AbilityMovement.Settings settings;

    public void Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
    {
        dstManager.AddComponentData(entity, new Ability.AbilityTag { Value = Ability.AbilityTagValue.Movement });
        dstManager.AddComponentData(entity,settings);
        dstManager.AddComponentData(entity, new AbilityMovement.PredictedState());
        dstManager.AddComponentData(entity, new AbilityMovement.InterpolatedState());

#if UNITY_EDITOR
        dstManager.SetName(entity,name);
#endif
    }
}
