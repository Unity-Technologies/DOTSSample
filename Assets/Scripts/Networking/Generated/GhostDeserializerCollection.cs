using System;
using Unity.Collections;
using Unity.Entities;
using Unity.Networking.Transport;
using Unity.NetCode;

public struct DotsSampleGhostDeserializerCollection : IGhostDeserializerCollection
{
#if UNITY_EDITOR || DEVELOPMENT_BUILD
    public string[] CreateSerializerNameList()
    {
        var arr = new string[]
        {
            "PlayerStateGhostSerializer",
            "Char_TerraformerGhostSerializer",
            "Weapon_TerraformerGhostSerializer",
            "GameModeGhostSerializer",
            "TeleporterGhostSerializer",
        };
        return arr;
    }

    public int Length => 5;
#endif
    public void Initialize(World world)
    {
        var curPlayerStateGhostSpawnSystem = world.GetOrCreateSystem<PlayerStateGhostSpawnSystem>();
        m_PlayerStateSnapshotDataNewGhostIds = curPlayerStateGhostSpawnSystem.NewGhostIds;
        m_PlayerStateSnapshotDataNewGhosts = curPlayerStateGhostSpawnSystem.NewGhosts;
        curPlayerStateGhostSpawnSystem.GhostType = 0;
        var curChar_TerraformerGhostSpawnSystem = world.GetOrCreateSystem<Char_TerraformerGhostSpawnSystem>();
        m_Char_TerraformerSnapshotDataNewGhostIds = curChar_TerraformerGhostSpawnSystem.NewGhostIds;
        m_Char_TerraformerSnapshotDataNewGhosts = curChar_TerraformerGhostSpawnSystem.NewGhosts;
        curChar_TerraformerGhostSpawnSystem.GhostType = 1;
        var curWeapon_TerraformerGhostSpawnSystem = world.GetOrCreateSystem<Weapon_TerraformerGhostSpawnSystem>();
        m_Weapon_TerraformerSnapshotDataNewGhostIds = curWeapon_TerraformerGhostSpawnSystem.NewGhostIds;
        m_Weapon_TerraformerSnapshotDataNewGhosts = curWeapon_TerraformerGhostSpawnSystem.NewGhosts;
        curWeapon_TerraformerGhostSpawnSystem.GhostType = 2;
        var curGameModeGhostSpawnSystem = world.GetOrCreateSystem<GameModeGhostSpawnSystem>();
        m_GameModeSnapshotDataNewGhostIds = curGameModeGhostSpawnSystem.NewGhostIds;
        m_GameModeSnapshotDataNewGhosts = curGameModeGhostSpawnSystem.NewGhosts;
        curGameModeGhostSpawnSystem.GhostType = 3;
        var curTeleporterGhostSpawnSystem = world.GetOrCreateSystem<TeleporterGhostSpawnSystem>();
        m_TeleporterSnapshotDataNewGhostIds = curTeleporterGhostSpawnSystem.NewGhostIds;
        m_TeleporterSnapshotDataNewGhosts = curTeleporterGhostSpawnSystem.NewGhosts;
        curTeleporterGhostSpawnSystem.GhostType = 4;
    }

