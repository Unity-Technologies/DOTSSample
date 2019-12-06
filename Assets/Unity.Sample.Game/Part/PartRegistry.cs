using System;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using Unity.Sample.Core;



public class PartRegistry
{
    public struct PartRegistryData : IComponentData
    {
        public BlobAssetReference<PartRegistryBlob> Value;
    }
    
    public struct CategoryPartMapping
    {
        public int ShiftCount;
        public int BitCount;
    }

    public struct LODLevel
    {
        public float EndDist;
    }

    public struct Rig
    {
        public int skeletonHash;
    }


    public struct PartEntry
    {
        public WeakAssetReference Asset;

        // Conditions
        public int CategoryId;
        public int PartId;
        public int LODFlags;
        public int RigFlags;
    }


    public struct PartRegistryBlob
    {
        // TODO (mogens) for now we just organize in linear list. This needs to be optimized AND CORRECT DECISIONTREE LOGIC IMPLEMENTED
        public BlobArray<PartEntry> Entries;
        public BlobArray<CategoryPartMapping> CategoryPartMapping;
        public BlobArray<LODLevel> LODLevels;
        public BlobArray<Rig> Rigs;

        public bool FindAsset(int channelId, int partId, int skeletonHash, int lod, ref WeakAssetReference result)
        {

            var rigFlag = 0xFFFF;
            if (skeletonHash != 0)
            {
                rigFlag = 0;
                for (int i = 0; i < Rigs.Length; i++)
                {
                    if (Rigs[i].skeletonHash == skeletonHash)
                    {
                        rigFlag = 1 << i;
                        break;
                    }
                }
            }

            for (int i = 0; i < Entries.Length; i++)
            {
                var entry = Entries[i];
                if (entry.CategoryId != channelId)
                    continue;
                if (entry.PartId != partId)
                    continue;

                if ((entry.RigFlags & rigFlag) == 0)
                    continue;

                var lodFlag = 1 << lod;
                if ((entry.LODFlags & lodFlag) == 0)
                    continue;

                result = entry.Asset;
                return true;
            }
            return false;
        }

        public int GetCategoryCount()
        {
            return CategoryPartMapping.Length;
        }

//        // TODO (mogensh) should this be on PartRegistry editor resource (not blob)?
//        public uint PackPartsList(NativeArray<int> parts)
//        {
//            GameDebug.Assert(parts.Length == CategoryPartMapping.Length,
//                "GetParts requested with array of wrong size. CategoryPartMapping:{0} parts:{1}", CategoryPartMapping.Length, parts.Length);
//
//            uint result = 0;
//            for (int i = 0; i < CategoryPartMapping.Length; i++)
//            {
//                if (parts[i] == 0)
//                    continue;
//
//                var mapping = CategoryPartMapping[i];
//                GameDebug.Assert(math.countbits(parts[i]) <= mapping.BitCount, "part id:{0} to big for bitcount. MaxBitcount:{1}. Current:{2}",
//                    parts[i], mapping.BitCount,math.countbits(parts[i]));
//
//                var val = parts[i] << mapping.ShiftCount;
//
//                result = result | (uint)val;
//            }
//
//            return result;
//        }


        // TODO (mogensh) remove when we only do conversion in editor (so we can use method in PartRegistryAsset)
        public uint PackPartsList(int[] partIds)
        {
            GameDebug.Assert(partIds.Length >= CategoryPartMapping.Length,
                "GetParts requested with array of wrong size. CategoryPartMapping:{0} parts:{1}", CategoryPartMapping.Length, partIds.Length);

            uint result = 0;
            for (int i = 0; i < CategoryPartMapping.Length; i++)
            {
                var val = partIds[i] << CategoryPartMapping[i].ShiftCount;
                result = result | (uint)val;
            }

            return result;
        }

