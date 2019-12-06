using Unity.Animation;
using Unity.Animation.Hybrid;
using Unity.Entities;
using Unity.Sample.Core;
using UnityEngine;


#if UNITY_EDITOR
using UnityEditor;
#endif

public class RigDefinitionAsset : MonoBehaviour
{



#if UNITY_EDITOR

    // TODO (mogens) move this to RigConversion in animation package ?
    public static BlobAssetReference<RigDefinition> ConvertRig(RigComponent rigComponent)
    {
        var skeletonNodes = RigGenerator.ExtractSkeletonNodesFromRigComponent(rigComponent);
        var channels = RigGenerator.ExtractAnimationChannelFromRigComponent(rigComponent);
        var rigDefinition = RigBuilder.CreateRigDefinition(skeletonNodes, null, channels);
        return rigDefinition;
    }
    
//    public static BlobAssetReference<RigDefinition> ConvertRig(Unity.Animation.Hybrid.Skeleton skeletonComponent)
//    {
//        SkeletonNode[] skeletonNodes;
//        IAnimationChannel[] animationChannels = new IAnimationChannel[] {
//            new Unity.Animation.FloatChannel { Id = "LeftFootIKWeight", DefaultValue = 0.0f },
//            new Unity.Animation.FloatChannel { Id = "RightFootIKWeight", DefaultValue = 0.0f },
//            new Unity.Animation.FloatChannel { Id = "LeftHandIKWeight", DefaultValue = 0.0f },
//            new Unity.Animation.FloatChannel { Id = "RightHandIKWeight", DefaultValue = 0.0f },
//        };
//
//        skeletonNodes = RigGenerator.ExtractSkeletonNodesFromTransforms(skeletonComponent.transform, skeletonComponent.Bones);
//
//        var rigDefinition = RigBuilder.CreateRigDefinition(skeletonNodes, null, animationChannels);
//
//        GameDebug.Log(string.Format("ConvertRig. Skeleton:{0} hash:{1}",skeletonComponent, skeletonComponent.GetSkeletonHash()));
//
//        return rigDefinition;
//    }

#endif




    public WeakAssetReference SelfReference;    // TODO (mogensh) remove when all conversion is in editor (and we can use AssetDatabase)

#if UNITY_EDITOR
    private void OnValidate()
    {
        var path = AssetDatabase.GetAssetPath(this);
        if (string.IsNullOrEmpty(path))
            return;

        var guid = AssetDatabase.AssetPathToGUID(path);
        SelfReference = new WeakAssetReference(guid);
    }
#endif
}