    public void BeginDeserialize(JobComponentSystem system)
    {
        m_PlayerStateSnapshotDataFromEntity = system.GetBufferFromEntity<PlayerStateSnapshotData>();
        m_Char_TerraformerSnapshotDataFromEntity = system.GetBufferFromEntity<Char_TerraformerSnapshotData>();
        m_Weapon_TerraformerSnapshotDataFromEntity = system.GetBufferFromEntity<Weapon_TerraformerSnapshotData>();
        m_GameModeSnapshotDataFromEntity = system.GetBufferFromEntity<GameModeSnapshotData>();
        m_TeleporterSnapshotDataFromEntity = system.GetBufferFromEntity<TeleporterSnapshotData>();
    }
    public bool Deserialize(int serializer, Entity entity, uint snapshot, uint baseline, uint baseline2, uint baseline3,
        DataStreamReader reader,
        ref DataStreamReader.Context ctx, NetworkCompressionModel compressionModel)
    {
        switch (serializer)
        {
            case 0:
                return GhostReceiveSystem<DotsSampleGhostDeserializerCollection>.InvokeDeserialize(m_PlayerStateSnapshotDataFromEntity, entity, snapshot, baseline, baseline2,
                baseline3, reader, ref ctx, compressionModel);
            case 1:
                return GhostReceiveSystem<DotsSampleGhostDeserializerCollection>.InvokeDeserialize(m_Char_TerraformerSnapshotDataFromEntity, entity, snapshot, baseline, baseline2,
                baseline3, reader, ref ctx, compressionModel);
            case 2:
                return GhostReceiveSystem<DotsSampleGhostDeserializerCollection>.InvokeDeserialize(m_Weapon_TerraformerSnapshotDataFromEntity, entity, snapshot, baseline, baseline2,
                baseline3, reader, ref ctx, compressionModel);
            case 3:
                return GhostReceiveSystem<DotsSampleGhostDeserializerCollection>.InvokeDeserialize(m_GameModeSnapshotDataFromEntity, entity, snapshot, baseline, baseline2,
                baseline3, reader, ref ctx, compressionModel);
            case 4:
                return GhostReceiveSystem<DotsSampleGhostDeserializerCollection>.InvokeDeserialize(m_TeleporterSnapshotDataFromEntity, entity, snapshot, baseline, baseline2,
                baseline3, reader, ref ctx, compressionModel);
            default:
                throw new ArgumentException("Invalid serializer type");
        }
    }
    public void Spawn(int serializer, int ghostId, uint snapshot, DataStreamReader reader,
        ref DataStreamReader.Context ctx, NetworkCompressionModel compressionModel)
    {
        switch (serializer)
        {
            case 0:
                m_PlayerStateSnapshotDataNewGhostIds.Add(ghostId);
                m_PlayerStateSnapshotDataNewGhosts.Add(GhostReceiveSystem<DotsSampleGhostDeserializerCollection>.InvokeSpawn<PlayerStateSnapshotData>(snapshot, reader, ref ctx, compressionModel));
                break;
            case 1:
                m_Char_TerraformerSnapshotDataNewGhostIds.Add(ghostId);
                m_Char_TerraformerSnapshotDataNewGhosts.Add(GhostReceiveSystem<DotsSampleGhostDeserializerCollection>.InvokeSpawn<Char_TerraformerSnapshotData>(snapshot, reader, ref ctx, compressionModel));
                break;
            case 2:
                m_Weapon_TerraformerSnapshotDataNewGhostIds.Add(ghostId);
                m_Weapon_TerraformerSnapshotDataNewGhosts.Add(GhostReceiveSystem<DotsSampleGhostDeserializerCollection>.InvokeSpawn<Weapon_TerraformerSnapshotData>(snapshot, reader, ref ctx, compressionModel));
                break;
            case 3:
                m_GameModeSnapshotDataNewGhostIds.Add(ghostId);
                m_GameModeSnapshotDataNewGhosts.Add(GhostReceiveSystem<DotsSampleGhostDeserializerCollection>.InvokeSpawn<GameModeSnapshotData>(snapshot, reader, ref ctx, compressionModel));
                break;
            case 4:
                m_TeleporterSnapshotDataNewGhostIds.Add(ghostId);
                m_TeleporterSnapshotDataNewGhosts.Add(GhostReceiveSystem<DotsSampleGhostDeserializerCollection>.InvokeSpawn<TeleporterSnapshotData>(snapshot, reader, ref ctx, compressionModel));
                break;
            default:
                throw new ArgumentException("Invalid serializer type");
        }
    }

    private BufferFromEntity<PlayerStateSnapshotData> m_PlayerStateSnapshotDataFromEntity;
    private NativeList<int> m_PlayerStateSnapshotDataNewGhostIds;
    private NativeList<PlayerStateSnapshotData> m_PlayerStateSnapshotDataNewGhosts;
    private BufferFromEntity<Char_TerraformerSnapshotData> m_Char_TerraformerSnapshotDataFromEntity;
    private NativeList<int> m_Char_TerraformerSnapshotDataNewGhostIds;
    private NativeList<Char_TerraformerSnapshotData> m_Char_TerraformerSnapshotDataNewGhosts;
    private BufferFromEntity<Weapon_TerraformerSnapshotData> m_Weapon_TerraformerSnapshotDataFromEntity;
    private NativeList<int> m_Weapon_TerraformerSnapshotDataNewGhostIds;
    private NativeList<Weapon_TerraformerSnapshotData> m_Weapon_TerraformerSnapshotDataNewGhosts;
    private BufferFromEntity<GameModeSnapshotData> m_GameModeSnapshotDataFromEntity;
    private NativeList<int> m_GameModeSnapshotDataNewGhostIds;
    private NativeList<GameModeSnapshotData> m_GameModeSnapshotDataNewGhosts;
    private BufferFromEntity<TeleporterSnapshotData> m_TeleporterSnapshotDataFromEntity;
    private NativeList<int> m_TeleporterSnapshotDataNewGhostIds;
    private NativeList<TeleporterSnapshotData> m_TeleporterSnapshotDataNewGhosts;
}
public struct EnableDotsSampleGhostReceiveSystemComponent : IComponentData
{}
public class DotsSampleGhostReceiveSystem : GhostReceiveSystem<DotsSampleGhostDeserializerCollection>
{
    protected override void OnCreate()
    {
        base.OnCreate();
        RequireSingletonForUpdate<EnableDotsSampleGhostReceiveSystemComponent>();
    }
}
