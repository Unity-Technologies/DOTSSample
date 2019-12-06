using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using UnityEngine;

public class TurretAuthoring : MonoBehaviour, IConvertGameObjectToEntity
{
    public WeakAssetReference Projectile;

    public void Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
    {
        var state = Turret.State.Default;
        state.Projectile = Projectile;

        dstManager.AddComponentData(entity,state);
    }
}
