using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Sample.Core;
using UnityEngine;

#if UNITY_EDITOR
public partial class TerraformerMovementPart : MonoBehaviour, IConvertGameObjectToEntity
{
    public AuthoringData data;
    
    public void Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
    {
        var state = State.Default;
        
        dstManager.AddComponentData(entity, data);
        dstManager.AddComponentData(entity, state);
    }
}
#endif


public partial class TerraformerMovementPart 
{
    [Serializable]
    public struct AuthoringData : IComponentData
    {
        public static AuthoringData Default => new AuthoringData();
        [AssetType(typeof(SoundDef))]
        public WeakAssetReference jumpSound;
        [AssetType(typeof(SoundDef))]
        public WeakAssetReference doubleJumpSound;
        [AssetType(typeof(SoundDef))]
        public WeakAssetReference landSound;
        [AssetType(typeof(SoundDef))]
        public WeakAssetReference footstepsSound;
    }

    public struct AbilityEntity : IComponentData
    {
        public Entity Value;
    }

    public struct State : IComponentData
    {
        public static State Default => new State();
        public int LastStateChangeTick;
        public SoundSystem.SoundHandle footstapSoundHandle;
    }

    [UpdateInGroup(typeof(InitializationSystemGroup))]
    [AlwaysSynchronizeSystem]
    public class Initialize : JobComponentSystem
    {
        protected override JobHandle OnUpdate(JobHandle inputDeps)
        {
            inputDeps.Complete();

            var commands = new EntityCommandBuffer(Allocator.TempJob);
            var ownedAbilityBufferFromEntity = GetBufferFromEntity<AbilityOwner.OwnedAbility>(true);

            Entities
                .WithAll<AuthoringData>()
                .WithNone<AbilityEntity>()
                .ForEach((Entity entity, ref Part.Owner partOwner) =>
            {
                var abilityEntity = Ability.FindAbility(ownedAbilityBufferFromEntity, partOwner.Value, AbilityMovement.Tag);
                if (abilityEntity == Entity.Null)
                    return;
                
                var ability = new AbilityEntity
                {
                    Value = abilityEntity
                };

                commands.AddComponent(entity,ability);

            }).Run();

            commands.Playback(EntityManager);
            commands.Dispose();

            return default;
        }
    }

    [UpdateInGroup(typeof(PresentationSystemGroup))]
    [AlwaysSynchronizeSystem]
    public class Update : JobComponentSystem
    {
        protected override JobHandle OnUpdate(JobHandle inputDeps)
        {
            var PostUpdateCommands = new EntityCommandBuffer(Allocator.TempJob);
            
            // TODO: Burst not compatible with accessing EntityManager
            Entities.WithNativeDisableContainerSafetyRestriction(PostUpdateCommands)
                .WithoutBurst()
                .ForEach((Entity entity, ref AuthoringData authData, ref AbilityEntity ability, ref State state, ref Unity.Transforms.LocalToWorld localToWorld) =>
            {
                if (!EntityManager.Exists(ability.Value))
                {
                    GameDebug.LogWarning(World,"Ability entity:{0}" + ability.Value + " does no longer exist");
                    return;
                }
                
                if (!EntityManager.HasComponent<AbilityMovement.InterpolatedState>(ability.Value))
                {
                    GameDebug.LogWarning(World,"Ability entity:{0}" + ability.Value + " does not have Ability_AutoRifle.InterpolatedState component");
                    return;
                }
                    
                var movementState = EntityManager.GetComponentData<AbilityMovement.InterpolatedState>(ability.Value);

                if(!state.footstapSoundHandle.IsNull() && movementState.charLocoState != AbilityMovement.LocoState.GroundMove)
                {
                    SoundSystem.Instance.Stop(state.footstapSoundHandle, 0.5f);
                    state.footstapSoundHandle = new SoundSystem.SoundHandle();
                }
                else if (movementState.charLocoState == AbilityMovement.LocoState.GroundMove && state.footstapSoundHandle.IsNull())
                {
                    state.footstapSoundHandle = SoundSystem.Instance.Play(authData.footstepsSound, localToWorld.Position);
                    // Create sound position tracker
                    var e = PostUpdateCommands.CreateEntity();
                    PostUpdateCommands.AddComponent(e, new SoundRequest() { soundHandle = state.footstapSoundHandle, trackEntity = entity });
                }

                if (movementState.charLocoTick > state.LastStateChangeTick)    // This will trigger for late joiners 
                {
                    if(movementState.charLocoState == AbilityMovement.LocoState.Jump)
                    {
                        SoundSystem.Instance.Play(authData.jumpSound, localToWorld.Position);
                    }
                    else if (movementState.charLocoState == AbilityMovement.LocoState.DoubleJump)
                    {
                        SoundSystem.Instance.Play(authData.doubleJumpSound, localToWorld.Position);
                    }
                    else if (movementState.previousCharLocoState == AbilityMovement.LocoState.InAir)
                    {
                        SoundSystem.Instance.Play(authData.landSound, localToWorld.Position);
                    }

                    state.LastStateChangeTick = movementState.charLocoTick;
                }
            }).Run();
            
            PostUpdateCommands.Playback(EntityManager);
            PostUpdateCommands.Dispose();

            return default;
        }
    }
    
}