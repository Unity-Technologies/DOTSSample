using System;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.NetCode;
using Unity.Sample.Core;


public static class Ability
{
    [ConfigVar(Name = "ability.showdebug", DefaultValue = "0", Description = "show ability debug")]
    public static ConfigVar DebugShow;


    // TODO (mogensh) AbilityType and AbilityAction should be modifiable/extendable without having to change code. Should be possible to include 2 packages with abilities without code change.
    public enum AbilityType
    {
        Locomotion        = 1 << 0,
        Ability           = 1 << 1,
        LocoModifier      = 1 << 2,
        Death             = 1 << 3,
    }

    [Serializable]
    public struct AbilityAction : IComponentData
    {
        public static AbilityAction Default => new AbilityAction();

        public enum Action
        {
            None,
            PrimaryFire,
            SecondaryFire,
            Reloading,
            Melee,
            NumActions,
        }

        [GhostDefaultField]
        public Action action;
        [GhostDefaultField]
        public int actionStartTick;

        public void SetAction(Action action, int tick)
        {
            //        GameDebug.Log("SetAction:" + action + " tick:" + tick);
            this.action = action;
            this.actionStartTick = tick;
        }
    }

    // Added to abilities that are in an enabled ability collection. Ability might not be running
    public struct EnabledAbility : IComponentData
    {
        public static EnabledAbility Default => new EnabledAbility { activeButtonIndex = -1 };

        public Entity owner;
        public int activeButtonIndex;    // Index of active ability button. -1 if non is active// TODO (mogensh) this should be changed to button flags as we want to detect multiple down
    }

    public struct AbilityStateIdle : IComponentData
    {
        public bool requestActive;
    }

    public struct AbilityStateActive : IComponentData
    {
        public bool requestCooldown;
    }

    public struct AbilityStateCooldown : IComponentData
    {
        public bool requestIdle;
    }

    // Added to abilities that are requested to deactivate
    public struct AbilityRequestDeactivate : IComponentData
    {
    }

    // Added to all ability entities. Used by AbilityOwner to control ability state.
    // Property behaviorState drives what AbilityStateXXX components should be added. This is needed as we can't replicate or rollback component add/remove
    public struct AbilityControl : IComponentData
    {
        public enum State
        {
            Idle,
            Active,
            Cooldown,
        }

        [GhostDefaultField]
        public State behaviorState;
        [GhostDefaultField]
        public bool requestDeactivate;
    }

    public enum AbilityTagValue
    {
        Melee = 1,
        Movement = 2,
        AutoRifle = 3,
        SelectSlot = 4,
        Dead = 5,
        Sprint = 6
    }

    public struct AbilityTag : IComponentData
    {
        public AbilityTagValue Value;
    }

    public static Entity FindAbility(BufferFromEntity<AbilityOwner.OwnedAbility> bufferFromEntity, Entity entity, AbilityTagValue tagValue)
    {
        if (!bufferFromEntity.Exists(entity))
            return Entity.Null;

        var ownedAbilities = bufferFromEntity[entity];
        for (int i = 0; i < ownedAbilities.Length; i++)
        {
            var ability = ownedAbilities[i];
            if (ability.tagValue == tagValue)
                return ability.entity;
        }

        return Entity.Null;
    }

    public static Entity FindActiveAction(BufferFromEntity<AbilityOwner.OwnedAbility> bufferFromEntity, Entity entity)
    {
        if (!bufferFromEntity.Exists(entity))
            return Entity.Null;

        var ownedAbilities = bufferFromEntity[entity];
        for (int i = 0; i < ownedAbilities.Length; i++)
        {
            var ability = ownedAbilities[i];
            if (ability.isAction && ability.isActive)
                return ability.entity;
        }

        return Entity.Null;
    }

    // As we cannot replicate or rollback component add/remove we use abilityCtrl to control what flag components should be added
    [UpdateInGroup(typeof(AbilityUpdateSystemGroup))]
    [UpdateBefore(typeof(BehaviourRequestPhase))]
    [DisableAutoCreation]
    [AlwaysSynchronizeSystem]
    public class UpdateAbilityFlags : JobComponentSystem
    {
        EntityQueryMask m_abilityStateIdleMask;
        EntityQueryMask m_abilityStateActiveMask;
        EntityQueryMask m_abilityStateCooldownMask;
        EntityQueryMask m_abilityRequestDeactivateMask;

