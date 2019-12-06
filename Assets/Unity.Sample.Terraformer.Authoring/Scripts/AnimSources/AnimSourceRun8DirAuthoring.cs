#if UNITY_EDITOR

using System;
using Unity.Animation;
using Unity.Entities;
using UnityEngine;
using UnityEditor.Animations;

public class AnimSourceRun8DirAuthoring : AnimSourceAuthoring, IConvertGameObjectToEntity
{
//    public Settings settings;

    public BlendTree RunBlendSpace2D;
    public AnimationClip RunAimClipRef;
    public AnimationClip RunAimHorizontalClipRef;
    public AnimationClip AdditiveRefPose;

    [Range(0f, 90f)] public float MaxHipOffset;
    [Range(0f, 1f)] public float HipDragSpeed;

    public float damping;
    public float maxStep;

    [Range(0f, 1f)]
    public float StateResetWindow;

    [Range(0, 1)]
    public float blendOutAimOnReloadPitch;


    public void Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
    {
        dstManager.AddComponentData(entity, new AnimSource.Data());

        var settings = new AnimSourceRun8Dir.Settings
        {
            RunAimClipRef = ClipBuilder.AnimationClipToDenseClip(RunAimClipRef),
            RunAimHorizontalClipRef = ClipBuilder.AnimationClipToDenseClip(RunAimHorizontalClipRef),
            AdditiveRefPose = ClipBuilder.AnimationClipToDenseClip(AdditiveRefPose),
            MaxHipOffset = MaxHipOffset,
            HipDragSpeed = HipDragSpeed,
            damping = damping,
            maxStep = maxStep,
            StateResetWindow = StateResetWindow,
            blendOutAimOnReloadPitch = blendOutAimOnReloadPitch,
        };

        BlendTreeEntityStoreHelper.AddBlendTree2DComponents(dstManager, entity, RunBlendSpace2D);

        dstManager.AddComponentData(entity, settings);
    }
}

#endif
