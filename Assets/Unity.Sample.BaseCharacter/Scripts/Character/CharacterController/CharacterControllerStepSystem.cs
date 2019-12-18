using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Physics.Extensions;
using Unity.Physics.Systems;
using Unity.Transforms;
using Unity.NetCode;

[UpdateInGroup(typeof(AbilityUpdateSystemGroup))]
[UpdateAfter(typeof(MovementUpdatePhase))]
[UpdateBefore(typeof(CharacterControllerStepSystem))]
[DisableAutoCreation]
[AlwaysSynchronizeSystem]
public class CharacterControllerFollowGroundSystem : JobComponentSystem
{
    BuildPhysicsWorld m_BuildPhysicsWorldSystem;

    protected override void OnCreate()
    {
        m_BuildPhysicsWorldSystem = World.GetOrCreateSystem<BuildPhysicsWorld>();
    }

    protected override JobHandle OnUpdate(JobHandle inputDeps)
    {
        inputDeps.Complete();

        var physicsWorld = m_BuildPhysicsWorldSystem.PhysicsWorld;
        var PredictingTick = World.GetExistingSystem<GhostPredictionSystemGroup>().PredictingTick;

        Entities
            .ForEach((
                ref CharacterControllerComponentData ccData,
                ref CharacterControllerInitializationData ccInitData,
                ref CharacterControllerMoveQuery ccMoveQuery,
                ref CharacterControllerVelocity ccVelocity,
                in PredictedGhostComponent predictedGhostComponent) =>
        {
            if (!GhostPredictionSystemGroup.ShouldPredict(PredictingTick, predictedGhostComponent))
                return;
            if (!ccMoveQuery.FollowGround)
                return;

            var vel = ccVelocity.Velocity;
            if (math.lengthsq(vel) == 0.0f)
                return;

            var skinWidth = ccData.SkinWidth;
            var startPos = ccMoveQuery.StartPosition - math.up() * skinWidth;
            var dir = math.normalizesafe(vel);
            var horizDir = new float3(dir.x, 0.0f, dir.z);
            var len = ccInitData.CapsuleRadius;
            var endPos = startPos + len * dir;
            var slopeAdjustment = math.up() * len * math.tan(ccData.MaxSlope);
            var rayInput = new RaycastInput
            {
                Start = endPos + slopeAdjustment,
                End = endPos - slopeAdjustment,
                Filter = new CollisionFilter { BelongsTo = 1, CollidesWith = 1, GroupIndex = 0 }
            };
            var rayHit = new RaycastHit();
            if (!physicsWorld.CastRay(rayInput, out rayHit))
                return;

            var newDir = math.normalize(rayHit.Position - startPos);
            var newHorizDir = new float3(newDir.x, 0.0f, newDir.z);
            var newVel = newDir * math.length(vel) * math.length(horizDir) / math.length(newHorizDir);
            if (math.abs(newVel.y) > 0.01f)
                ccVelocity.Velocity = newVel;
        }).Run();

        return default;
    }
}

[UpdateInGroup(typeof(AbilityUpdateSystemGroup))]
[UpdateAfter(typeof(MovementUpdatePhase))]
[UpdateBefore(typeof(MovementResolvePhase))]
[DisableAutoCreation]
[AlwaysSynchronizeSystem]
public class CharacterControllerStepSystem : JobComponentSystem
{
    [BurstCompile]
    struct ApplyDeferredImpulses : IJob
    {
        public NativeStream.Reader DeferredImpulseReader;

        public ComponentDataFromEntity<PhysicsVelocity> PhysicsVelocityData;
        public ComponentDataFromEntity<PhysicsMass> PhysicsMassData;
        public ComponentDataFromEntity<Translation> TranslationData;
        public ComponentDataFromEntity<Rotation> RotationData;

        public void Execute()
        {
            int index = 0;
            int maxIndex = DeferredImpulseReader.ForEachCount;
            DeferredImpulseReader.BeginForEachIndex(index++);
            while (DeferredImpulseReader.RemainingItemCount == 0 && index < maxIndex)
            {
                DeferredImpulseReader.BeginForEachIndex(index++);
            }

            while (DeferredImpulseReader.RemainingItemCount > 0)
            {
                // Read the data
                var impulse = DeferredImpulseReader.Read<DeferredCharacterControllerImpulse>();
                while (DeferredImpulseReader.RemainingItemCount == 0 && index < maxIndex)
                {
                    DeferredImpulseReader.BeginForEachIndex(index++);
                }

                PhysicsVelocity pv = PhysicsVelocityData[impulse.Entity];
                PhysicsMass pm = PhysicsMassData[impulse.Entity];
                Translation t = TranslationData[impulse.Entity];
                Rotation r = RotationData[impulse.Entity];

                // Don't apply on kinematic bodies
                if (pm.InverseMass > 0.0f)
                {
                    // Apply impulse
                    pv.ApplyImpulse(pm, t, r, impulse.Impulse, impulse.Point);

                    // Write back
                    PhysicsVelocityData[impulse.Entity] = pv;
                }
            }
        }
    }