        // TODO (mogensh) should this be on PartRegistry editor resource (not blob)?
        public void UnpackPartsList(uint parts, NativeArray<int> partIds)
        {
            GameDebug.Assert(partIds.Length == CategoryPartMapping.Length,
                "part list needs to be same size as category list. PartIds:{0}. Categories:{1}", partIds.Length, CategoryPartMapping.Length);

            uint baseMask = 0xffffffff;
            for (int i = 0; i < CategoryPartMapping.Length; i++)
            {
                var mapping = CategoryPartMapping[i];
                if(mapping.BitCount == 0)
                    continue;

                var partId = parts >>  mapping.ShiftCount;
                var mask = baseMask >> 32 - mapping.BitCount;
                partIds[i] = (int)(partId & mask);
            }
        }

//        public void FindParts(uint parts, ref WeakAssetReference rig, int lod, NativeArray<WeakAssetReference> assets)
//        {
//            GameDebug.Assert(assets.Length == CategoryPartMapping.Length,
//                "Part requested with array of wrong size. CategoryPartMapping:{0} assets:{1}", CategoryPartMapping.Length, assets.Length);
//
//            uint baseMask = 0xffffffff;
//            for (int i = 0; i < CategoryPartMapping.Length; i++)
//            {
//                var mapping = CategoryPartMapping[i];
//                if(mapping.BitCount == 0)
//                    continue;
//
//                var partId = parts >>  mapping.ShiftCount;
//                var mask = baseMask >> 32 - mapping.BitCount;
//                partId = partId & mask;
//
//                if (partId == 0)
//                {
//                    assets[i] = WeakAssetReference.Default;
//                    continue;
//                }
//
//                var asset = new WeakAssetReference();
//                var found = FindAsset(i, (int)partId, ref rig, lod, ref asset);
//                if (found)
//                    assets[i] = asset;
//            }
//
//
//        }
    }

//    // TODO (mogensh) Find place to store part registries
//    private static Dictionary<WeakAssetReference, BlobAssetReference<PartRegistryBlob>> g_PartRegistries
//        = new Dictionary<WeakAssetReference, BlobAssetReference<PartRegistryBlob>>();
//    public static BlobAssetReference<PartRegistryBlob> GetPartRegistry(WeakAssetReference asset)
//    {
//        var registry = BlobAssetReference<PartRegistryBlob>.Null;
//        if (g_PartRegistries.TryGetValue(asset, out registry))
//            return registry;
//
//        var path = GetBlobAssetPath(asset.ToGuidStr());
//        GameDebug.Assert(File.Exists(path), "Can find file:" + path);
//        registry = BlobAssetReference<PartRegistryBlob>.Create(File.ReadAllBytes(path));
//        GameDebug.Log("Loaded registry");
//        g_PartRegistries.Add(asset,registry);
//        return registry;
//    }
//    static string GetBlobAssetPath(string guid)
//    {
//        return Application.streamingAssetsPath + "/BlobAssets/" + guid + ".blob";
//    }


    // TODO (mogensh) Find place to store part registries
    private static Dictionary<Tuple<World,WeakAssetReference>, BlobAssetReference<PartRegistry.PartRegistryBlob>> g_PartRegistries
        = new Dictionary<Tuple<World,WeakAssetReference>, BlobAssetReference<PartRegistry.PartRegistryBlob>>();

    public static BlobAssetReference<PartRegistry.PartRegistryBlob> GetPartRegistry(World world, WeakAssetReference asset)
    {
        var tuple = new Tuple<World, WeakAssetReference>(world, asset);
        var registry = BlobAssetReference<PartRegistry.PartRegistryBlob>.Null;
        if (g_PartRegistries.TryGetValue(tuple, out registry))
            return registry;

        var entity = PrefabAssetManager.CreateEntity(world.EntityManager, asset);
        var partRegistryData = world.EntityManager.GetComponentData<PartRegistryData>(entity);

        registry = partRegistryData.Value;
//        GameDebug.Log("Loaded registry");
        g_PartRegistries.Add(tuple,registry);
        return registry;
    }
}

