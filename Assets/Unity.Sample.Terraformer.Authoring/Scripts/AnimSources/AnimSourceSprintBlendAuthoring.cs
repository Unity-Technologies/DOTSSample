#if UNITY_EDITOR

using System;
using Unity.Entities;
using Unity.Sample.Core;
using UnityEngine;

public class AnimSourceSprintBlendAuthoring : AnimSourceAuthoring, IConvertGameObjectToEntity
{
    public AnimSourceSprintBlend.Settings settings;

    public void Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
    {
        dstManager.AddComponentData(entity, new AnimSource.Data());
        dstManager.AddComponentData(entity, settings);
        dstManager.AddBuffer<AnimSourceSprintBlend.AnimSourceEntities>(entity);

        // TODO: Try to use Native Array version of buffer, once the code works
        // Add child AnimSources
        int numChildren = 0;
        for (var i = 0; i < transform.childCount; i++)
        {
            var child = transform.GetChild(i);

            if (child.GetComponent<AnimSourceAuthoring>() == null)
                continue;

            var entryEntity = conversionSystem.GetPrimaryEntity(child.gameObject);

            var childAnimSourceEntities = dstManager.GetBuffer<AnimSourceSprintBlend.AnimSourceEntities>(entity);
            var e = new AnimSourceSprintBlend.AnimSourceEntities { Value = entryEntity };
            childAnimSourceEntities.Add(e);

            numChildren++;
        }

        if (numChildren != 2)
            GameDebug.LogWarning("Sprint Blend supports exactly child AnimSources, seems you are using the wrong number!");

//        GameDebug.Log("CREATING SPRINT BLEND");
    }
}

#endif
