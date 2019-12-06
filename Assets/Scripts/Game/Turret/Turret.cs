using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Transforms;
using UnityEngine;
using Unity.NetCode;

public class Turret
{
    public struct State : IComponentData
    {
        public static State Default => new State();
        public WeakAssetReference Projectile;
        public int NextShootTick;
    }

    [UpdateInGroup(typeof(ServerSimulationSystemGroup))]
    [AlwaysSynchronizeSystem]
    class Update : JobComponentSystem
    {
        protected override JobHandle OnUpdate(JobHandle inputDeps)
        {
            inputDeps.Complete();

            var damageDeps = new JobHandle();
            var damageManager = World.GetExistingSystem<DamageManager>();
            if (damageManager == null)
            {
//                GameDebug.LogError("Could not find DamageManager system");
                return inputDeps;
            }
            var damageEvents = damageManager.GetDamageBufferWriter(out damageDeps);
            damageDeps.Complete();



            var globalTime = GetEntityQuery(ComponentType.ReadOnly<GlobalGameTime>()).GetSingleton<GlobalGameTime>();



            Entities            
                .WithReadOnly(globalTime).ForEach((State state, LocalToWorld ltw) =>
            {
                if (globalTime.gameTime.tick > state.NextShootTick)
                {
                    state.NextShootTick = globalTime.gameTime.tick + (int)(1f/globalTime.gameTime.tickInterval);

                    var rot = new quaternion(ltw.Value);
                    var dir = math.mul(rot,new float3(0, 0, 1));
                    var pos = ltw.Value.c3.xyz;

//                    ProjectileRequest.Create(PostUpdateCommands, globalTime.gameTime.tick,
//                        state.Projectile, Entity.Null, -1, pos, pos + dir*100);



//                    var collisionTestTick = 0;
//                    Debug.DrawLine(pos, pos + dir * 10, Color.red,0.5f);
//
//                    var query = new HitCollisionQuery.ProjectileQuery
//                    {
//                        ColliderOwnerFromEntity = GetComponentDataFromEntity<HitCollider.Owner>(true),
//                        EnvironmentFilter = 1u << 0,
//                        HitColliderFilter = 1u << 1,
//                        ExcludedOwner = Entity.Null,
//                        Start = pos,
//                        End = (float3)pos + dir*100,
//                        Radius = 0.2f,
//                    };
//                    var result = new HitCollisionQuery.ProjectileQueryResult();
//
//
//                    var collisionTestTick = 0;
//                    var collWorldValid = World.GetExistingSystem<PhysicsWorldHistory>().CollisionHistory
//                        .GetCollisionWorld(collisionTestTick, out var collisionWorld);
//                    if (!collWorldValid)
//                    {
//                        collisionWorld.Dispose();
////                        GameDebug.LogError("Turrent: No valid collision world for tick:" + collisionTestTick);
//                        return;
//                    }
//
//                    HitCollisionQuery.Query(collisionWorld, query, ref result);
//
//                    if (result.Hit)
//                    {
//                        var damageEvent = new DamageEvent
//                        {
//                            Target = result.ColliderOwner,
//                            Instigator = Entity.Null,
//                            Damage = 26,
//                            Direction = math.normalize(query.End - query.Start),
//                            Impulse = 0
//                        };
//
//                        damageEvents.Add(damageEvent.Target, damageEvent);
//
////                        GameDebug.Log("Add damage to:" + damageEvent.Target + " damage:" + damageEvent.Damage);
//                    }
                }
            }).Run();

            return default;
        }
    }
}
