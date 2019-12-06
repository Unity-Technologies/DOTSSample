using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Collections;
using Unity.NetCode;
using Unity.Transforms;
using Unity.Physics;
using Unity.Rendering;

public struct TeleporterGhostSerializer : IGhostSerializer<TeleporterSnapshotData>
{
    private ComponentType componentTypeTeleporterClientDataClass;
    private ComponentType componentTypeTeleporterPresentationData;
    private ComponentType componentTypeTeleporterServer;
    private ComponentType componentTypeLocalToWorld;
    private ComponentType componentTypeRotation;
    private ComponentType componentTypeTranslation;
    private ComponentType componentTypeLinkedEntityGroup;
    // FIXME: These disable safety since all serializers have an instance of the same type - causing aliasing. Should be fixed in a cleaner way
    [NativeDisableContainerSafetyRestriction][ReadOnly] private ArchetypeChunkComponentType<TeleporterPresentationData> ghostTeleporterPresentationDataType;
    [NativeDisableContainerSafetyRestriction][ReadOnly] private ArchetypeChunkComponentType<Translation> ghostTranslationType;
    [NativeDisableContainerSafetyRestriction][ReadOnly] private ArchetypeChunkBufferType<LinkedEntityGroup> ghostLinkedEntityGroupType;
    [NativeDisableContainerSafetyRestriction][ReadOnly] private ComponentDataFromEntity<Translation> ghostChild0TranslationType;
    [NativeDisableContainerSafetyRestriction][ReadOnly] private ComponentDataFromEntity<Translation> ghostChild1TranslationType;


    public int CalculateImportance(ArchetypeChunk chunk)
    {
        return 1;
    }

    public bool WantsPredictionDelta => true;

    public int SnapshotSize => UnsafeUtility.SizeOf<TeleporterSnapshotData>();
    public void BeginSerialize(ComponentSystemBase system)
    {
        componentTypeTeleporterClientDataClass = ComponentType.ReadWrite<TeleporterClientDataClass>();
        componentTypeTeleporterPresentationData = ComponentType.ReadWrite<TeleporterPresentationData>();
        componentTypeTeleporterServer = ComponentType.ReadWrite<TeleporterServer>();
        componentTypeLocalToWorld = ComponentType.ReadWrite<LocalToWorld>();
        componentTypeRotation = ComponentType.ReadWrite<Rotation>();
        componentTypeTranslation = ComponentType.ReadWrite<Translation>();
        componentTypeLinkedEntityGroup = ComponentType.ReadWrite<LinkedEntityGroup>();
        ghostTeleporterPresentationDataType = system.GetArchetypeChunkComponentType<TeleporterPresentationData>(true);
        ghostTranslationType = system.GetArchetypeChunkComponentType<Translation>(true);
        ghostLinkedEntityGroupType = system.GetArchetypeChunkBufferType<LinkedEntityGroup>(true);
        ghostChild0TranslationType = system.GetComponentDataFromEntity<Translation>(true);
        ghostChild1TranslationType = system.GetComponentDataFromEntity<Translation>(true);
    }

    public bool CanSerialize(EntityArchetype arch)
    {
        var components = arch.GetComponentTypes();
        int matches = 0;
        for (int i = 0; i < components.Length; ++i)
        {
            if (components[i] == componentTypeTeleporterClientDataClass)
                ++matches;
            if (components[i] == componentTypeTeleporterPresentationData)
                ++matches;
            if (components[i] == componentTypeTeleporterServer)
                ++matches;
            if (components[i] == componentTypeLocalToWorld)
                ++matches;
            if (components[i] == componentTypeRotation)
                ++matches;
            if (components[i] == componentTypeTranslation)
                ++matches;
            if (components[i] == componentTypeLinkedEntityGroup)
                ++matches;
        }
        return (matches == 7);
    }

    public void CopyToSnapshot(ArchetypeChunk chunk, int ent, uint tick, ref TeleporterSnapshotData snapshot, GhostSerializerState serializerState)
    {
        snapshot.tick = tick;
        var chunkDataTeleporterPresentationData = chunk.GetNativeArray(ghostTeleporterPresentationDataType);
        var chunkDataTranslation = chunk.GetNativeArray(ghostTranslationType);
        var chunkDataLinkedEntityGroup = chunk.GetBufferAccessor(ghostLinkedEntityGroupType);
        snapshot.SetTeleporterPresentationDataeffectTick(chunkDataTeleporterPresentationData[ent].effectTick, serializerState);
        snapshot.SetTranslationValue(chunkDataTranslation[ent].Value, serializerState);
        snapshot.SetChild0TranslationValue(ghostChild0TranslationType[chunkDataLinkedEntityGroup[ent][1].Value].Value, serializerState);
        snapshot.SetChild1TranslationValue(ghostChild1TranslationType[chunkDataLinkedEntityGroup[ent][2].Value].Value, serializerState);
    }
}
