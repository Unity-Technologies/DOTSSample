using Unity.Entities;
using UnityEngine;

public class AbilitySelectSlotAuthoring : MonoBehaviour, IConvertGameObjectToEntity
{
    public AbilitySelectSlot.Settings settings;

    public void Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
    {
        dstManager.AddComponentData(entity, new Ability.AbilityTag { Value = Ability.AbilityTagValue.SelectSlot });
        dstManager.AddComponentData(entity,settings);
        dstManager.AddComponentData(entity, new AbilitySelectSlot.State());

#if UNITY_EDITOR
        dstManager.SetName(entity,name);
#endif
    }
}
