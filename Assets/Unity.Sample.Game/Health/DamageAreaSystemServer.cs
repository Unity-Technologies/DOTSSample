using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;

public struct DamageArea : IComponentData
{
    public bool instantKill;
    public float hitsPerSecond;
    public float damagePerHit;
    public float3 size;
}

[DisableAutoCreation][AlwaysSynchronizeSystem]
public class DamageAreaSystemServer : JobComponentSystem
{
    private EntityQuery m_EntitiesToDamageQuery;

    protected override void OnCreate()
    {
        m_EntitiesToDamageQuery = GetEntityQuery(typeof(DamageEvent), typeof(HealthStateData),
            typeof(Unity.Transforms.LocalToWorld));
    }

    protected override JobHandle OnUpdate(JobHandle inputDeps)
    {
        // Gather all entities that have Health and can take damage and their positions
        var entities = m_EntitiesToDamageQuery.ToEntityArray(Allocator.TempJob);
        if (entities.Length == 0)
        {
            entities.Dispose();
            return default;
        }

        var positions = new NativeArray<float3>(entities.Length, Allocator.TempJob);

        for (var i = 0; i < entities.Length; ++i)
        {
            positions[i] = EntityManager.GetComponentData<LocalToWorld>(entities[i]).Position;
        }

        var healthStateData = GetComponentDataFromEntity<HealthStateData>(true);
        var damageEventBufferData = GetBufferFromEntity<DamageEvent>(false);

        // Loop over all damage areas
        Entities
            .WithReadOnly(healthStateData)
            .WithoutBurst()
            .ForEach((Entity e, ref DamageArea damageArea, ref Unity.Transforms.LocalToWorld t) =>
        {
            var invT = math.transpose(new float3x3(t.Value.c0.xyz, t.Value.c1.xyz, t.Value.c2.xyz));
            var pos = t.Position;

            // Check if entity is inside
            for (int i = 0, c = positions.Length; i < c; ++i)
            {
                float3 localpos = math.mul(invT, pos - positions[i]);
                Entity entity = entities[i];
                if(math.abs(localpos.x) > damageArea.size.x ||
                   math.abs(localpos.y) > damageArea.size.y ||
                   math.abs(localpos.z) > damageArea.size.z)
                {
                    continue;
                }
                var healthState = healthStateData[entity];
                if (healthState.health <= 0)
                    continue;

                var damageEvent = new DamageEvent
                {
                    Target = entity,
                    Instigator = Entity.Null,
                    Damage = 10000.0f,
                    Direction = Vector3.zero,
                    Impulse = 0,
                };

                var damageEventBuffer = damageEventBufferData[entity];
                damageEventBuffer.Add(damageEvent);
            }
        }).Run();

        /*
        var damageAreaArray = Group.ToComponentArray<DamageArea>();
        for (int idx = 0; idx < damageAreaArray.Length; ++idx)
        {
            var area = damageAreaArray[idx];
            var damageAmount = area.damagePerHit;
            var ticksBetweenDamage = Mathf.FloorToInt(1.0f / (area.hitsPerSecond * m_GameWorld.GetWorldTime().tickInterval));
            if (area.instantKill)
                damageAmount = 100000.0f;
            var charactersInside = area.charactersInside;

            // Iterating backwards as we need to clear out any destroyed characters
            for (var i = charactersInside.Count - 1; i >= 0; --i)
            {
                if (charactersInside[i].hitCollisionOwner == Entity.Null || !EntityManager.Exists(charactersInside[i].hitCollisionOwner))
                {
                    charactersInside.EraseSwap(i);
                    continue;
                }

                var healthState = EntityManager.GetComponentData<HealthStateData>(charactersInside[i].hitCollisionOwner);
                if (healthState.health <= 0)
                    continue;

                if (m_GameWorld.GetWorldTime().tick > charactersInside[i].nextDamageTick)
                {
                    var damageEventBuffer = EntityManager.GetBuffer<DamageEvent>(charactersInside[i].hitCollisionOwner);
                    DamageEvent.AddEvent(damageEventBuffer, Entity.Null, damageAmount, Vector3.zero, 0);

                    var info = charactersInside[i];
                    info.nextDamageTick = m_GameWorld.GetWorldTime().tick + ticksBetweenDamage;
                    charactersInside[i] = info;
                }
            }
        }
        */

        entities.Dispose();
        positions.Dispose();

        return default;
    }
}