    BuildPhysicsWorld m_BuildPhysicsWorldSystem;

    EntityQuery m_CharacterControllersGroup;
    EntityQuery m_TimeSingletonQuery;

    protected override void OnCreate()
    {
        m_BuildPhysicsWorldSystem = World.GetOrCreateSystem<BuildPhysicsWorld>();
        m_CharacterControllersGroup = GetEntityQuery( new EntityQueryDesc
        {
            All = new []
            {
                ComponentType.ReadWrite<CharacterControllerComponentData>(),
            }
        });

        m_TimeSingletonQuery = GetEntityQuery(ComponentType.ReadOnly<GlobalGameTime>());
    }

    protected unsafe override JobHandle OnUpdate(JobHandle inputDeps)
    {
        var entityCount = m_CharacterControllersGroup.CalculateEntityCount();
        if (entityCount == 0)
            return inputDeps;

        var deferredImpulses = new NativeStream(entityCount, Allocator.TempJob);
        var time = m_TimeSingletonQuery.GetSingleton<GlobalGameTime>().gameTime;
        var physicsWorld = m_BuildPhysicsWorldSystem.PhysicsWorld;

        var writer = deferredImpulses.AsWriter();

        var constraints = new NativeList<SurfaceConstraintInfo>(Allocator.Temp);
        var castHits = new NativeList<ColliderCastHit>(Allocator.Temp);
        var distanceHits = new NativeList<DistanceHit>(Allocator.Temp);

        var PredictingTick = World.GetExistingSystem<GhostPredictionSystemGroup>().PredictingTick;

        Entities
            .WithName("CharacterControllerStepSystem")
            .ForEach((
                ref CharacterControllerComponentData ccData,
                ref CharacterControllerCollider ccCollider,
                ref CharacterControllerMoveQuery moveQuery,
                ref CharacterControllerMoveResult moveResult,
                ref CharacterControllerVelocity velocity,
                in PredictedGhostComponent predictedGhostComponent) =>
            {
                if (!GhostPredictionSystemGroup.ShouldPredict(PredictingTick, predictedGhostComponent))
                    return;

                constraints.Clear();
                castHits.Clear();
                distanceHits.Clear();

                var collider = (Collider*)ccCollider.Collider.GetUnsafePtr();

                var stepInput = new CharacterControllerUtilities.CharacterControllerStepInput
                {
                    World = physicsWorld,
                    DeltaTime = time.tickDuration,
                    Up = math.up(),
                    Gravity = new float3(0.0f, -9.8f, 0.0f),
                    MaxIterations = ccData.MaxIterations,
                    Tau = CharacterControllerUtilities.k_DefaultTau,
                    Damping = CharacterControllerUtilities.k_DefaultDamping,
                    SkinWidth = ccData.SkinWidth,
                    ContactTolerance = ccData.ContactTolerance,
                    MaxSlope = ccData.MaxSlope,
                    RigidBodyIndex = -1,
                    CurrentVelocity = velocity.Velocity,
                    MaxMovementSpeed = ccData.MaxMovementSpeed,
                    FollowGround = moveQuery.FollowGround
                };

                var transform = new RigidTransform
                {
                    pos = moveQuery.StartPosition,
                    rot = quaternion.identity
                };

                // World collision + integrate
                CharacterControllerUtilities.CollideAndIntegrate(
                    stepInput,
                    ccData.CharacterMass,
                    ccData.AffectsPhysicsBodies > 0,
                    collider,
                    ref transform,
                    ref velocity.Velocity,
                    ref writer,
                    ref constraints,
                    ref castHits,
                    ref distanceHits);

                moveResult.MoveResult = transform.pos;
            }).Run();

        var applyJob = new ApplyDeferredImpulses()
        {
            DeferredImpulseReader = deferredImpulses.AsReader(),
            PhysicsVelocityData = GetComponentDataFromEntity<PhysicsVelocity>(),
            PhysicsMassData = GetComponentDataFromEntity<PhysicsMass>(),
            TranslationData = GetComponentDataFromEntity<Translation>(),
            RotationData = GetComponentDataFromEntity<Rotation>()
        };
        applyJob.Run();

        deferredImpulses.Dispose();
        constraints.Dispose();
        castHits.Dispose();
        distanceHits.Dispose();


        return inputDeps;
    }
}
