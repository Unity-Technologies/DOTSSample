using System.Collections.Generic;
using Unity.Entities;
using UnityEditor;
using UnityEngine;

#if UNITY_EDITOR

public interface IPrefabAsset
{

}


public class PrefabAssetRegistryAuthoring : MonoBehaviour, IConvertGameObjectToEntity , IDeclareReferencedPrefabs
{
    public List<GameObject> Assets = new List<GameObject>();
    public void Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
    {
        var buffer = dstManager.AddBuffer<PrefabAssetRegistry.Entry>(entity);

        for (int i = 0; i < Assets.Count; i++)
        {
            var assetPath = AssetDatabase.GetAssetPath(Assets[i]);
            var assetGUID = AssetDatabase.AssetPathToGUID(assetPath);
            var reference = new WeakAssetReference(assetGUID);

            var prefabEntity = conversionSystem.GetPrimaryEntity(Assets[i]);

            buffer.Add(new PrefabAssetRegistry.Entry
            {
                Reference = reference,
                EntityPrefab = prefabEntity,
            });
        }

        dstManager.AddComponentData(entity, new PrefabAssetRegistry.State());

    }

    public void DeclareReferencedPrefabs(List<GameObject> referencedPrefabs)
    {
        for (int i = 0; i < Assets.Count; i++)
        {
            referencedPrefabs.Add(Assets[i]);
        }
    }
}

#endif
