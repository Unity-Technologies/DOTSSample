#if UNITY_EDITOR

using Unity.Animation;
using Unity.Entities;
using UnityEngine;

public class AnimSourceSingleClipAuthoring : AnimSourceAuthoring, IConvertGameObjectToEntity
{
    public AnimationClip Clip;

    public void Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
    {
        dstManager.AddComponentData(entity, new AnimSource.Data());

        var settings = new AnimSourceSingleClip.Settings
        {
            Clip = ClipBuilder.AnimationClipToDenseClip(Clip),
        };

        dstManager.AddComponentData(entity, settings);
    }
}

#endif
