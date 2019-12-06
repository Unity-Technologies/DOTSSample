using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Sample.Core;
using UnityEngine;

public class HitCollisionQuery
{

    public struct ProjectileQuery
    {
        public float3 Start;
        public float3 End;
        public float Radius;
        public uint EnvironmentFilter;
        public uint HitColliderFilter;
        public uint HitColliderOwnerFlagFilter;
        
        public Entity ExcludedOwner;
        public ComponentDataFromEntity<HitCollider.Owner> ColliderOwnerFromEntity;
        public ComponentDataFromEntity<HitColliderOwner.State> ColliderOwnerStateFromEntity;
    }

    public struct ProjectileQueryResult
    {
        public bool Hit;
        public Entity ColliderOwner;
        public float3 Position;
        public float3 Normal;
    }

    public struct SplashQuery
    {
        public float3 Position;
        public float Radius;
        public uint EnvironmentFilter;
        public uint HitColliderFilter;
        public Entity ExcludedOwner;
        public ComponentDataFromEntity<HitCollider.Owner> ColliderOwnerFromEntity;
    }

    public struct SplashQueryResult
    {
        public Entity ColliderOwner;
        public float3 Position;
        public float Distance;
    }

    unsafe public static bool Query(in CollisionWorld collWorld, in ProjectileQuery query, ref ProjectileQueryResult result)
    {
        //GameDebug.Assert(query.HitColliderFromEntity != null,); TODO (mogensh) get "IsValid" check
        var environmentFilter = CollisionFilter.Default;
        environmentFilter.CollidesWith = query.EnvironmentFilter;
        var hitCollFilter = CollisionFilter.Default;
        hitCollFilter.CollidesWith = query.HitColliderFilter;

        // Raycast against environment and adjust end point of test on collision;
        var environmentRaycast = new RaycastInput
        {
            Start = query.Start,
            End = query.End,
            Filter = environmentFilter,
        };
        //Debug.DrawLine(environmentRaycast.Start,environmentRaycast.End,Color.gray,0.2f);
        var envCastResult = new Unity.Physics.RaycastHit();
        var envHit = collWorld.CastRay(environmentRaycast, out envCastResult);
        if (envHit)
        {
            var rigidBody = collWorld.Bodies[envCastResult.RigidBodyIndex];
            var entity = rigidBody.Entity;

            result.Hit = true;
            result.Position = envCastResult.Position;
            result.Normal = envCastResult.SurfaceNormal;

            if (query.ColliderOwnerFromEntity.Exists(entity))
            {
                result.ColliderOwner = rigidBody.Entity;
            }
        }
        //Debug.DrawLine(query.Start,query.End,Color.blue,0.2f);


        // Sphere cast against hit collision
        var sg = new SphereGeometry()
        {
            Center = float3.zero,
            Radius = query.Radius
        };
        var collider = Unity.Physics.SphereCollider.Create(sg, hitCollFilter);// TODO (mogensh) cache collider?
        var hitCollisionCast = new ColliderCastInput
        {
            Collider = (Unity.Physics.Collider*)collider.GetUnsafePtr(),
            Start = query.Start,
            End = envHit ? envCastResult.Position : query.End,
        };
        var hitCollCastResults = new NativeList<ColliderCastHit>(Allocator.Temp);
        var hitCollHit = collWorld.CastCollider(hitCollisionCast, ref hitCollCastResults);
        if (hitCollHit)
        {
//            GameDebug.Log("Hit coll hit. Count:" + hitCollCastResults.Length);
            var closest = -1;
            var minDistSq = float.MaxValue;
            for(int i=0;i<hitCollCastResults.Length;i++)
            {
                var collResult = hitCollCastResults[i];
                var rigidBody = collWorld.Bodies[collResult.RigidBodyIndex];
                var entity = rigidBody.Entity;

                // Check for HitCollider component
                if (!query.ColliderOwnerFromEntity.Exists(entity))
                {
// TODO (mogensh) NOT ALLOWED BY BURST
//                    GameDebug.LogWarning("HitCollider RigidBody entity do not have HitCollider component");
                    continue;
                }

                // Check distance
                var distSq = math.distancesq(collResult.Position,query.Start);
                if (distSq > minDistSq)
                    continue;

                var colliderOwner = query.ColliderOwnerFromEntity[entity];

                if (!query.ColliderOwnerStateFromEntity.HasComponent(colliderOwner.Value))
                {
// TODO (mogensh) NOT ALLOWED BY BURST
//                    GameDebug.LogError("HitCollider (" +entity + ") has no owner.");
                    continue;
                }

                // Check 
                var ownerState = query.ColliderOwnerStateFromEntity[colliderOwner.Value];
                if ((ownerState.colliderFlags & query.HitColliderOwnerFlagFilter) == 0)
                    continue;
                    
                // make sure owner is not in excluded set
                if (query.ExcludedOwner != Entity.Null && colliderOwner.Value == query.ExcludedOwner)
                {
                    continue;
                }

                minDistSq = distSq;
                closest = i;
            }

            if (closest != -1)
            {
                var queryResult = hitCollCastResults[closest];
                var body = collWorld.Bodies[queryResult.RigidBodyIndex];
                result.Hit = true;
                result.ColliderOwner = query.ColliderOwnerFromEntity[body.Entity].Value;
                result.Position = queryResult.Position;
                result.Normal = queryResult.SurfaceNormal;

                //DebugDraw.Sphere(result.Position,0.2f,Color.yellow,0.2f);
            }
        }

        collider.Dispose();
        hitCollCastResults.Dispose();

    // TODO (mogensh) I WANT DEBUG LINE FROM JOBS !
        if (result.Hit)
        {
            //DebugDraw.Sphere(result.Position,0.2f,Color.magenta,0.5f);
            //DebugDraw.Line(result.Position,result.Position+result.Normal*0.2f,Color.magenta,0.5f);
        }

        return result.Hit;
    }


