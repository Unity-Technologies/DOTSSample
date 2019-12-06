using Unity.Entities;
using UnityEngine;


#if UNITY_EDITOR

public class AbilityAuthoring : MonoBehaviour, IConvertGameObjectToEntity
{
    public void Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
    {
        dstManager.AddComponentData(entity, new Ability.AbilityControl());
    }
}

#endif


