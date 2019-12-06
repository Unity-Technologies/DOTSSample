using Unity.Animation;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor.Animations;
#endif

// We currently do not support blobassets referencing other blobassets. This helper class can be used to store blendspace data on entity
// and create blendspace blobasset at entity initialization
public class BlendTreeEntityStoreHelper
{
    public struct BlendTree1DData : IComponentData
    {
        public StringHash BlendParameter;
    }

    public struct BlendTree1DMotionData : IBufferElementData
    {
        public float MotionThreshold;
        public float MotionSpeed;
        public BlobAssetReference<Clip> Motion;
        public MotionType MotionType;
    }

    public struct BlendTree2DData : IComponentData
    {
        public StringHash BlendParam;
        public StringHash BlendParamY;
    }

    public struct BlendTree2DMotionData : IBufferElementData
    {
        public float2                              MotionPosition;
        public float                               MotionSpeed;
        public BlobAssetReference<Clip>            Motion;
        public MotionType                          MotionType;
    }



#if UNITY_EDITOR
    public static void AddBlendTree1DComponents(EntityManager entityManager, Entity entity, BlendTree blendTree)
    {
        entityManager.AddComponentData(entity, new BlendTree1DData
        {
            BlendParameter = new StringHash(blendTree.blendParameter)
        });

        var buffer = entityManager.AddBuffer<BlendTree1DMotionData>(entity);
        for(int i=0;i<blendTree.children.Length;i++)
        {
            buffer.Add(new BlendTree1DMotionData
            {
                MotionThreshold = blendTree.children[i].threshold,
                MotionSpeed = blendTree.children[i].timeScale,
                Motion = UnityEditor.Animations.BlendTreeConvertHelper.Convert(blendTree.children[i].motion),
                MotionType = UnityEditor.Animations.BlendTreeConvertHelper.GetMotionType(blendTree.children[i].motion),
            });
        }
    }

    public static void AddBlendTree2DComponents(EntityManager entityManager, Entity entity, BlendTree blendTree)
    {
        var blendSpaceData = new BlendTree2DData
        {
            BlendParam = blendTree.blendParameter,
            BlendParamY = blendTree.blendParameterY,
        };
        entityManager.AddComponentData(entity, blendSpaceData);


        var blendSpaceEntries =  entityManager.AddBuffer<BlendTree2DMotionData>(entity);
        for (int i = 0; i < blendTree.children.Length; i++)
        {
            blendSpaceEntries.Add(new BlendTree2DMotionData
            {
                MotionPosition = blendTree.children[i].position,
                MotionSpeed = blendTree.children[i].timeScale,
                Motion = UnityEditor.Animations.BlendTreeConvertHelper.Convert(blendTree.children[i].motion),
                MotionType = UnityEditor.Animations.BlendTreeConvertHelper.GetMotionType(blendTree.children[i].motion),
            });
        }
    }
#endif

    public static BlobAssetReference<BlendTree1D> CreateBlendTree1DFromComponents(EntityManager entityManager, Entity entity)
    {

        var data = entityManager.GetComponentData<BlendTree1DData>(entity);
        var motionData =  entityManager.GetBuffer<BlendTree1DMotionData>(entity);
        var targetMotionData = new Unity.Animation.BlendTree1DMotionData[motionData.Length];
        for(int i=0;i<motionData.Length;i++)
        {
            targetMotionData[i].MotionThreshold = motionData[i].MotionThreshold;
            targetMotionData[i].MotionSpeed = motionData[i].MotionSpeed;
            targetMotionData[i].Motion.Clip = motionData[i].Motion;
            targetMotionData[i].MotionType = motionData[i].MotionType;
        }

        return BlendTreeBuilder.CreateBlendTree(targetMotionData, data.BlendParameter );
    }

    public static BlobAssetReference<BlendTree2DSimpleDirectionnal> CreateBlendTree2DFromComponents(EntityManager entityManager,
        Entity entity)
    {
        // Create blendspace
        var blendSpaceData = entityManager.GetComponentData<BlendTree2DData>(entity);
        var blendSpaceEntries =  entityManager.GetBuffer<BlendTree2DMotionData>(entity);
        var blendTree2DMotionData = new Unity.Animation.BlendTree2DMotionData[blendSpaceEntries.Length];
        for(int i=0;i<blendSpaceEntries.Length;i++)
        {
            blendTree2DMotionData[i].MotionPosition = blendSpaceEntries[i].MotionPosition;
            blendTree2DMotionData[i].MotionSpeed = blendSpaceEntries[i].MotionSpeed;
            blendTree2DMotionData[i].Motion.Clip = blendSpaceEntries[i].Motion;
            blendTree2DMotionData[i].MotionType = blendSpaceEntries[i].MotionType;
        }
        return BlendTreeBuilder.CreateBlendTree2DSimpleDirectionnal(blendTree2DMotionData, blendSpaceData.BlendParam, blendSpaceData.BlendParamY );
    }
}


#if UNITY_EDITOR
namespace UnityEditor.Animations
{
    public class BlendTreeConvertHelper
    {
        public static BlobAssetReference<Clip> Convert(UnityEngine.Motion motion)
        {
            var animationClip = motion as AnimationClip;
//            var blendTree = motion as BlendTree;
//
//            if (blendTree != null)
//                return Convert(blendTree);
//            else if( animationClip != null)
            if( animationClip != null)
            {
                var clip = ClipBuilder.AnimationClipToDenseClip(animationClip);
                return clip;
            }
            else
                throw new System.ArgumentException($"Selected Motion type is not supported.");
        }

        public static MotionType GetMotionType(UnityEngine.Motion motion)
        {
            var blendTree = motion as BlendTree;

            if (blendTree != null)
            {
                return blendTree.blendType == BlendTreeType.Simple1D ? MotionType.BlendTree1D : MotionType.BlendTree2DSimpleDirectionnal;
            }
            else
                return MotionType.Clip;
        }
    }
}
#endif
