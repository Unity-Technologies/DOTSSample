using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using UnityEngine;

[Serializable]
public struct GlobalAssetRegistry : IComponentData
{
    public WeakAssetReference gameModePrefab;
}

public class GlobalAssetRegistryAuthoring : MonoBehaviour, IConvertGameObjectToEntity
{
    public GlobalAssetRegistry data;

    public void Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
    {
        dstManager.AddComponentData(entity, data);
    }
}

