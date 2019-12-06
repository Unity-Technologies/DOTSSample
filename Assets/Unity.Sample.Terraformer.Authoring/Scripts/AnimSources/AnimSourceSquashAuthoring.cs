#if UNITY_EDITOR

using System;
using Unity.Animation;
using Unity.Entities;
using UnityEngine;

public class AnimSourceSquashAuthoring : AnimSourceAuthoring, IConvertGameObjectToEntity
{
    public AnimationClip SquashClip;
    public AnimSourceSquash.Settings settings;

    public void Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
    {
        dstManager.AddComponentData(entity, new AnimSource.Data());

        settings.ClipRef = ClipBuilder.AnimationClipToDenseClip(SquashClip);
        dstManager.AddComponentData(entity, settings);

        dstManager.AddComponentData(entity, AnimSource.AllowWrite.Default);
    }
}

#endif
