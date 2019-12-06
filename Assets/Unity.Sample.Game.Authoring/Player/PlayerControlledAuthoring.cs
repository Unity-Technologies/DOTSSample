using Unity.Entities;
using UnityEngine;
using Unity.NetCode;

#if UNITY_EDITOR

public class PlayerControlledAuthoring : MonoBehaviour, IConvertGameObjectToEntity
{
    public void Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
    {
        dstManager.AddComponentData(entity,new PlayerControlled.State());
    }
}

#endif
