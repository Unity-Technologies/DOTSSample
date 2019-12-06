#if UNITY_EDITOR

using System;
using Unity.Animation;
using Unity.Entities;
using UnityEngine;
using UnityEditor.Animations;


public class AnimSourceSprintAuthoring : AnimSourceAuthoring, IConvertGameObjectToEntity
{
    public BlendTree LocoBlendTreeAsset;
    public AnimationClip animAimDownToUp;
    public AnimationClip additiveRefPose;
    [Range(0f, 1f)] public float changeDirSpeed;
    [Tooltip("The max. time between exiting ground move and re-entering before a state reset is triggered")]
    [Range(0f, 1f)] public float stateResetWindow;

    public void Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
    {
        dstManager.AddComponentData(entity, new AnimSource.Data());

        var settings = new AnimSourceSprint.Settings
        {
            animAimDownToUp = ClipBuilder.AnimationClipToDenseClip(animAimDownToUp),
            additiveRefPose = ClipBuilder.AnimationClipToDenseClip(additiveRefPose),
            changeDirSpeed = changeDirSpeed,
            stateResetWindow = stateResetWindow,
        };

        BlendTreeEntityStoreHelper.AddBlendTree1DComponents(dstManager, entity, LocoBlendTreeAsset);

        dstManager.AddComponentData(entity, settings);
    }
}

#endif
