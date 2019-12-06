using Unity.DataFlowGraph;
using Unity.Entities;
using Unity.NetCode;
using Unity.Sample.Core;


public static class AnimSource
{
    [ConfigVar(Name = "animsource.show.lifetime", DefaultValue = "0", Description = "Show animsource lifetime")]
    public static ConfigVar ShowLifetime;

    public struct HasValidRig : IComponentData { }

    public struct Data : IComponentData
    {
        public Entity animStateEntity;

        public NodeHandle outputNode;
        public NodeHandle inputNode;
        public OutputPortID outputPortID;
        public InputPortID inputPortID;
    }

    // Component put on animsources that are allowed to update state of animStateEntity
    public struct AllowWrite : IComponentData
    {
        public static AllowWrite Default => new AnimSource.AllowWrite {FirstUpdate = true};
        public bool FirstUpdate;
    }

    public static bool ShouldPredict(ComponentDataFromEntity<PredictedGhostComponent> predictedGhostComponentFromEntity, in Data animSource, uint predictTick)
    {
        if (!predictedGhostComponentFromEntity.HasComponent(animSource.animStateEntity))
            return false;
        var predictEntity = predictedGhostComponentFromEntity[animSource.animStateEntity];
        return GhostPredictionSystemGroup.ShouldPredict(predictTick, predictEntity);
    }

    public static void SetAnimStateEntityOnPrefab(EntityManager entityManager, Entity prefabEntity, Entity animStateEntity, EntityCommandBuffer cmdBuffer)
    {
        // Set AnimSource animStateEntity
        var linkedEntityBuffer = entityManager.GetBuffer<LinkedEntityGroup>(prefabEntity);
        for (int j = 0; j < linkedEntityBuffer.Length; j++)
        {
            var e = linkedEntityBuffer[j].Value;
            if (!entityManager.HasComponent<Data>(e))
                continue;

            var animSource = entityManager.GetComponentData<Data>(e);
            animSource.animStateEntity = animStateEntity;
            cmdBuffer.SetComponent(e, animSource);
        }
    }


    public static void SetAnimStateEntityOnPrefab(EntityManager entityManager, Entity prefabEntity, Entity animStateEntity)
    {
        // Set AnimSource animStateEntity
        var linkedEntityBuffer = entityManager.GetBuffer<LinkedEntityGroup>(prefabEntity);
        for (int j = 0; j < linkedEntityBuffer.Length; j++)
        {
            var e = linkedEntityBuffer[j].Value;
            if (!entityManager.HasComponent<Data>(e))
                continue;

            var animSource = entityManager.GetComponentData<Data>(e);
            animSource.animStateEntity = animStateEntity;
            entityManager.SetComponentData(e, animSource);
        }
    }
}

