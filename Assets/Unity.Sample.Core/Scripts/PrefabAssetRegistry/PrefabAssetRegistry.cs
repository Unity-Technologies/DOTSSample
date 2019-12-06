
using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using Unity.Sample.Core;

public class PrefabAssetRegistry
{
    public struct State : IComponentData
    {
        public int foo;
    }

    public struct Entry : IBufferElementData
    {
        public WeakAssetReference Reference;
        public Entity EntityPrefab;
    }






    static Entity GetRegistryEntity(EntityManager entityManager)
    {
        var query = entityManager.CreateEntityQuery(typeof(PrefabAssetRegistry.Entry));
        var entityArray = query.ToEntityArray(Allocator.TempJob);
        if (entityArray.Length == 0)
        {
            GameDebug.LogError("Failed to find PrefabAssetRegistry. Have you included the PrefabAssetRegistry subscene ?");
        }
        if (entityArray.Length > 1)
        {
            GameDebug.LogWarning("Found " + entityArray.Length  + " PrefabAssetRegistries. First one will be used");
        }

        var entity = entityArray.Length > 0 ? entityArray[0] : Entity.Null;
        entityArray.Dispose();
        query.Dispose();
        return entity;
    }


    public static void GetAllResources(EntityManager entityManager, List<WeakAssetReference> resources)
    {
        var entity = GetRegistryEntity(entityManager);
        var entries = entityManager.GetBuffer<PrefabAssetRegistry.Entry>(entity);
        for (int i = 0; i < entries.Length;i++)
        {
            resources.Add(entries[i].Reference);
        }
    }

    public static Entity FindEntityPrefab(EntityManager entityManager, WeakAssetReference assetGuid)
    {
        var entity = GetRegistryEntity(entityManager);
        if(entity == Entity.Null)
            return Entity.Null;


        var entries = entityManager.GetBuffer<PrefabAssetRegistry.Entry>(entity);
        for (int i = 0; i < entries.Length;i++)
        {
            if (entries[i].Reference.Equals(assetGuid))
                return entries[i].EntityPrefab;
        }

        return Entity.Null;
    }



}
