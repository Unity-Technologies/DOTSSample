#if UNITY_EDITOR

using System;
using Unity.Animation;
using Unity.Entities;
using UnityEngine;

// TODO: Go over naming convetions

public class AnimSourceInAirAuthoring : AnimSourceAuthoring, IConvertGameObjectToEntity
{
    public AnimationClip animInAir;
    public AnimationClip animLandAntic;
    public AnimationClip animAimDownToUp;
    public AnimationClip AdditiveRefPose;

    public float landAnticStartHeight;
    public float blendDuration;
    [Range(0, 1)] public float aimDuringReloadPitch;

    public void Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
    {
        dstManager.AddComponentData(entity, new AnimSource.Data());

        var settings = new AnimSourceInAir.Settings
        {
            animInAir = ClipBuilder.AnimationClipToDenseClip(animInAir),
            animLandAntic = ClipBuilder.AnimationClipToDenseClip(animLandAntic),
            animAimDownToUp = ClipBuilder.AnimationClipToDenseClip(animAimDownToUp),
            AdditiveRefPose = ClipBuilder.AnimationClipToDenseClip(AdditiveRefPose),
            landAnticStartHeight = landAnticStartHeight,
            blendDuration = blendDuration,
            aimDuringReloadPitch = aimDuringReloadPitch,
        };

        dstManager.AddComponentData(entity, settings);
    }
}

#endif
