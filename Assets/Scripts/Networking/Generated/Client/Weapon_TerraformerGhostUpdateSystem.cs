using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
using Unity.Jobs.LowLevel.Unsafe;
using Unity.Networking.Transport.Utilities;
using Unity.NetCode;
using Unity.Entities;

[UpdateInGroup(typeof(GhostUpdateSystemGroup))]
public class Weapon_TerraformerGhostUpdateSystem : JobComponentSystem
{
    [BurstCompile]
    struct UpdateInterpolatedJob : IJobChunk
    {
        [ReadOnly] public NativeHashMap<int, GhostEntity> GhostMap;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        [NativeDisableContainerSafetyRestriction] public NativeArray<uint> minMaxSnapshotTick;
#pragma warning disable 649
        [NativeSetThreadIndex]
        public int ThreadIndex;
#pragma warning restore 649
#endif
        [ReadOnly] public ArchetypeChunkBufferType<Weapon_TerraformerSnapshotData> ghostSnapshotDataType;
        [ReadOnly] public ArchetypeChunkEntityType ghostEntityType;
        public ArchetypeChunkComponentType<Item.InputState> ghostItemInputStateType;
        [ReadOnly] public ArchetypeChunkBufferType<LinkedEntityGroup> ghostLinkedEntityGroupType;
        [NativeDisableParallelForRestriction] public ComponentDataFromEntity<Ability.AbilityAction> ghostAbilityAbilityActionFromEntity;
        [NativeDisableParallelForRestriction] public ComponentDataFromEntity<Ability.AbilityControl> ghostAbilityAbilityControlFromEntity;
        [NativeDisableParallelForRestriction] public ComponentDataFromEntity<AbilityAutoRifle.InterpolatedState> ghostAbilityAutoRifleInterpolatedStateFromEntity;
        [NativeDisableParallelForRestriction] public ComponentDataFromEntity<AbilityAutoRifle.PredictedState> ghostAbilityAutoRiflePredictedStateFromEntity;

