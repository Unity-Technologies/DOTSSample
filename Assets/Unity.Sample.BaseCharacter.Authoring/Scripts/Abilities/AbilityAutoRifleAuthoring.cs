using Unity.Collections;
using Unity.Entities;
using UnityEngine;
using Unity.Sample.Core;

public class AbilityAutoRifleAuthoring : MonoBehaviour, IConvertGameObjectToEntity
{
    [ConfigVar(Name = "ability.autorifle", DefaultValue = "0", Description = "show autorifle")]
    public static ConfigVar ShowDebug;


    public AbilityAutoRifle.Settings settings;

    public WeakAssetReference ProjectileAssetGuid;

    public float roundsPerSecond;
    public int clipSize;
    public float reloadDuration;

    public float damage;
    public float damageImpulse;
    public float hitscanRadius;

    public AbilityAutoRifle.COFData COFSettings;

    public void Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
    {
        var settings = AbilityAutoRifle.Settings.Default;

        settings.projectileAssetGuid = ProjectileAssetGuid;
        settings.roundsPerSecond = roundsPerSecond;
        settings.clipSize = clipSize;
        settings.reloadDuration = reloadDuration;
        settings.COFData = COFSettings;
        settings.damage = damage;
        settings.damageImpulse = damageImpulse;
        settings.hitscanRadius = hitscanRadius;
        settings.RandomList = RandomValueList.CreateRandomFloat(32);

        var predictedState = new AbilityAutoRifle.PredictedState
        {
            action = AbilityAutoRifle.Phase.Idle,
            ammoInClip = settings.clipSize,
            COF = settings.COFData.min,
        };

        dstManager.AddComponentData(entity, new Ability.AbilityTag { Value = Ability.AbilityTagValue.AutoRifle });
        dstManager.AddComponentData(entity, settings);
        dstManager.AddComponentData(entity, new AbilityAutoRifle.State());
        dstManager.AddComponentData(entity, predictedState);
        dstManager.AddComponentData(entity, new AbilityAutoRifle.InterpolatedState());
        dstManager.AddComponentData(entity, Ability.AbilityAction.Default );

#if UNITY_EDITOR
        dstManager.SetName(entity,name);
#endif
    }
}




