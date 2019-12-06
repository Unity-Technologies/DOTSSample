using Unity.Entities;
using UnityEngine;

#if UNITY_EDITOR

public class PlayerAuthoring : MonoBehaviour, IConvertGameObjectToEntity
{
    public void Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
    {
        dstManager.AddComponentData(entity,new Player.State());
    }
}

#endif
