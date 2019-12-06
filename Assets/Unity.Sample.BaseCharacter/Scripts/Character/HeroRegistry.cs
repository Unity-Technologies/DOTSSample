
using Unity.Collections;
using Unity.Entities;
using Unity.Sample.Core;

public class HeroRegistry
{
    public struct Registry
    {
        public struct ItemEntry
        {
            public WeakAssetReference asset;
            public byte slot;
        }

        public struct HeroEntry
        {
            public BlobArray<ItemEntry> Items;
            public WeakAssetReference characterPrefab;
            public float health;
            public HeroTypeAsset.SprintCameraSettings sprintCameraSettings;
            public float eyeHeight;
        }

        public BlobArray<HeroEntry> Heroes;
    }

    public struct RegistryEntity : IComponentData
    {
        public static RegistryEntity Default => new RegistryEntity();
        public BlobAssetReference<Registry> Value;
    }


    public static BlobAssetReference<Registry> GetRegistry(EntityManager entityManager)
    {
        var query = entityManager.CreateEntityQuery(typeof(RegistryEntity));

        var registryEntityArray = query.ToComponentDataArray<RegistryEntity>(Allocator.TempJob);
        if (registryEntityArray.Length == 0)
        {
            GameDebug.LogError("Failed to find entity HeroRegistry.RegistryEntity. Is it included in a scene ?");
        }
        if (registryEntityArray.Length > 1)
        {
            GameDebug.LogWarning("Found " + registryEntityArray.Length  + " HeroRegistry.RegistryEntity entities. First one will be used");
        }

        var result = registryEntityArray.Length > 0 ? registryEntityArray[0].Value : BlobAssetReference<Registry>.Null;

        registryEntityArray.Dispose();
        query.Dispose();

        return result;
    }

}
