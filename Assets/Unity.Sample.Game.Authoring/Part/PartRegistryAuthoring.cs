using System;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using Unity.Sample.Core;
using UnityEngine;
using Unity.Animation.Hybrid;

#if UNITY_EDITOR
public class PartRegistryAuthoring : MonoBehaviour, IConvertGameObjectToEntity, IBundledAssetProvider
{
    [Serializable]
    public class Part
    {
        public string Name;
    }

    [Serializable]
    public class Category
    {
        public string Name;
        public List<Part> Parts = new List<Part>();
    }

    [Serializable]
    public class LODLevel
    {
        public float EndDist;
    }

    public List<Category> Categories = new List<Category>();

    public List<RigComponent> Rigs = new List<RigComponent>();   

    public List<LODLevel> LODlevels = new List<LODLevel>();


    public void Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
    {
        // Create blob root
        var blobBuilder = new BlobBuilder(Allocator.Temp);
        ref var root = ref blobBuilder.ConstructRoot<PartRegistry.PartRegistryBlob>();

        // Setup entries
        var entries = new List<PartRegistry.PartEntry>();

        var buildType = BuildType.Client;

        try
        {
            buildType = conversionSystem.GetBuildSettingsComponent<NetCodeConversionSettings>().Target ==
                    NetcodeConversionTarget.Server
            ? BuildType.Server
            : BuildType.Client;
        }
        catch(Exception)
        {
            GameDebug.LogWarning("Failed to find build settings");
        }

        GameDebug.Log("Converting PartRegistry:{0} for build:{1}", name, buildType);
        FindEntries((int)buildType,entries);

        var blobEntries = blobBuilder.Allocate(ref root.Entries, entries.Count);
        for(int i = 0; i < entries.Count; i++)
        {
            blobEntries[i] = entries[i];
        }

        // Create category part mapping
        var categoryPartMapping = blobBuilder.Allocate(ref root.CategoryPartMapping, Categories.Count);
        var shiftCount = 0;
        for (int i = 0; i < Categories.Count; i++)
        {
            categoryPartMapping[i].ShiftCount = shiftCount;
            var bitCount = Categories[i].Parts.Count > 0 ? CountNeededBits(Categories[i].Parts.Count + 1) : 0;
            categoryPartMapping[i].BitCount = bitCount;
            shiftCount += categoryPartMapping[i].BitCount;
        }

        // Setup Rigs
        var rigs = blobBuilder.Allocate(ref root.Rigs, Rigs.Count);
        for (int i = 0; i < Rigs.Count; i++)
        {
            var rigDefinition = RigDefinitionAsset.ConvertRig(Rigs[i]);
            rigs[i].skeletonHash = rigDefinition.Value.GetHashCode(); // TODO (mogensh) can we get rig hash without having to convert ?
            rigDefinition.Dispose();
        }


        // Setup LOD levels
        var lodLevels = blobBuilder.Allocate(ref root.LODLevels, LODlevels.Count);
        for (int i = 0; i < LODlevels.Count; i++)
        {
            lodLevels[i].EndDist = LODlevels[i].EndDist;
        }

        var rootRef =  blobBuilder.CreateBlobAssetReference<PartRegistry.PartRegistryBlob>(Allocator.Persistent);

        var partRegData = new PartRegistry.PartRegistryData();
        partRegData.Value = rootRef;

        dstManager.AddComponentData(entity,partRegData);
    }


    public void AddBundledAssets(BuildType buildType, List<WeakAssetReference> assets)
    {
        var entries = new List<PartRegistry.PartEntry>();
        FindEntries((int)buildType, entries);
        foreach (var entry in entries)
        {
            assets.Add(entry.Asset);
        }

    }

    public void FindEntries(int buildTypeFlags, List<PartRegistry.PartEntry> entries)
    {
        var assetEntries = GetComponentsInChildren<PartRegistryAssetEntry>();

        // TODO (mogensh) make sure correct decision logic is used
        foreach (var assetEntry in assetEntries)
        {
            // Only get entries for specified build
            if ((buildTypeFlags & assetEntry.BuildTypeFlags) == 0)
                continue;

            var entry = new PartRegistry.PartEntry();
            entry.CategoryId = assetEntry.CategoryIndex;
            entry.PartId = assetEntry.PartIndex + 1;
            entry.Asset = assetEntry.Asset;
            entry.LODFlags = LODlevels.Count > 1 ? assetEntry.LODFlags : 0xFFFF;
            entry.RigFlags = Rigs.Count > 0 ? assetEntry.RigFlags : 0xFFFF;

            entries.Add(entry);
        }
    }

    public void UnpackPartsList(uint packedPartIds, int[] partIds)
    {
        GameDebug.Assert(partIds.Length == Categories.Count,
            "part list needs to be same size as category list. PartIds:{0}. Categories:{1}", partIds.Length, Categories.Count);

        uint baseMask = 0xffffffff;
        var shiftCount = 0;
        for (int i = 0; i < Categories.Count; i++)
        {
            var bitCount = Categories[i].Parts.Count > 0 ? CountNeededBits(Categories[i].Parts.Count + 1) : 0;
            if (bitCount == 0)
            {
                partIds[i] = 0;
                continue;
            }


            var partId = packedPartIds >>  shiftCount;
            var mask = baseMask >> 32 - bitCount;
            partIds[i] = (int)(partId & mask);
            shiftCount += bitCount;
        }
    }

    public uint PackPartsList(int[] partIds)
    {
        GameDebug.Assert(partIds.Length >= Categories.Count,
            "GetParts requested with array of wrong size. CategoryPartMapping:{0} parts:{1}", Categories.Count, partIds.Length);

        uint result = 0;
        var shiftCount = 0;
        for (int i = 0; i < Categories.Count; i++)
        {
            var bitCount = Categories[i].Parts.Count > 0 ? CountNeededBits(Categories[i].Parts.Count + 1) : 0;

//            GameDebug.Assert(CountNeededBits(partIds[i]) <= bitCount, "part id:{0} to big for bitcount. MaxBitcount:{1}. Current:{2}",
//                partIds[i], bitCount,CountNeededBits(partIds[i]));

            var val = partIds[i] << shiftCount;

            result = result | (uint)val;
            shiftCount += bitCount;
        }

        return result;
    }


    int CountNeededBits(int i)
    {
        var count = 1;
        while ( (i >>= 1) != 0)
        {
            count++;
        }

        return count;
    }

}
#endif


