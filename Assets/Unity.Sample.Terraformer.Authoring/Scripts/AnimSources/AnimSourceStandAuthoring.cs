#if UNITY_EDITOR

using System;
using Unity.Animation;
using Unity.Entities;
using UnityEngine;


public class AnimSourceStandAuthoring : AnimSourceAuthoring, IConvertGameObjectToEntity
{
    public AnimationClip AnimIdle;
    public AnimationClip AnimTurnL;
    public AnimationClip AnimTurnR;
    public AnimationClip AnimAimLeft;
    public AnimationClip AnimAimMid;
    public AnimationClip AnimAimRight;
    public AnimationClip AdditiveRefPose;

    public float animTurnAngle; // Total turn in turn anim 90.0f
    public float aimTurnLocalThreshold; // 90f Turn threshold
    public float turnSpeed; // 250
    public float turnThreshold; // 100
    public float turnTransitionSpeed; // = 7.5f;

    [Range(0, 1)]
    public float aimDuringReloadPitch;
    [Range(0, 1)]
    public float aimDuringReloadYaw;

    public void Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
    {
        var settings = new AnimSourceStand.Settings
        {
            StandClip = ClipBuilder.AnimationClipToDenseClip(AnimIdle),
            TurnLeftClip = ClipBuilder.AnimationClipToDenseClip(AnimTurnL),
            TurnRightClip = ClipBuilder.AnimationClipToDenseClip(AnimTurnR),
            StandAimLeftClip = ClipBuilder.AnimationClipToDenseClip(AnimAimLeft),
            StandAimMidClip = ClipBuilder.AnimationClipToDenseClip(AnimAimMid),
            StandAimRightClip = ClipBuilder.AnimationClipToDenseClip(AnimAimRight),
            AdditiveRefPoseClip = ClipBuilder.AnimationClipToDenseClip(AdditiveRefPose),

            animTurnAngle = animTurnAngle,
            aimTurnLocalThreshold = aimTurnLocalThreshold,
            turnSpeed = turnSpeed,
            turnThreshold = turnThreshold,
            turnTransitionSpeed = turnTransitionSpeed,
            aimDuringReloadPitch = aimDuringReloadPitch,
            aimDuringReloadYaw = aimDuringReloadYaw,
        };

        dstManager.AddComponentData(entity, new AnimSource.Data());
        dstManager.AddComponentData(entity, settings);
        dstManager.AddBuffer<AnimSourceStand.SimpleTransition.PortWeights>(entity);
    }
}

#endif
