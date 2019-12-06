#if UNITY_EDITOR

using System;
using Unity.Animation;
using Unity.Entities;
using UnityEngine;

// TODO: (sunek) Can we further store/access the buffers as native arrays?

public class AnimSourceActionsAuthoring : AnimSourceAuthoring, IConvertGameObjectToEntity
{
    public AnimSourceActions.AuthoringSettings authoringSettings;

    public void Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
    {
        dstManager.AddComponentData(entity, new AnimSource.Data());
        var actionDefinitions = dstManager.AddBuffer<AnimSourceActions.ActionDefinitions>(entity);

        // Add Actions to buffer
        for (int i = 0; i < authoringSettings.ActionDef.Length; i++)
        {
            var actionDef = new AnimSourceActions.ActionAnimationDefinition();
            actionDef.action = authoringSettings.ActionDef[i].action;
            actionDef.animation = ClipBuilder.AnimationClipToDenseClip(authoringSettings.ActionDef[i].animation);
            actionDef.restartTimeOffset = authoringSettings.ActionDef[i].restartTimeOffset;

            var e = new AnimSourceActions.ActionDefinitions { Value = actionDef};
            actionDefinitions.Add(e);
        }

        var settings = AnimSourceActions.Settings.Default;
        settings.ReloadBlendOutAimCurve = authoringSettings.reloadBlendOutAimCurve.ToKeyframeCurveBlob();
        settings.BasePoseClip = ClipBuilder.AnimationClipToDenseClip(authoringSettings.ActionAnimationsBasePose);
        dstManager.AddComponentData(entity, settings);

        dstManager.AddBuffer<AnimSourceActions.ActionAnimations>(entity);
        dstManager.AddComponentData(entity, AnimSource.AllowWrite.Default);
    }
}

#endif
