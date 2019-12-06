using System;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Sample.Core;
using Unity.Transforms;
using UnityEngine;
using UnityEngine.VFX;

#if UNITY_EDITOR
public partial class TerraformerWeaponPart : MonoBehaviour, IConvertGameObjectToEntity
{
    [AssetType(typeof(SoundDef))]
    public WeakAssetReference soundRef;
    
    public VisualEffectAsset muzzleEffect;
    public VisualEffectAsset hitscanEffect;
    
    public VisualEffectAsset impactEffectEnv;
    public WeakAssetReference impactSoundEnv;

    public VisualEffectAsset impactEffectChar;
    public WeakAssetReference impactSoundChar;

    public Transform MuzzleTransform;
    
    public void Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
    {
        var authoringData = AutoringData.Default;
        authoringData.soundRef = soundRef;
        authoringData.impactSoundEnv = impactSoundEnv;
        authoringData.impactSoundChar = impactSoundChar;
        authoringData.MuzzleEntity = conversionSystem.GetPrimaryEntity(MuzzleTransform);
        
        var authoringClass = new AuthoringClass();
        authoringClass.muzzleEffect = muzzleEffect;
        authoringClass.hitscanEffect = hitscanEffect;
        authoringClass.impactEffectEnvironment = impactEffectEnv;
        authoringClass.impactEffectCharacter = impactEffectChar;

        var state = State.Default;
        
        dstManager.AddComponentData(entity, authoringData);
        dstManager.AddComponentData(entity, state);
        dstManager.AddComponentData(entity, authoringClass);
    }
}
#endif




public partial class TerraformerWeaponPart 
{
    public struct AutoringData : IComponentData
    {
        public static AutoringData Default => new AutoringData();
        public WeakAssetReference soundRef;
        public WeakAssetReference impactSoundEnv;
        public WeakAssetReference impactSoundChar;

        public Entity MuzzleEntity;
    }

    public class AuthoringClass : IComponentData, IEquatable<AuthoringClass>
    {
        public VisualEffectAsset hitscanEffect;
        public VisualEffectAsset muzzleEffect;
        public VisualEffectAsset impactEffectEnvironment;
        public VisualEffectAsset impactEffectCharacter;
        
        public bool Equals(AuthoringClass other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return Equals(muzzleEffect, other.muzzleEffect) && Equals(hitscanEffect, other.hitscanEffect);
        }

        public override int GetHashCode()
        {
            int hash = base.GetHashCode();
            hash = ReferenceEquals(muzzleEffect, null) ? hash :  hash * 23 + muzzleEffect.GetHashCode();
            hash = ReferenceEquals(hitscanEffect, null) ? hash :  hash * 23 + hitscanEffect.GetHashCode();
            hash = ReferenceEquals(impactEffectEnvironment, null) ? hash :  hash * 23 + impactEffectEnvironment.GetHashCode();
            hash = ReferenceEquals(impactEffectCharacter, null) ? hash :  hash * 23 + impactEffectCharacter.GetHashCode();
            return hash;
        }
    }

    
    public struct AbilityEntity : IComponentData
    {
        public Entity Value;
    }

    public struct State : IComponentData
    {
        public static State Default => new State();
        public int LastFireTick;
    }

    [UpdateInGroup(typeof(InitializationSystemGroup))]
    public class Initialize : JobComponentSystem
    {
        protected override JobHandle OnUpdate(JobHandle inputDeps)
        {
            inputDeps.Complete();
            
            var PostUpdateCommands = new EntityCommandBuffer(Allocator.TempJob);
            var OwnedAbilityBufferFromEntity = GetBufferFromEntity<AbilityOwner.OwnedAbility>(true);

            Entities.WithNativeDisableContainerSafetyRestriction(PostUpdateCommands)
                .WithAll<AutoringData>()
                .WithNone<AbilityEntity>()
                .WithoutBurst()
                .ForEach((Entity entity, ref Part.Owner partOwner) =>
            {
                var rootOwner = EntityManager.GetComponentData<Item.InputState>(partOwner.Value).owner;
                if (rootOwner == Entity.Null)
                    return;

                var abilityEntity = Ability.FindAbility(OwnedAbilityBufferFromEntity, rootOwner, AbilityAutoRifle.Tag);
                if (abilityEntity == Entity.Null)
                    return;
                
                var ability = new AbilityEntity
                {
                    Value = abilityEntity
                };
                
                PostUpdateCommands.AddComponent(entity,ability);
            }).Run();

            PostUpdateCommands.Playback(EntityManager);
            PostUpdateCommands.Dispose();

            return default;
        }
    }
    

    [UpdateInGroup(typeof(PresentationSystemGroup))]
    [UpdateBefore(typeof(VFXSystem))]
    [UpdateBefore(typeof(PresentationSystemGroup))]
    [AlwaysSynchronizeSystem]
    public class Update : JobComponentSystem
    {
        protected override JobHandle OnUpdate(JobHandle inputDeps)
        {
            var vfxSystem = World.GetExistingSystem<VFXSystem>();
            
            // TODO: Burst not compatible with accessing EntityManager
            Entities.WithoutBurst()
                .ForEach((AuthoringClass authClass, ref AutoringData authData, ref AbilityEntity ability, ref State state, ref Unity.Transforms.LocalToWorld localToWorld) =>
            {
                if (!EntityManager.Exists(ability.Value))
                {
                    GameDebug.LogWarning(World,"Ability entity:{0}" + ability.Value + " does no longer exist");
                    return;
                }
                
                if (!EntityManager.HasComponent<AbilityAutoRifle.InterpolatedState>(ability.Value))
                {
                    GameDebug.LogWarning(World,"Ability entity:{0}" + ability.Value + " does not have Ability_AutoRifle.InterpolatedState component");
                    return;
                }
                    
                
                var rifleState = EntityManager.GetComponentData<AbilityAutoRifle.InterpolatedState>(ability.Value);


                if (rifleState.fireTick > state.LastFireTick)    // This will trigger for late joiners 
                {
                    var muzzleLTW = EntityManager.GetComponentData<LocalToWorld>(authData.MuzzleEntity);                
//                    DebugDraw.Sphere(muzzleLTW.Position,0.2f,Color.red);
                    
                    vfxSystem.SpawnPointEffect(authClass.muzzleEffect, muzzleLTW.Position, muzzleLTW.Forward);
                    vfxSystem.SpawnLineEffect(authClass.hitscanEffect,muzzleLTW.Position, rifleState.fireEndPos);

                    if (rifleState.impactType != AbilityAutoRifle.ImpactType.None)
                    {
                        var impactEffect = rifleState.impactType == AbilityAutoRifle.ImpactType.Character
                            ? authClass.impactEffectCharacter
                            : authClass.impactEffectEnvironment;
                        vfxSystem.SpawnPointEffect(impactEffect, rifleState.fireEndPos, rifleState.impactNormal);

                        var impactSound = rifleState.impactType == AbilityAutoRifle.ImpactType.Character
                            ? authData.impactSoundChar
                            : authData.impactSoundEnv;
                        
                        SoundSystem.Instance.Play(impactSound, rifleState.fireEndPos);
                    }
                    
                    SoundSystem.Instance.Play(authData.soundRef, localToWorld.Position);
                    
                    state.LastFireTick = rifleState.fireTick;
                }
            }).Run();

            return default;
        }
    }
    
}