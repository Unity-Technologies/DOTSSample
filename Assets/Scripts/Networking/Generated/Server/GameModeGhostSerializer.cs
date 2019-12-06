using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Collections;
using Unity.NetCode;

public struct GameModeGhostSerializer : IGhostSerializer<GameModeSnapshotData>
{
    private ComponentType componentTypeGameModeData;
    // FIXME: These disable safety since all serializers have an instance of the same type - causing aliasing. Should be fixed in a cleaner way
    [NativeDisableContainerSafetyRestriction][ReadOnly] private ArchetypeChunkComponentType<GameModeData> ghostGameModeDataType;


    public int CalculateImportance(ArchetypeChunk chunk)
    {
        return 1;
    }

    public bool WantsPredictionDelta => true;

    public int SnapshotSize => UnsafeUtility.SizeOf<GameModeSnapshotData>();
    public void BeginSerialize(ComponentSystemBase system)
    {
        componentTypeGameModeData = ComponentType.ReadWrite<GameModeData>();
        ghostGameModeDataType = system.GetArchetypeChunkComponentType<GameModeData>(true);
    }

    public bool CanSerialize(EntityArchetype arch)
    {
        var components = arch.GetComponentTypes();
        int matches = 0;
        for (int i = 0; i < components.Length; ++i)
        {
            if (components[i] == componentTypeGameModeData)
                ++matches;
        }
        return (matches == 1);
    }

    public void CopyToSnapshot(ArchetypeChunk chunk, int ent, uint tick, ref GameModeSnapshotData snapshot, GhostSerializerState serializerState)
    {
        snapshot.tick = tick;
        var chunkDataGameModeData = chunk.GetNativeArray(ghostGameModeDataType);
        snapshot.SetGameModeDatagameTimerSeconds(chunkDataGameModeData[ent].gameTimerSeconds, serializerState);
        snapshot.SetGameModeDatagameTimerMessage(chunkDataGameModeData[ent].gameTimerMessage, serializerState);
        snapshot.SetGameModeDatateamName0(chunkDataGameModeData[ent].teamName0, serializerState);
        snapshot.SetGameModeDatateamName1(chunkDataGameModeData[ent].teamName1, serializerState);
        snapshot.SetGameModeDatateamScore0(chunkDataGameModeData[ent].teamScore0, serializerState);
        snapshot.SetGameModeDatateamScore1(chunkDataGameModeData[ent].teamScore1, serializerState);
    }
}
