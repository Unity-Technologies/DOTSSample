using Unity.Entities;
using UnityEngine;

public class AbilityDeadAuthoring : MonoBehaviour, IConvertGameObjectToEntity
{
    public void Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
    {
        dstManager.AddComponentData(entity, new Ability.AbilityTag { Value = Ability.AbilityTagValue.Dead });
        dstManager.AddComponentData(entity, new AbilityDead.State());

#if UNITY_EDITOR
        dstManager.SetName(entity,name);
#endif
    }
}