    public static void Query(in CollisionWorld collWorld, in SplashQuery query, NativeHashMap<Entity,SplashQueryResult> results)
    {
        //GameDebug.Assert(query.HitColliderFromEntity != null,); TODO (mogensh) get "IsValid" check

        var environmentFilter = CollisionFilter.Default;        // TODO (mogensh) could we get one liner way to setup filder (CollisionFilter(uint,uint))
        environmentFilter.CollidesWith = query.EnvironmentFilter;
        var hitCollFilter = CollisionFilter.Default;
        hitCollFilter.CollidesWith = query.HitColliderFilter;

        var input = new PointDistanceInput
        {
            Position = query.Position,
            MaxDistance = query.Radius,
            Filter = hitCollFilter,
        };


        var distResults = new NativeList<DistanceHit>(Allocator.Temp);
        collWorld.CalculateDistance(input, ref distResults);

// TODO (mogensh) I WANT DEBUG LINE FROM JOBS !
//        DebugDraw.Sphere(query.Position,query.Radius,Color.red,0.5f);
        foreach (var distResult in distResults)
        {
            var rigidBody = collWorld.Bodies[distResult.RigidBodyIndex];
            var entity = rigidBody.Entity;

            if (!query.ColliderOwnerFromEntity.Exists(entity))
            {
                GameDebug.LogWarning("HitCollider RigidBody entity do not have HitCollider component");
                continue;
            }

            var colliderOwner = query.ColliderOwnerFromEntity[entity];
#if UNITY_EDITOR
            if (colliderOwner.Value == Entity.Null)
            {
// TODO (mogensh) NOT ALLOWED BY BURST
//                GameDebug.LogError("HitCollider has no owner");
                continue;
            }

#endif

            var owner = colliderOwner.Value;

            // make sure owner is not in excluded set
            if (query.ExcludedOwner != Entity.Null && colliderOwner.Value == query.ExcludedOwner)
            {
                continue;
            }

            // TODO (mogens) occlusion test against environment. Multiple rays pr body and not just center ?

            var entityResult = new SplashQueryResult();
            if (results.TryGetValue(owner, out entityResult))
            {
                if (distResult.Distance < entityResult.Distance)
                {
                    entityResult.ColliderOwner = colliderOwner.Value;
                    entityResult.Distance = distResult.Distance;
                    entityResult.Position = distResult.Position;
                    results[owner] = entityResult;
                }
            }
            else
            {
                results.TryAdd(owner, new SplashQueryResult
                {
                    ColliderOwner = colliderOwner.Value,
                    Distance = distResult.Distance,
                    Position = distResult.Position,
                });
            }
        }
        distResults.Dispose();

// TODO (mogensh) I WANT DEBUG LINE FROM JOBS !
//        foreach (var value in results.GetValueArray(Allocator.Temp))
//        {
//            DebugDraw.Sphere(value.Position,0.1f,Color.red,0.5f);
//        }
    }
}
