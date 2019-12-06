#if UNITY_EDITOR

using System;
using Unity.Entities;
using UnityEngine;


public class AnimSourceBankingAuthoring : AnimSourceAuthoring, IConvertGameObjectToEntity
{
//    [ConfigVar(Name = "char.bankAmount", DefaultValue = "0", Description = "Set the bank amount something something")]
//    public static ConfigVar bankAmount;

    public AnimSourceBanking.Settings settings;
    public Unity.Animation.Hybrid.RigComponent RigReferences;

    public void Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
    {
        dstManager.AddComponentData(entity, new AnimSource.Data());

        settings.rigReference = RigDefinitionAsset.ConvertRig(RigReferences);
        dstManager.AddComponentData(entity, settings);
        dstManager.AddComponentData(entity, AnimSource.AllowWrite.Default);
    }
}

#endif
