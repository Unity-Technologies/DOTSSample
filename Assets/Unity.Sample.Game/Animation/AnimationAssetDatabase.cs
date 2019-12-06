using System;
using System.Collections.Generic;
using System.IO;
using Unity.Animation;
using Unity.Animation.Hybrid;
using Unity.Collections;
using Unity.Entities;
using Unity.Sample.Core;
using UnityEngine;
using UnityEngine.Profiling;

public class AnimationAssetDatabase
{
#pragma warning disable 649
    [ConfigVar(Name = "animation.rigmap.debug", DefaultValue = "0", Description = "Show rigmap debug info")]
    static ConfigVar DebugRigmap;
#pragma warning restore 649

    public struct RigMap
    {
        public int FromSkeletonHash;
        public int ToSkeletonHash;
        public BlobArray<int> BoneMap;
    }

    static Dictionary<int, BlobAssetReference<RigMap>> rigMapDict = new Dictionary<int, BlobAssetReference<RigMap>>();

    public static void GetOrCreateRigMapping(World world, BlobAssetReference<RigDefinition> fromRig, BlobAssetReference<RigDefinition> toRig, out BlobAssetReference<RigMap> blobRef)
    {
        var hash = (fromRig.Value.GetHashCode() * 397) ^ toRig.Value.GetHashCode();
        if (rigMapDict.TryGetValue(hash, out blobRef))
            return;

        Profiler.BeginSample("CreateRigMap");

        var fromBoneCount = fromRig.Value.Skeleton.BoneCount;

        var blobBuilder = new BlobBuilder(Allocator.Temp);
        ref var root = ref blobBuilder.ConstructRoot<RigMap>();

        root.FromSkeletonHash = fromRig.Value.GetHashCode();
        root.ToSkeletonHash = toRig.Value.GetHashCode();

        GameDebug.Log(world, DebugRigmap, "Creating rig map. Hash:{0}->{1}",root.FromSkeletonHash,root.ToSkeletonHash);

        var boneMap = blobBuilder.Allocate(ref root.BoneMap, fromBoneCount);
        for (int i = 0; i < fromBoneCount; i++)
        {
            boneMap[i] = FindIndex(toRig, fromRig.Value.Skeleton.Ids[i]);
        }

        blobRef =  blobBuilder.CreateBlobAssetReference<RigMap>(Allocator.Persistent);
        rigMapDict.Add(hash,blobRef);

        Profiler.EndSample();
    }

    static int FindIndex(BlobAssetReference<RigDefinition> rig, StringHash boneId)
    {
        for (int i = 0; i < rig.Value.Skeleton.Ids.Length; i++)
        {
            if (rig.Value.Skeleton.Ids[i].Equals(boneId))
                return i;
        }
        return -1;
    }

}