        public uint targetTick;
        public float targetTickFraction;
        public void Execute(ArchetypeChunk chunk, int chunkIndex, int firstEntityIndex)
        {
            var deserializerState = new GhostDeserializerState
            {
                GhostMap = GhostMap
            };
            var ghostEntityArray = chunk.GetNativeArray(ghostEntityType);
            var ghostSnapshotDataArray = chunk.GetBufferAccessor(ghostSnapshotDataType);
            var ghostItemInputStateArray = chunk.GetNativeArray(ghostItemInputStateType);
            var ghostLinkedEntityGroupArray = chunk.GetBufferAccessor(ghostLinkedEntityGroupType);
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            var minMaxOffset = ThreadIndex * (JobsUtility.CacheLineSize/4);
#endif
            for (int entityIndex = 0; entityIndex < ghostEntityArray.Length; ++entityIndex)
            {
                var snapshot = ghostSnapshotDataArray[entityIndex];
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                var latestTick = snapshot.GetLatestTick();
                if (latestTick != 0)
                {
                    if (minMaxSnapshotTick[minMaxOffset] == 0 || SequenceHelpers.IsNewer(minMaxSnapshotTick[minMaxOffset], latestTick))
                        minMaxSnapshotTick[minMaxOffset] = latestTick;
                    if (minMaxSnapshotTick[minMaxOffset + 1] == 0 || SequenceHelpers.IsNewer(latestTick, minMaxSnapshotTick[minMaxOffset + 1]))
                        minMaxSnapshotTick[minMaxOffset + 1] = latestTick;
                }
#endif
                Weapon_TerraformerSnapshotData snapshotData;
                snapshot.GetDataAtTick(targetTick, targetTickFraction, out snapshotData);

                var ghostItemInputState = ghostItemInputStateArray[entityIndex];
                var ghostChild0AbilityAbilityAction = ghostAbilityAbilityActionFromEntity[ghostLinkedEntityGroupArray[entityIndex][1].Value];
                var ghostChild0AbilityAbilityControl = ghostAbilityAbilityControlFromEntity[ghostLinkedEntityGroupArray[entityIndex][1].Value];
                var ghostChild0AbilityAutoRifleInterpolatedState = ghostAbilityAutoRifleInterpolatedStateFromEntity[ghostLinkedEntityGroupArray[entityIndex][1].Value];
                var ghostChild0AbilityAutoRiflePredictedState = ghostAbilityAutoRiflePredictedStateFromEntity[ghostLinkedEntityGroupArray[entityIndex][1].Value];
                ghostItemInputState.owner = snapshotData.GetItemInputStateowner(deserializerState);
                ghostItemInputState.slot = snapshotData.GetItemInputStateslot(deserializerState);
                ghostItemInputState.playerId = snapshotData.GetItemInputStateplayerId(deserializerState);
                ghostChild0AbilityAbilityAction.action = snapshotData.GetChild0AbilityAbilityActionaction(deserializerState);
                ghostChild0AbilityAbilityAction.actionStartTick = snapshotData.GetChild0AbilityAbilityActionactionStartTick(deserializerState);
                ghostChild0AbilityAbilityControl.behaviorState = snapshotData.GetChild0AbilityAbilityControlbehaviorState(deserializerState);
                ghostChild0AbilityAbilityControl.requestDeactivate = snapshotData.GetChild0AbilityAbilityControlrequestDeactivate(deserializerState);
                ghostChild0AbilityAutoRifleInterpolatedState.fireTick = snapshotData.GetChild0AbilityAutoRifleInterpolatedStatefireTick(deserializerState);
                ghostChild0AbilityAutoRifleInterpolatedState.fireEndPos = snapshotData.GetChild0AbilityAutoRifleInterpolatedStatefireEndPos(deserializerState);
                ghostChild0AbilityAutoRifleInterpolatedState.impactType = snapshotData.GetChild0AbilityAutoRifleInterpolatedStateimpactType(deserializerState);
                ghostChild0AbilityAutoRifleInterpolatedState.impactNormal = snapshotData.GetChild0AbilityAutoRifleInterpolatedStateimpactNormal(deserializerState);
                ghostChild0AbilityAutoRiflePredictedState.action = snapshotData.GetChild0AbilityAutoRiflePredictedStateaction(deserializerState);
                ghostChild0AbilityAutoRiflePredictedState.phaseStartTick = snapshotData.GetChild0AbilityAutoRiflePredictedStatephaseStartTick(deserializerState);
                ghostChild0AbilityAutoRiflePredictedState.ammoInClip = snapshotData.GetChild0AbilityAutoRiflePredictedStateammoInClip(deserializerState);
                ghostChild0AbilityAutoRiflePredictedState.COF = snapshotData.GetChild0AbilityAutoRiflePredictedStateCOF(deserializerState);
                ghostAbilityAbilityActionFromEntity[ghostLinkedEntityGroupArray[entityIndex][1].Value] = ghostChild0AbilityAbilityAction;
                ghostAbilityAbilityControlFromEntity[ghostLinkedEntityGroupArray[entityIndex][1].Value] = ghostChild0AbilityAbilityControl;
                ghostAbilityAutoRifleInterpolatedStateFromEntity[ghostLinkedEntityGroupArray[entityIndex][1].Value] = ghostChild0AbilityAutoRifleInterpolatedState;
                ghostAbilityAutoRiflePredictedStateFromEntity[ghostLinkedEntityGroupArray[entityIndex][1].Value] = ghostChild0AbilityAutoRiflePredictedState;
                ghostItemInputStateArray[entityIndex] = ghostItemInputState;
            }
        }
    }
    [BurstCompile]
    struct UpdatePredictedJob : IJobChunk
    {
        [ReadOnly] public NativeHashMap<int, GhostEntity> GhostMap;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        [NativeDisableContainerSafetyRestriction] public NativeArray<uint> minMaxSnapshotTick;
#endif
#pragma warning disable 649
        [NativeSetThreadIndex]
        public int ThreadIndex;
#pragma warning restore 649
        [NativeDisableParallelForRestriction] public NativeArray<uint> minPredictedTick;
        [ReadOnly] public ArchetypeChunkBufferType<Weapon_TerraformerSnapshotData> ghostSnapshotDataType;
        [ReadOnly] public ArchetypeChunkEntityType ghostEntityType;
        public ArchetypeChunkComponentType<PredictedGhostComponent> predictedGhostComponentType;
        public ArchetypeChunkComponentType<Item.InputState> ghostItemInputStateType;
        [ReadOnly] public ArchetypeChunkBufferType<LinkedEntityGroup> ghostLinkedEntityGroupType;
        [NativeDisableParallelForRestriction] public ComponentDataFromEntity<Ability.AbilityAction> ghostAbilityAbilityActionFromEntity;
        [NativeDisableParallelForRestriction] public ComponentDataFromEntity<Ability.AbilityControl> ghostAbilityAbilityControlFromEntity;
        [NativeDisableParallelForRestriction] public ComponentDataFromEntity<AbilityAutoRifle.InterpolatedState> ghostAbilityAutoRifleInterpolatedStateFromEntity;
        [NativeDisableParallelForRestriction] public ComponentDataFromEntity<AbilityAutoRifle.PredictedState> ghostAbilityAutoRiflePredictedStateFromEntity;
        public uint targetTick;
        public uint lastPredictedTick;
        public void Execute(ArchetypeChunk chunk, int chunkIndex, int firstEntityIndex)
        {
            var deserializerState = new GhostDeserializerState
            {
                GhostMap = GhostMap
            };
            var ghostEntityArray = chunk.GetNativeArray(ghostEntityType);
            var ghostSnapshotDataArray = chunk.GetBufferAccessor(ghostSnapshotDataType);
            var predictedGhostComponentArray = chunk.GetNativeArray(predictedGhostComponentType);
            var ghostItemInputStateArray = chunk.GetNativeArray(ghostItemInputStateType);
            var ghostLinkedEntityGroupArray = chunk.GetBufferAccessor(ghostLinkedEntityGroupType);
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            var minMaxOffset = ThreadIndex * (JobsUtility.CacheLineSize/4);
#endif
            for (int entityIndex = 0; entityIndex < ghostEntityArray.Length; ++entityIndex)
            {
                var snapshot = ghostSnapshotDataArray[entityIndex];
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                var latestTick = snapshot.GetLatestTick();
                if (latestTick != 0)
                {
                    if (minMaxSnapshotTick[minMaxOffset] == 0 || SequenceHelpers.IsNewer(minMaxSnapshotTick[minMaxOffset], latestTick))
                        minMaxSnapshotTick[minMaxOffset] = latestTick;
                    if (minMaxSnapshotTick[minMaxOffset + 1] == 0 || SequenceHelpers.IsNewer(latestTick, minMaxSnapshotTick[minMaxOffset + 1]))
                        minMaxSnapshotTick[minMaxOffset + 1] = latestTick;
                }
#endif
                Weapon_TerraformerSnapshotData snapshotData;
                snapshot.GetDataAtTick(targetTick, out snapshotData);

                var predictedData = predictedGhostComponentArray[entityIndex];
                var lastPredictedTickInst = lastPredictedTick;
                if (lastPredictedTickInst == 0 || predictedData.AppliedTick != snapshotData.Tick)
                    lastPredictedTickInst = snapshotData.Tick;
                else if (!SequenceHelpers.IsNewer(lastPredictedTickInst, snapshotData.Tick))
                    lastPredictedTickInst = snapshotData.Tick;
                if (minPredictedTick[ThreadIndex] == 0 || SequenceHelpers.IsNewer(minPredictedTick[ThreadIndex], lastPredictedTickInst))
                    minPredictedTick[ThreadIndex] = lastPredictedTickInst;
                predictedGhostComponentArray[entityIndex] = new PredictedGhostComponent{AppliedTick = snapshotData.Tick, PredictionStartTick = lastPredictedTickInst};
                if (lastPredictedTickInst != snapshotData.Tick)
                    continue;

                var ghostItemInputState = ghostItemInputStateArray[entityIndex];
                var ghostChild0AbilityAbilityAction = ghostAbilityAbilityActionFromEntity[ghostLinkedEntityGroupArray[entityIndex][1].Value];
                var ghostChild0AbilityAbilityControl = ghostAbilityAbilityControlFromEntity[ghostLinkedEntityGroupArray[entityIndex][1].Value];
                var ghostChild0AbilityAutoRifleInterpolatedState = ghostAbilityAutoRifleInterpolatedStateFromEntity[ghostLinkedEntityGroupArray[entityIndex][1].Value];
                var ghostChild0AbilityAutoRiflePredictedState = ghostAbilityAutoRiflePredictedStateFromEntity[ghostLinkedEntityGroupArray[entityIndex][1].Value];
                ghostItemInputState.owner = snapshotData.GetItemInputStateowner(deserializerState);
                ghostItemInputState.slot = snapshotData.GetItemInputStateslot(deserializerState);
                ghostItemInputState.playerId = snapshotData.GetItemInputStateplayerId(deserializerState);
                ghostChild0AbilityAbilityAction.action = snapshotData.GetChild0AbilityAbilityActionaction(deserializerState);
                ghostChild0AbilityAbilityAction.actionStartTick = snapshotData.GetChild0AbilityAbilityActionactionStartTick(deserializerState);
                ghostChild0AbilityAbilityControl.behaviorState = snapshotData.GetChild0AbilityAbilityControlbehaviorState(deserializerState);
                ghostChild0AbilityAbilityControl.requestDeactivate = snapshotData.GetChild0AbilityAbilityControlrequestDeactivate(deserializerState);
                ghostChild0AbilityAutoRifleInterpolatedState.fireTick = snapshotData.GetChild0AbilityAutoRifleInterpolatedStatefireTick(deserializerState);
                ghostChild0AbilityAutoRifleInterpolatedState.fireEndPos = snapshotData.GetChild0AbilityAutoRifleInterpolatedStatefireEndPos(deserializerState);
                ghostChild0AbilityAutoRifleInterpolatedState.impactType = snapshotData.GetChild0AbilityAutoRifleInterpolatedStateimpactType(deserializerState);
                ghostChild0AbilityAutoRifleInterpolatedState.impactNormal = snapshotData.GetChild0AbilityAutoRifleInterpolatedStateimpactNormal(deserializerState);
                ghostChild0AbilityAutoRiflePredictedState.action = snapshotData.GetChild0AbilityAutoRiflePredictedStateaction(deserializerState);
                ghostChild0AbilityAutoRiflePredictedState.phaseStartTick = snapshotData.GetChild0AbilityAutoRiflePredictedStatephaseStartTick(deserializerState);
                ghostChild0AbilityAutoRiflePredictedState.ammoInClip = snapshotData.GetChild0AbilityAutoRiflePredictedStateammoInClip(deserializerState);
                ghostChild0AbilityAutoRiflePredictedState.COF = snapshotData.GetChild0AbilityAutoRiflePredictedStateCOF(deserializerState);
                ghostAbilityAbilityActionFromEntity[ghostLinkedEntityGroupArray[entityIndex][1].Value] = ghostChild0AbilityAbilityAction;
                ghostAbilityAbilityControlFromEntity[ghostLinkedEntityGroupArray[entityIndex][1].Value] = ghostChild0AbilityAbilityControl;
                ghostAbilityAutoRifleInterpolatedStateFromEntity[ghostLinkedEntityGroupArray[entityIndex][1].Value] = ghostChild0AbilityAutoRifleInterpolatedState;
                ghostAbilityAutoRiflePredictedStateFromEntity[ghostLinkedEntityGroupArray[entityIndex][1].Value] = ghostChild0AbilityAutoRiflePredictedState;
                ghostItemInputStateArray[entityIndex] = ghostItemInputState;
            }
        }
    }
    private ClientSimulationSystemGroup m_ClientSimulationSystemGroup;
    private GhostPredictionSystemGroup m_GhostPredictionSystemGroup;
    private EntityQuery m_interpolatedQuery;
    private EntityQuery m_predictedQuery;
    private NativeHashMap<int, GhostEntity> m_ghostEntityMap;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
    private NativeArray<uint> m_ghostMinMaxSnapshotTick;
#endif
    private GhostUpdateSystemGroup m_GhostUpdateSystemGroup;
    private uint m_LastPredictedTick;
    protected override void OnCreate()
    {
        m_GhostUpdateSystemGroup = World.GetOrCreateSystem<GhostUpdateSystemGroup>();
        m_ghostEntityMap = m_GhostUpdateSystemGroup.GhostEntityMap;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        m_ghostMinMaxSnapshotTick = m_GhostUpdateSystemGroup.GhostSnapshotTickMinMax;
#endif
        m_ClientSimulationSystemGroup = World.GetOrCreateSystem<ClientSimulationSystemGroup>();
        m_GhostPredictionSystemGroup = World.GetOrCreateSystem<GhostPredictionSystemGroup>();
        m_interpolatedQuery = GetEntityQuery(new EntityQueryDesc
        {
            All = new []{
                ComponentType.ReadWrite<Weapon_TerraformerSnapshotData>(),
                ComponentType.ReadOnly<GhostComponent>(),
                ComponentType.ReadWrite<Item.InputState>(),
                ComponentType.ReadOnly<LinkedEntityGroup>(),
            },
            None = new []{ComponentType.ReadWrite<PredictedGhostComponent>()}
        });
        m_predictedQuery = GetEntityQuery(new EntityQueryDesc
        {
            All = new []{
                ComponentType.ReadOnly<Weapon_TerraformerSnapshotData>(),
                ComponentType.ReadOnly<GhostComponent>(),
                ComponentType.ReadOnly<PredictedGhostComponent>(),
                ComponentType.ReadWrite<Item.InputState>(),
                ComponentType.ReadOnly<LinkedEntityGroup>(),
            }
        });
        RequireForUpdate(GetEntityQuery(ComponentType.ReadWrite<Weapon_TerraformerSnapshotData>(),
            ComponentType.ReadOnly<GhostComponent>()));
    }
    protected override JobHandle OnUpdate(JobHandle inputDeps)
    {
        if (!m_predictedQuery.IsEmptyIgnoreFilter)
        {
            var updatePredictedJob = new UpdatePredictedJob
            {
                GhostMap = m_ghostEntityMap,
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                minMaxSnapshotTick = m_ghostMinMaxSnapshotTick,
#endif
                minPredictedTick = m_GhostPredictionSystemGroup.OldestPredictedTick,
                ghostSnapshotDataType = GetArchetypeChunkBufferType<Weapon_TerraformerSnapshotData>(true),
                ghostEntityType = GetArchetypeChunkEntityType(),
                predictedGhostComponentType = GetArchetypeChunkComponentType<PredictedGhostComponent>(),
                ghostItemInputStateType = GetArchetypeChunkComponentType<Item.InputState>(),
                ghostLinkedEntityGroupType = GetArchetypeChunkBufferType<LinkedEntityGroup>(true),
                ghostAbilityAbilityActionFromEntity = GetComponentDataFromEntity<Ability.AbilityAction>(),
                ghostAbilityAbilityControlFromEntity = GetComponentDataFromEntity<Ability.AbilityControl>(),
                ghostAbilityAutoRifleInterpolatedStateFromEntity = GetComponentDataFromEntity<AbilityAutoRifle.InterpolatedState>(),
                ghostAbilityAutoRiflePredictedStateFromEntity = GetComponentDataFromEntity<AbilityAutoRifle.PredictedState>(),

                targetTick = m_ClientSimulationSystemGroup.ServerTick,
                lastPredictedTick = m_LastPredictedTick
            };
            m_LastPredictedTick = m_ClientSimulationSystemGroup.ServerTick;
            if (m_ClientSimulationSystemGroup.ServerTickFraction < 1)
                m_LastPredictedTick = 0;
            inputDeps = updatePredictedJob.Schedule(m_predictedQuery, JobHandle.CombineDependencies(inputDeps, m_GhostUpdateSystemGroup.LastGhostMapWriter));
            m_GhostPredictionSystemGroup.AddPredictedTickWriter(inputDeps);
        }
        if (!m_interpolatedQuery.IsEmptyIgnoreFilter)
        {
            var updateInterpolatedJob = new UpdateInterpolatedJob
            {
                GhostMap = m_ghostEntityMap,
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                minMaxSnapshotTick = m_ghostMinMaxSnapshotTick,
#endif
                ghostSnapshotDataType = GetArchetypeChunkBufferType<Weapon_TerraformerSnapshotData>(true),
                ghostEntityType = GetArchetypeChunkEntityType(),
                ghostItemInputStateType = GetArchetypeChunkComponentType<Item.InputState>(),
                ghostLinkedEntityGroupType = GetArchetypeChunkBufferType<LinkedEntityGroup>(true),
                ghostAbilityAbilityActionFromEntity = GetComponentDataFromEntity<Ability.AbilityAction>(),
                ghostAbilityAbilityControlFromEntity = GetComponentDataFromEntity<Ability.AbilityControl>(),
                ghostAbilityAutoRifleInterpolatedStateFromEntity = GetComponentDataFromEntity<AbilityAutoRifle.InterpolatedState>(),
                ghostAbilityAutoRiflePredictedStateFromEntity = GetComponentDataFromEntity<AbilityAutoRifle.PredictedState>(),
                targetTick = m_ClientSimulationSystemGroup.InterpolationTick,
                targetTickFraction = m_ClientSimulationSystemGroup.InterpolationTickFraction
            };
            inputDeps = updateInterpolatedJob.Schedule(m_interpolatedQuery, JobHandle.CombineDependencies(inputDeps, m_GhostUpdateSystemGroup.LastGhostMapWriter));
        }
        return inputDeps;
    }
}
public partial class Weapon_TerraformerGhostSpawnSystem : DefaultGhostSpawnSystem<Weapon_TerraformerSnapshotData>
{
    struct SetPredictedDefault : IJobParallelFor
    {
        [ReadOnly] public NativeArray<Weapon_TerraformerSnapshotData> snapshots;
        public NativeArray<int> predictionMask;
        [ReadOnly][DeallocateOnJobCompletion] public NativeArray<NetworkIdComponent> localPlayerId;
        public void Execute(int index)
        {
            if (localPlayerId.Length == 1 && snapshots[index].GetItemInputStateplayerId() == localPlayerId[0].Value)
                predictionMask[index] = 1;
        }
    }
    protected override JobHandle SetPredictedGhostDefaults(NativeArray<Weapon_TerraformerSnapshotData> snapshots, NativeArray<int> predictionMask, JobHandle inputDeps)
    {
        JobHandle playerHandle;
        var job = new SetPredictedDefault
        {
            snapshots = snapshots,
            predictionMask = predictionMask,
            localPlayerId = m_PlayerGroup.ToComponentDataArray<NetworkIdComponent>(Allocator.TempJob, out playerHandle),
        };
        return job.Schedule(predictionMask.Length, 8, JobHandle.CombineDependencies(playerHandle, inputDeps));
    }
}
