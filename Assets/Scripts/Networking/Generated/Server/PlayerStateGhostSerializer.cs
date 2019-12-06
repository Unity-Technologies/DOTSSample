using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Collections;
using Unity.NetCode;
using Unity.Transforms;

public struct PlayerStateGhostSerializer : IGhostSerializer<PlayerStateSnapshotData>
{
    private ComponentType componentTypePlayerState;
    private ComponentType componentTypePlayerCharacterControlState;
    private ComponentType componentTypeLocalToWorld;
    private ComponentType componentTypeRotation;
    private ComponentType componentTypeTranslation;
    // FIXME: These disable safety since all serializers have an instance of the same type - causing aliasing. Should be fixed in a cleaner way
    [NativeDisableContainerSafetyRestriction][ReadOnly] private ArchetypeChunkComponentType<Player.State> ghostPlayerStateType;


    public int CalculateImportance(ArchetypeChunk chunk)
    {
        return 1;
    }

    public bool WantsPredictionDelta => true;

    public int SnapshotSize => UnsafeUtility.SizeOf<PlayerStateSnapshotData>();
    public void BeginSerialize(ComponentSystemBase system)
    {
        componentTypePlayerState = ComponentType.ReadWrite<Player.State>();
        componentTypePlayerCharacterControlState = ComponentType.ReadWrite<PlayerCharacterControl.State>();
        componentTypeLocalToWorld = ComponentType.ReadWrite<LocalToWorld>();
        componentTypeRotation = ComponentType.ReadWrite<Rotation>();
        componentTypeTranslation = ComponentType.ReadWrite<Translation>();
        ghostPlayerStateType = system.GetArchetypeChunkComponentType<Player.State>(true);
    }

    public bool CanSerialize(EntityArchetype arch)
    {
        var components = arch.GetComponentTypes();
        int matches = 0;
        for (int i = 0; i < components.Length; ++i)
        {
            if (components[i] == componentTypePlayerState)
                ++matches;
            if (components[i] == componentTypePlayerCharacterControlState)
                ++matches;
            if (components[i] == componentTypeLocalToWorld)
                ++matches;
            if (components[i] == componentTypeRotation)
                ++matches;
            if (components[i] == componentTypeTranslation)
                ++matches;
        }
        return (matches == 5);
    }

    public void CopyToSnapshot(ArchetypeChunk chunk, int ent, uint tick, ref PlayerStateSnapshotData snapshot, GhostSerializerState serializerState)
    {
        snapshot.tick = tick;
        var chunkDataPlayerState = chunk.GetNativeArray(ghostPlayerStateType);
        snapshot.SetPlayerStateplayerId(chunkDataPlayerState[ent].playerId, serializerState);
        snapshot.SetPlayerStateplayerName(chunkDataPlayerState[ent].playerName, serializerState);
        snapshot.SetPlayerStateteamIndex(chunkDataPlayerState[ent].teamIndex, serializerState);
        snapshot.SetPlayerStatescore(chunkDataPlayerState[ent].score, serializerState);
        snapshot.SetPlayerStategameModeSystemInitialized(chunkDataPlayerState[ent].gameModeSystemInitialized, serializerState);
        snapshot.SetPlayerStatedisplayCountDown(chunkDataPlayerState[ent].displayCountDown, serializerState);
        snapshot.SetPlayerStatecountDown(chunkDataPlayerState[ent].countDown, serializerState);
        snapshot.SetPlayerStatedisplayScoreBoard(chunkDataPlayerState[ent].displayScoreBoard, serializerState);
        snapshot.SetPlayerStatedisplayGameScore(chunkDataPlayerState[ent].displayGameScore, serializerState);
        snapshot.SetPlayerStatedisplayGameResult(chunkDataPlayerState[ent].displayGameResult, serializerState);
        snapshot.SetPlayerStategameResult(chunkDataPlayerState[ent].gameResult, serializerState);
        snapshot.SetPlayerStatedisplayGoal(chunkDataPlayerState[ent].displayGoal, serializerState);
        snapshot.SetPlayerStategoalPosition(chunkDataPlayerState[ent].goalPosition, serializerState);
        snapshot.SetPlayerStategoalDefendersColor(chunkDataPlayerState[ent].goalDefendersColor, serializerState);
        snapshot.SetPlayerStategoalAttackersColor(chunkDataPlayerState[ent].goalAttackersColor, serializerState);
        snapshot.SetPlayerStategoalAttackers(chunkDataPlayerState[ent].goalAttackers, serializerState);
        snapshot.SetPlayerStategoalDefenders(chunkDataPlayerState[ent].goalDefenders, serializerState);
        snapshot.SetPlayerStategoalString(chunkDataPlayerState[ent].goalString, serializerState);
        snapshot.SetPlayerStateactionString(chunkDataPlayerState[ent].actionString, serializerState);
        snapshot.SetPlayerStategoalCompletion(chunkDataPlayerState[ent].goalCompletion, serializerState);
    }
}
