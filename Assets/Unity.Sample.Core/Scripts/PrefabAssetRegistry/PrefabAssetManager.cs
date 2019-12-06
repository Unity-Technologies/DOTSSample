using System;
using System.Collections.Generic;
using Unity.Entities;
using Unity.Sample.Core;
using UnityEngine;
using Object = UnityEngine.Object;


public class PrefabAssetManager
{
    [ConfigVar(Name = "prefab.show.lifetime", DefaultValue = "0", Description = "Show prefab lifetime data")]
    public static ConfigVar ShowLifetime;


    public static void Shutdown()
    {
        m_EntityPrefabs.Clear();
    }

    public static Entity CreateEntity(EntityManager entityManager, WeakAssetReference assetGuid)
    {
        var entityPrefab = PrefabAssetRegistry.FindEntityPrefab(entityManager,assetGuid);
        if (entityPrefab == Entity.Null)
        {
            GameDebug.LogError("Failed to create prefab for asset:" + assetGuid.ToGuidStr());
            return Entity.Null;
        }

        var e = entityManager.Instantiate(entityPrefab);

        GameDebug.Log(entityManager.World,ShowLifetime,"Created entity:{0} from asset:{1}", e, assetGuid.ToGuidStr());

        return e;
    }


    public static Entity CreateEntity(EntityManager entityManager, EntityCommandBuffer cmdBuffer, WeakAssetReference assetGuid)
    {
        var entityPrefab = PrefabAssetRegistry.FindEntityPrefab(entityManager,assetGuid);
        if (entityPrefab == Entity.Null)
        {
            GameDebug.LogError("Failed to create prefab for asset:" + assetGuid.ToGuidStr());
            return Entity.Null;
        }
        var instance = cmdBuffer.Instantiate(entityPrefab);
        return instance;
    }

    public static Entity CreateEntity(World world, GameObject prefab)
    {
//        GameDebug.Log("CreateEntity prefab:" + prefab.name);

        // If gameObject has GameObjectEntity it is already registered in entitymanager. If not we register it here


#pragma warning disable 618
        // we're keeping World.Active until we can properly remove them all
        var defaultWorld = World.Active;
        try
        {
            World.Active = world;
#pragma warning restore 618
            if (prefab.GetComponent<GameObjectEntity>() != null)
            {
                var go = Object.Instantiate(prefab);
                var entity = go.GetComponent<GameObjectEntity>().Entity;

#if UNITY_EDITOR
                world.EntityManager.SetName(entity, "Entity " + entity.Index + " GameObject:" + go.name);
#endif
                return entity;
            }

            var entityPrefab = GetOrCreateEntityPrefab(world, prefab);
            var instance = world.EntityManager.Instantiate(entityPrefab);

#if UNITY_EDITOR
            world.EntityManager.SetName(instance, "Entity " + instance.Index + " Inst:" + prefab.name);
#endif

            return instance;
        }
        finally
        {
#pragma warning disable 618
            // we're keeping World.Active until we can properly remove them all
            World.Active = defaultWorld;
#pragma warning restore 618
        }
    }

    public static void DestroyEntity(EntityCommandBuffer commandBuffer, Entity entity)
    {
        commandBuffer.DestroyEntity(entity);
    }


    public static void DestroyEntity(EntityManager entityManager, Entity entity)
    {
        if (entityManager.HasComponent<Transform>(entity))
        {
            var transform = entityManager.GetComponentObject<Transform>(entity);
            Object.Destroy(transform.gameObject);

            // GameObjectEntity will take care of destorying entities
            if (transform.GetComponent<GameObjectEntity>() != null)
                return;
        }

        if (entityManager.HasComponent<RectTransform>(entity))
        {
            var transform = entityManager.GetComponentObject<RectTransform>(entity);
            Object.Destroy(transform.gameObject);

            // GameObjectEntity will take care of destorying entities
            if (transform.GetComponent<GameObjectEntity>() != null)
                return;
        }

        GameDebug.Log(entityManager.World,ShowLifetime,"Destroying entity:{0} ", entity);

        entityManager.DestroyEntity(entity);

    }


    static Entity GetOrCreateEntityPrefab(World world, GameObject prefab)
    {
        Entity entityPrefab;
        var tuple = new Tuple<GameObject, World>(prefab, world);
        if (!m_EntityPrefabs.TryGetValue(tuple, out entityPrefab))
        {
#pragma warning disable 618
            // we're keeping World.Active until we can properly remove them all
            var defaultWorld = World.Active;
            try
            {
                World.Active = world;
                entityPrefab = GameObjectConversionUtility.ConvertGameObjectHierarchy(prefab, world);
                m_EntityPrefabs.Add(tuple,entityPrefab);

#if UNITY_EDITOR
                world.EntityManager.SetName(entityPrefab, "Entity " + entityPrefab.Index + " Prefab:" + prefab.name);
#endif
            }
            finally
            {
                World.Active = defaultWorld;
            }
#pragma warning restore 618
        }

        return entityPrefab;
    }

    static Dictionary<Tuple<GameObject,World>,Entity> m_EntityPrefabs = new Dictionary<Tuple<GameObject,World>, Entity>(64);
}
