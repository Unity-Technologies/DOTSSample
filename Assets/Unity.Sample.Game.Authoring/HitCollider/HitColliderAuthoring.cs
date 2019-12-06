using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using UnityEngine;

public class HitColliderAuthoring : MonoBehaviour,IConvertGameObjectToEntity
{
    public GameObject Owner;

    public void Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
    {
        var ownerEntity = conversionSystem.GetPrimaryEntity(Owner);
        var hitCollider = new HitCollider.Owner();
        hitCollider.Value = ownerEntity;
        dstManager.AddComponentData(entity, hitCollider);
    }
}
