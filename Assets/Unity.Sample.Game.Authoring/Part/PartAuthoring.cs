using Unity.Entities;
using UnityEngine;

public class PartAuthoring : MonoBehaviour, IConvertGameObjectToEntity
{
    public void Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
    {
        dstManager.AddComponent<Part.Owner>(entity);
    }
}

