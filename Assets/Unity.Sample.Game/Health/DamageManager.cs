using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Sample.Core;
using UnityEngine;

[ExecuteAlways]
[DisableAutoCreation]
public class HandleDamageSystemGroup : ManualComponentSystemGroup
{
}


[DisableAutoCreation]
[UpdateInGroup(typeof(HandleDamageSystemGroup))]
[AlwaysSynchronizeSystem]
public class DamageManager : JobComponentSystem
{
    NativeMultiHashMap<Entity,DamageEvent> DamageEventBuffer;
    private JobHandle DamageEventBufferWriterDeps;
    private JobHandle DamageEventBufferApplyDeps;

    public NativeMultiHashMap<Entity, DamageEvent>.ParallelWriter GetDamageBufferWriter(out JobHandle deps)
    {
        deps = DamageEventBufferWriterDeps;
        return DamageEventBuffer.AsParallelWriter();
    }

    public void AddDamageWriterDependency(in JobHandle deps)
    {
        DamageEventBufferApplyDeps = JobHandle.CombineDependencies(DamageEventBufferApplyDeps, deps);
    }

    protected override void OnCreate()
    {
        base.OnCreate();
        DamageEventBuffer = new NativeMultiHashMap<Entity,DamageEvent>(1024,Allocator.Persistent);
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();
        DamageEventBuffer.Dispose();
    }

    protected override JobHandle OnUpdate(JobHandle inputDeps)
    {
        // TODO (mogensh) fix this ugly time stuff
        var timeQuery = GetEntityQuery(ComponentType.ReadOnly<GlobalGameTime>());
        if (timeQuery.CalculateEntityCount() == 0)
            return default;
        var globalTime = timeQuery.GetSingleton<GlobalGameTime>();


        DamageEventBufferApplyDeps.Complete();

        for (int i = 0; i < DamageEventBuffer.Length; i++)
        {
//            m_DamageEventBuffers[i].Dispose();

//            GameDebug.Log("DAMAGE:");
        }



        var keys = DamageEventBuffer.GetKeyArray(Allocator.Temp);
        for (int nKey = 0; nKey < keys.Length; nKey++)
        {

            var key = keys[nKey];
            if (EntityManager.HasComponent<DamageEvent>(key))
            {
                var damageEvents = EntityManager.GetBuffer<DamageEvent>(key);
                var values = DamageEventBuffer.GetValueArray(Allocator.Temp);
                for (int nValue = 0; nValue < values.Length; nValue++)
                {
                    damageEvents.Add(values[nValue]);
//                    GameDebug.Log("Damage: Entity:" + key + " got " + values[nValue].Damage + " damage");
                }
                values.Dispose();
            }
            else
            {
//                GameDebug.Log("Damage ignored: Entity:" + key + " has no HealthStateData");
            }



//            GameDebug.Log("DAMAGE: ");
        }
        keys.Dispose();;





        DamageEventBuffer.Clear();

        DamageEventBufferWriterDeps = new JobHandle();
        return default;
    }
}