        protected override void OnCreate()
        {
            m_abilityStateIdleMask = EntityManager.GetEntityQueryMask(GetEntityQuery(typeof(AbilityStateIdle)));
            m_abilityStateActiveMask = EntityManager.GetEntityQueryMask(GetEntityQuery(typeof(AbilityStateActive)));
            m_abilityStateCooldownMask = EntityManager.GetEntityQueryMask(GetEntityQuery(typeof(AbilityStateCooldown)));
            m_abilityRequestDeactivateMask = EntityManager.GetEntityQueryMask(GetEntityQuery(typeof(AbilityRequestDeactivate)));
        }

        protected override JobHandle OnUpdate(JobHandle inputDeps)
        {
            var PostUpdateCommands = new EntityCommandBuffer(Allocator.TempJob);

            var abilityStateIdleMask = m_abilityStateIdleMask;
            var abilityStateActiveMask = m_abilityStateActiveMask;
            var abilityStateCooldownMask = m_abilityStateCooldownMask;
            var abilityRequestDeactivateMask = m_abilityRequestDeactivateMask;

            Entities.WithAll<EnabledAbility>()
                .WithNativeDisableContainerSafetyRestriction(PostUpdateCommands)
                .WithChangeFilter<AbilityControl>()
                .ForEach((Entity entity, in AbilityControl abilityCtrl) =>
                {
                    if (abilityCtrl.behaviorState == AbilityControl.State.Idle)
                    {
                        if (!abilityStateIdleMask.Matches(entity))
                        {
                            PostUpdateCommands.AddComponent<AbilityStateIdle>(entity);
                        }

                        if (abilityStateActiveMask.Matches(entity))
                        {
                            PostUpdateCommands.RemoveComponent<AbilityStateActive>(entity);
                        }

                        if (abilityStateCooldownMask.Matches(entity))
                        {
                            PostUpdateCommands.RemoveComponent<AbilityStateCooldown>(entity);
                        }
                    }
                    else if (abilityCtrl.behaviorState == AbilityControl.State.Active)
                    {
                        if (abilityStateIdleMask.Matches(entity))
                        {
                            PostUpdateCommands.RemoveComponent<AbilityStateIdle>(entity);
                        }

                        if (!abilityStateActiveMask.Matches(entity))
                        {
                            PostUpdateCommands.AddComponent<AbilityStateActive>(entity);
                        }

                        if (abilityStateCooldownMask.Matches(entity))
                        {
                            PostUpdateCommands.RemoveComponent<AbilityStateCooldown>(entity);
                        }

                    }
                    else if (abilityCtrl.behaviorState == AbilityControl.State.Cooldown)
                    {
                        if (abilityStateIdleMask.Matches(entity))
                        {
                            PostUpdateCommands.RemoveComponent<AbilityStateIdle>(entity);
                        }

                        if (abilityStateActiveMask.Matches(entity))
                        {
                            PostUpdateCommands.RemoveComponent<AbilityStateActive>(entity);
                        }

                        if (!abilityStateCooldownMask.Matches(entity))
                        {
                            PostUpdateCommands.AddComponent<AbilityStateCooldown>(entity);
                        }
                    }

                    if (abilityCtrl.requestDeactivate)
                    {
                        if (!abilityRequestDeactivateMask.Matches(entity))
                        {
                            PostUpdateCommands.AddComponent<AbilityRequestDeactivate>(entity);
                        }
                    }
                    else
                    {
                        if (abilityRequestDeactivateMask.Matches(entity))
                        {
                            PostUpdateCommands.RemoveComponent<AbilityRequestDeactivate>(entity);
                        }
                    }

                }).Run();


            PostUpdateCommands.Playback(EntityManager);
            PostUpdateCommands.Dispose();
            return default;
        }
    }
}
