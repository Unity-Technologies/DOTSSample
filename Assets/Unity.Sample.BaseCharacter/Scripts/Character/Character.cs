using System;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;
using Unity.Sample.Core;
using UnityEngine;

public enum CameraProfile
{
    FirstPerson,
    Shoulder,
    ThirdPerson,
}



public static class Character
{
    [ConfigVar(Name = "character.showlifetime", DefaultValue = "0", Description = "Show character lifetime events")]
    public static ConfigVar ShowLifetime;

    public struct Settings : IComponentData
    {
        public NativeString64 characterName;

        public bool m_TeleportPending;
        public Vector3 m_TeleportToPosition;
        public Quaternion m_TeleportToRotation;
    }

    public struct State : ISystemStateComponentData
    {
        public int teamId;
//        public HeroTypeAsset heroTypeData;
//        public Entity presentation;    // TODO (mogensh) get rid of this
        public float altitude;
//        public Collider groundCollider;
        public float3 groundNormal;

        // TODO (mogensh) setup by hero. Where to put this ?
        public float eyeHeight;
        public HeroTypeAsset.SprintCameraSettings sprintCameraSettings;
    }

    [Serializable]
    public struct ReplicatedData : IComponentData
    {
        [GhostDefaultField]
        [NonSerialized] public int heroTypeIndex;
    }

    [Serializable]
    public struct PredictedData : IComponentData
    {
        [GhostDefaultField]
        public int tick;                    // Tick is only for debug purposes
        [GhostDefaultField(0)]
        public float3 position;
        [GhostDefaultField(0)]
        public float3 velocity;
        [GhostDefaultField]
        public bool sprinting;

        [GhostDefaultField]
        public CameraProfile cameraProfile;

        [GhostDefaultField]
        public int damageTick;
        [GhostDefaultField(1000)]
        public float3 damageDirection;
        public float damageImpulse;

#if UNITY_EDITOR
        public bool VerifyPrediction(ref PredictedData state)
        {
            return Vector3.Distance(position, state.position) < 0.1f
                   && Vector3.Distance(velocity, state.velocity) < 0.1f
                   && sprinting == state.sprinting
                   && damageTick == state.damageTick;
        }
#endif
    }

    [Serializable]
    public struct InterpolatedData : IComponentData
    {
        [GhostDefaultField(100, true)]
        public float3 Position;
        [GhostDefaultField(1, true)]
        [GhostAngleValue]
        public float rotation;
        [GhostDefaultField(1, true)]
        [GhostAngleValue]
        public float aimYaw;
        [GhostDefaultField(1, true)]
        [GhostAngleValue]
        public float aimPitch;
        [GhostDefaultField(1, true)]
        [GhostAngleValue]
        public float moveYaw;                                       // Global rotation 0->360 deg

        [GhostDefaultField]
        public Ability.AbilityAction.Action charAction;
        [GhostDefaultField]
        public int charActionTick;
        [GhostDefaultField]
        public int damageTick;
        [GhostDefaultField(10)]
        public float damageDirection;
        [GhostDefaultField]
        public bool sprinting;
        [GhostDefaultField(100, true)]
        public float sprintWeight;
        [GhostDefaultField(100, true)]
        public float crouchWeight;

        // Custom properties for Animation states
        [GhostDefaultField]
        public int selectorTargetSource;
        public int lastGroundMoveTick;
        [GhostDefaultField(1, true)]
        [GhostAngleValue]
        public float moveAngleLocal;                                // Movement rotation realtive to character forward -180->180 deg clockwise
        [GhostDefaultField(1000, true)]
        public float shootPoseWeight;
        [GhostDefaultField(1000, true)]
        public float2 locomotionVector;
        [GhostDefaultField(1000, true)]
        public float locomotionPhase;
        [GhostDefaultField(1000, true)]
        public float banking;
        [GhostDefaultField(100, true)]
        public float landAnticWeight;
        [GhostDefaultField(1)]
        public float turnStartAngle;
        [GhostDefaultField]
        public short turnDirection;                                 // -1 TurnLeft, 0 Idle, 1 TurnRight
        [GhostDefaultField(100, true)]
        public float squashTime;
        [GhostDefaultField(100, true)]
        public float squashWeight;
        [GhostDefaultField(100, true)]
        public float inAirTime;
        [GhostDefaultField(100, true)]
        public float jumpTime;
        [GhostDefaultField(100, true)]
        public float simpleTime;
        [GhostDefaultField(100, true)]
        public float2 footIkOffset;
        [GhostDefaultField(100, true)]
        public float3 footIkNormalLeft;
        [GhostDefaultField(100, true)]
        public float3 footIkNormalRight;
        [GhostDefaultField(100, true)]
        public float footIkWeight;
        [GhostDefaultField(100, true)]
        public float blendOutAim;
    }

    public static void TeleportTo(ref Settings settings, Vector3 position, Quaternion rotation)
    {
        settings.m_TeleportPending = true;
        settings.m_TeleportToPosition = position;
        settings.m_TeleportToRotation = rotation;
    }
}
