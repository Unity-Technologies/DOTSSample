#if UNITY_EDITOR

using System;
using Unity.Animation;
using Unity.Entities;
using UnityEngine;

// TODO:
//
// Convert to jobcomponetsystem + IJobForEach + deal with warning and errors +Check in burst inspector that everything is bursted

// Settings live
// Example

public class AnimSourceStandIKAuthoring : AnimSourceAuthoring, IConvertGameObjectToEntity
{
    public float animTurnAngle; // Total turn in turn anim
    public StandIkNode.Settings FootIk;
    public Unity.Animation.Hybrid.RigComponent RigReferences;
    public AnimSourceStandIK.BoneReferences boneReferences;

    public void Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
    {
        dstManager.AddComponentData(entity, new AnimSource.Data());

        var settings = new AnimSourceStandIK.Settings
        {
            animTurnAngle = animTurnAngle,
            FootIk = FootIk,
            boneReferences = boneReferences,
            rigReference = RigDefinitionAsset.ConvertRig(RigReferences),
        };

        dstManager.AddComponentData(entity, settings);

        if (!dstManager.HasComponent<AnimSource.AllowWrite>(entity))
        {
            dstManager.AddComponentData(entity, AnimSource.AllowWrite.Default);
        }
    }
}

#endif
