#if UNITY_EDITOR

using System;
using Unity.Animation;
using Unity.Entities;
using UnityEngine;

public class AnimSourceJumpAuthoring : AnimSourceAuthoring, IConvertGameObjectToEntity
{

    public AnimationClip JumpClip;
    public AnimationClip JumpAimVerticalClip;
    public AnimationClip JumpAimHorizontalClip;
    public AnimationClip AdditiveRefPose;
    [Range(0f, 90f)] public float MaxHipOffset;
    [Range(0f, 1f)] public float HipDragSpeed;
    [Tooltip("Jump height in animation. NOT actual ingame jump height")]
    [Range(0.1f, 5f)] public float jumpHeight; // Jump height of character in last frame of animation
    [Range(0, 1)] public float aimDuringReloadPitch;

    public void Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
    {
        var settings = new AnimSourceJump.Settings
        {
            JumpClip = ClipBuilder.AnimationClipToDenseClip(JumpClip),
            JumpAimVerticalClip = ClipBuilder.AnimationClipToDenseClip(JumpAimVerticalClip),
            JumpAimHorizontalClip = ClipBuilder.AnimationClipToDenseClip(JumpAimHorizontalClip),
            AdditiveRefPose = ClipBuilder.AnimationClipToDenseClip(AdditiveRefPose),
            MaxHipOffset = MaxHipOffset,
            HipDragSpeed = HipDragSpeed,
            jumpHeight = jumpHeight,
            aimDuringReloadPitch = aimDuringReloadPitch,
        };

        dstManager.AddComponentData(entity, new AnimSource.Data());
        dstManager.AddComponentData(entity, settings);
    }
}

#endif
