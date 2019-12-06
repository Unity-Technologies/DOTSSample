#if UNITY_EDITOR

using System;
using Unity.Animation;
using Unity.Entities;
using UnityEngine;


public class AnimSourceKnockBackAuthoring : AnimSourceAuthoring, IConvertGameObjectToEntity
{
    public AnimationClip animShootPose;
    public AnimationClip animReferenceShootPose;
    [Range(0, 2)] public float shootPoseMagnitude;
    [Range(0f, 10f)] public float shootPoseEnterSpeed;
    [Range(0f, 10f)] public float shootPoseExitSpeed;
    [Range(0f, 1f)] public float positionMultiplier;
    [Range(0f, 1f)] public float angleMultiplier;
    public AnimationCurve shootPoseEnter;
    public AnimationCurve shootPoseExit;
    public Unity.Animation.Hybrid.RigComponent RigReference;
    public AnimSourceKnockBack.BoneReferences boneReferences;

    public void Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
    {
        dstManager.AddComponentData(entity, new AnimSource.Data());

        var settings = new AnimSourceKnockBack.Settings
        {
            animShootPose = ClipBuilder.AnimationClipToDenseClip(animShootPose),
            animReferenceShootPose = ClipBuilder.AnimationClipToDenseClip(animReferenceShootPose),
            shootPoseMagnitude = shootPoseMagnitude,
            shootPoseEnterSpeed = shootPoseEnterSpeed,
            shootPoseExitSpeed = shootPoseExitSpeed,
            positionMultiplier = positionMultiplier,
            angleMultiplier = angleMultiplier,
            shootPoseEnter = shootPoseEnter.ToKeyframeCurveBlob(),
            shootPoseExit = shootPoseExit.ToKeyframeCurveBlob(),
            rigReference = RigDefinitionAsset.ConvertRig(RigReference),
            boneReferences = new AnimSourceKnockBack.BoneReferences
            {
                hipBoneIndex = boneReferences.hipBoneIndex
            },
        };

        dstManager.AddComponentData(entity, settings);
        dstManager.AddComponentData(entity, AnimSource.AllowWrite.Default);
    }
}

#endif
