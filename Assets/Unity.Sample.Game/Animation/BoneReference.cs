using System;
using Unity.Animation;
using Unity.Entities;

[Serializable]
public struct BoneReference
{
    public static BoneReference Default => new BoneReference() { BoneIndex = -1};
    public Unity.Animation.Hybrid.RigComponent RigAsset;
    public int BoneIndex;
}

public struct RuntimeBoneReference : IEquatable<RuntimeBoneReference>
{
    public static RuntimeBoneReference Default => new RuntimeBoneReference() { BoneIndex = -1};
    public BlobAssetReference<RigDefinition> ReferenceRig;
    public int BoneIndex;

    public bool Equals(RuntimeBoneReference other)
    {
        return other.BoneIndex == BoneIndex &&
               other.ReferenceRig.Value.GetHashCode() == ReferenceRig.Value.GetHashCode();
    }
}

[Serializable]
public class BoneReferenceAuthoring
{
    public RigDefinitionAsset RigAsset;
    public String BoneName;

#if UNITY_EDITOR

//    public BoneReference GetBoneReference()
//    {
//        var skeleton = RigAsset.GetComponent<Skeleton>();
//        GameDebug.Assert(skeleton != null, "Rig definition has no skeleton component. It is required");
//
//        var boneRef = new BoneReference();
//        var assetPath = AssetDatabase.GetAssetPath(RigAsset.gameObject);
//        var assetGUID = AssetDatabase.AssetPathToGUID(assetPath);
//        boneRef.RigAsset = new WeakAssetReference(assetGUID);
//        boneRef.BoneIndex = -1;
//
//        for(int i=0;i<skeleton.Bones.Length;i++)
//        {
//            if(skeleton.Bones[i].name == BoneName)
//            {
//                boneRef.BoneIndex = i;
//                break;
//            }
//        }
//
//        if(boneRef.BoneIndex == -1)
//            GameDebug.LogError("Failed to map bone reference to valid bone");
//
//        return boneRef;
//    }
#endif
}
