using System;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Physics;
using UnityEngine;
using Unity.NetCode;
using static CharacterControllerUtilities;
using static Unity.Physics.PhysicsStep;

[Serializable]
public struct CharacterControllerComponentData : IComponentData
{
    public float3 GroundProbeVector;
    public float MaxSlope; // radians
    public int MaxIterations;
    public float CharacterMass;
    public float SkinWidth;
    public float ContactTolerance;
    public int AffectsPhysicsBodies;
    public float MaxMovementSpeed;
}

public struct CharacterControllerGroundSupportData : IComponentData
{
    [GhostDefaultField(0)]
    public float3 SurfaceNormal;
    [GhostDefaultField(0)]
    public float3 SurfaceVelocity;
    [GhostDefaultField]
    public CharacterSupportState SupportedState;
}

public struct CharacterControllerMoveQuery : IComponentData
{
    public float3 StartPosition;
    public bool FollowGround;
    public bool CheckSupport;
}

public struct CharacterControllerMoveResult : IComponentData
{
    [GhostDefaultField(0)]
    public float3 MoveResult;
}

public struct CharacterControllerVelocity : IComponentData
{
    [GhostDefaultField(0)]
    public float3 Velocity;
}

public struct CharacterControllerInitializationData : IComponentData
{
    public float3 CapsuleCenter;
    public float CapsuleRadius;
    public float CapsuleHeight;
}

struct CharacterControllerCollider : ISystemStateComponentData
{
    public BlobAssetReference<Unity.Physics.Collider> Collider;
}

[Serializable]
public class CharacterControllerAuthoring : MonoBehaviour, IConvertGameObjectToEntity
{
    // Gravity force applied to the character controller body
    public float3 Gravity = Default.Gravity;

    // Maximum slope angle character can overcome (in degrees)
    public float MaxSlope = 60.0f;

    // Maximum number of character controller solver iterations
    public int MaxIterations = 10;

    // Mass of the character (used for affecting other rigid bodies)
    public float CharacterMass = 1.0f;

    // Keep the character at this distance to planes (used for numerical stability)
    public float SkinWidth = 0.02f;

    // Anything in this distance to the character will be considered a potential contact
    // when checking support
    public float ContactTolerance = 0.1f;

    // Whether to affect other rigid bodies
    public int AffectsPhysicsBodies = 1;

    // Maximum speed of movement at any given time
    public float MaxMovementSpeed = 10.0f;

    public float3 GroundProbeVector = new float3(0.0f, -0.1f, 0.0f);

    public float3 CapsuleCenter = new float3(0.0f, 1.0f, 0.0f);
    public float CapsuleRadius = 0.5f;
    public float CapsuleHeight = 2.0f;

    void IConvertGameObjectToEntity.Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
    {
        if (enabled)
        {
            dstManager.AddComponentData(entity, new CharacterControllerComponentData
            {
                GroundProbeVector = GroundProbeVector,
                MaxSlope = math.radians(MaxSlope),
                MaxIterations = MaxIterations,
                CharacterMass = CharacterMass,
                SkinWidth = SkinWidth,
                ContactTolerance = ContactTolerance,
                AffectsPhysicsBodies = AffectsPhysicsBodies,
                MaxMovementSpeed = MaxMovementSpeed
            });

            dstManager.AddComponentData(entity, new CharacterControllerInitializationData
            {
                CapsuleCenter = CapsuleCenter,
                CapsuleHeight = CapsuleHeight,
                CapsuleRadius = CapsuleRadius
            });

            dstManager.AddComponent(entity, typeof(CharacterControllerVelocity));
            dstManager.AddComponent(entity, typeof(CharacterControllerMoveQuery));
            dstManager.AddComponent(entity, typeof(CharacterControllerMoveResult));
            dstManager.AddComponent(entity, typeof(CharacterControllerGroundSupportData));
        }
    }
}

[DisableAutoCreation]
[AlwaysSynchronizeSystem]
[AlwaysUpdateSystem]
[UpdateInGroup(typeof(AbilityUpdateSystemGroup))]
[UpdateAfter(typeof(MovementUpdatePhase))]
[UpdateBefore(typeof(CharacterControllerStepSystem))]
public class CharacterControllerInitAndCleanupSystem : JobComponentSystem
{
    protected override JobHandle OnUpdate(JobHandle inputDeps)
    {
        var ecb  = new EntityCommandBuffer(Allocator.TempJob);

        Entities
            .WithNone<CharacterControllerCollider>()
            .ForEach((Entity e, ref CharacterControllerInitializationData initData) =>
            {
                var capsule = new CapsuleGeometry
                {
                    Vertex0 = initData.CapsuleCenter + new float3(0, 0.5f * initData.CapsuleHeight - initData.CapsuleRadius, 0),
                    Vertex1 = initData.CapsuleCenter - new float3(0, 0.5f * initData.CapsuleHeight - initData.CapsuleRadius, 0),
                    Radius = initData.CapsuleRadius
                };
                var filter = new CollisionFilter { BelongsTo = 1, CollidesWith = 1, GroupIndex = 0 };
                var collider = Unity.Physics.CapsuleCollider.Create(capsule, filter, new Unity.Physics.Material { Flags = new Unity.Physics.Material.MaterialFlags() });
                ecb.AddComponent(e, new CharacterControllerCollider { Collider = collider });
            }).Run();

        Entities
            .WithNone<CharacterControllerComponentData>()
            .ForEach((Entity e, ref CharacterControllerCollider collider) =>
                {
                    collider.Collider.Dispose();
                    ecb.RemoveComponent<CharacterControllerCollider>(e);
                }).Run();

        ecb.Playback(EntityManager);
        ecb.Dispose();

        return inputDeps;
    }
}
