using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif



public class HeroTypeRegistry : MonoBehaviour, IConvertGameObjectToEntity
{
    public List<HeroTypeAsset> entries = new List<HeroTypeAsset>();

    public void Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
    {
        var blobBuilder = new BlobBuilder(Allocator.Temp);
        ref var root = ref blobBuilder.ConstructRoot<HeroRegistry.Registry>();

        var heroEntries = blobBuilder.Allocate(ref root.Heroes, entries.Count);
        for (int nHero = 0; nHero < entries.Count; nHero++)
        {
            var heroType = entries[nHero];
            heroEntries[nHero].characterPrefab = heroType.characterPrefab;
            heroEntries[nHero].health = heroType.health;
            heroEntries[nHero].sprintCameraSettings = heroType.sprintCameraSettings;
            heroEntries[nHero].eyeHeight = heroType.eyeHeight;

            var itemEntries = blobBuilder.Allocate(ref heroEntries[nHero].Items, heroType.items.Length);
            for (int nItem = 0; nItem < heroType.items.Length; nItem++)
            {
                itemEntries[nItem].asset = heroType.items[nItem].asset;
                itemEntries[nItem].slot = heroType.items[nItem].slot;
            }

        }

        var rootRef =  blobBuilder.CreateBlobAssetReference<HeroRegistry.Registry>(Allocator.Persistent);

        var registry = HeroRegistry.RegistryEntity.Default;
        registry.Value = rootRef;
        dstManager.AddComponentData(entity, registry);
    }
}
