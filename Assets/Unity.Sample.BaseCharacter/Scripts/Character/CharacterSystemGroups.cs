using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using Unity.NetCode;
using Unity.Sample.Core;
using UnityEngine;
using UnityEngine.Profiling;
using ComponentSystemGroup = Unity.Entities.ComponentSystemGroup;
using ComponentType = Unity.Entities.ComponentType;


[DisableAutoCreation]
[UpdateBefore(typeof(PartSystemUpdateGroup))]    // TODO (mogensh) how to avoid having references to part system from character ?
public class CharacterPresentationSystemGroup : ComponentSystemGroup
{}

[DisableAutoCreation]
public class CharacterHandleControlledEntityChangedSystemGroup : ComponentSystemGroup
{}

[DisableAutoCreation]
[UpdateBefore(typeof(AbilityUpdateSystemGroup))]
public class CharacterUpdateSystemGroup : ManualComponentSystemGroup
{
    protected override void OnUpdate()
    {
        // TODO (mogensh) put this check into its own system. Can we enable/disable systems depending on configvars ?
        if (CharacterModule.PredictionCheck.IntValue > 0)
        {
            var timeQuery = EntityManager.CreateEntityQuery(ComponentType.ReadOnly<GlobalGameTime>());
            var time = timeQuery.GetSingleton<GlobalGameTime>().gameTime;

            var query = EntityManager.CreateEntityQuery(typeof(Character.PredictedData), typeof(PredictedGhostComponent));
            var predictedDataArray = query.ToComponentDataArray<Character.PredictedData>(Allocator.TempJob);
            var entityArray = query.ToEntityArray(Allocator.TempJob);
            for (int i = 0; i < predictedDataArray.Length; i++)
            {
                if (!GhostPredictionSystemGroup.ShouldPredict(World.GetExistingSystem<GhostPredictionSystemGroup>().PredictingTick, EntityManager.GetComponentData<PredictedGhostComponent>(entityArray[i])))
                    continue;
                var predictedData = predictedDataArray[i];

                if (predictedData.tick > 0 && time.tick != predictedData.tick + 1)
                    GameDebug.Log("Update tick invalid. Game tick:" + time.tick + " but current state is at tick:" + predictedData.tick);

                predictedData.tick = time.tick;
                EntityManager.SetComponentData(entityArray[i],predictedData);
            }

            entityArray.Dispose();
            predictedDataArray.Dispose();
            timeQuery.Dispose();
            query.Dispose();
        }

        base.OnUpdate();
    }
}

