using System;
using System.Collections.Generic;
using System.IO;
using System.Xml;
using Unity.Animation;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Sample.Core;
using Unity.Transforms;
using UnityEngine;
using UnityEngine.PlayerLoop;

#if UNITY_EDITOR
using UnityEditor;
#endif


#if UNITY_EDITOR
public class PartOwnerAuthoring : MonoBehaviour, IConvertGameObjectToEntity, IBundledAssetProvider
{
    public GameObject PartRegistryAsset;        // TODO (mogensh) use PartRegistry reference
    public List<int> PartIds = new List<int>();

    public void AddBundledAssets(BuildType buildType, List<WeakAssetReference> assets)
    {
        if (PartRegistryAsset != null)
        {
            var path = AssetDatabase.GetAssetPath(PartRegistryAsset.gameObject);
            if (path != null)
            {
                var guid = AssetDatabase.AssetPathToGUID(path);
                assets.Add(new WeakAssetReference(guid));
            }
        }
    }

    public void Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
    {
        var registry = PartRegistryAsset.GetComponent<PartRegistryAuthoring>();

        while (PartIds.Count < registry.Categories.Count)
            PartIds.Add(0);
        var packedPartId = registry.PackPartsList(PartIds.ToArray());

        var inputState = PartOwner.InputState.Default;
        inputState.PackedPartIds = packedPartId;
        dstManager.AddComponentData(entity, inputState);

        var registryPath = AssetDatabase.GetAssetPath(PartRegistryAsset);
        var registryGuid = AssetDatabase.AssetPathToGUID(registryPath);
        var registryRef = new WeakAssetReference(registryGuid);
        dstManager.AddComponentData(entity, new PartOwner.RegistryAsset(registryRef));
    }
}
#endif
