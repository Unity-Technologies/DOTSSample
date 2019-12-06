#if UNITY_EDITOR

using System;
using Unity.Animation;
using Unity.Entities;
using UnityEngine;

public class AnimSourceDeathAuthoring : MonoBehaviour, IConvertGameObjectToEntity
{
    public AnimationClip Clip;

    public void Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
    {
        dstManager.AddComponentData(entity, new AnimSource.Data());
        var settings = new AnimSourceDeath.Settings { Clip = ClipBuilder.AnimationClipToDenseClip(Clip) };
        dstManager.AddComponentData(entity, settings);
    }
}

#endif
