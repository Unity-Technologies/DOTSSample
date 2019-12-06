#if UNITY_EDITOR

using System;
using Unity.Entities;
using Unity.Mathematics;

public class AnimSourceIKAuthoring : AnimSourceAuthoring, IConvertGameObjectToEntity
{
    public AnimSourceIK.Settings settings;
    public Unity.Animation.Hybrid.RigComponent RigReference;

    public void Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
    {
        dstManager.AddComponentData(entity, new AnimSource.Data());

        settings.RigAsset = RigDefinitionAsset.ConvertRig(RigReference);
        settings.IkData.TargetOffset = RigidTransform.identity;

        dstManager.AddComponentData(entity, settings);
        dstManager.AddComponentData(entity,AnimSource.AllowWrite.Default);
    }
}

#endif
