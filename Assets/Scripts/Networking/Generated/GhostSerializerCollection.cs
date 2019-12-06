using System;
using Unity.Collections;
using Unity.Entities;
using Unity.Networking.Transport;
using Unity.NetCode;

public struct DotsSampleGhostSerializerCollection : IGhostSerializerCollection
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
    public static int FindGhostType<T>()
        where T : struct, ISnapshotData<T>
    {
        if (typeof(T) == typeof(PlayerStateSnapshotData))
            return 0;
        if (typeof(T) == typeof(Char_TerraformerSnapshotData))
            return 1;
        if (typeof(T) == typeof(Weapon_TerraformerSnapshotData))
            return 2;
        if (typeof(T) == typeof(GameModeSnapshotData))
            return 3;
        if (typeof(T) == typeof(TeleporterSnapshotData))
            return 4;
        return -1;
    }
    public int FindSerializer(EntityArchetype arch)
    {
        if (m_PlayerStateGhostSerializer.CanSerialize(arch))
            return 0;
        if (m_Char_TerraformerGhostSerializer.CanSerialize(arch))
            return 1;
        if (m_Weapon_TerraformerGhostSerializer.CanSerialize(arch))
            return 2;
        if (m_GameModeGhostSerializer.CanSerialize(arch))
            return 3;
        if (m_TeleporterGhostSerializer.CanSerialize(arch))
            return 4;
        throw new ArgumentException("Invalid serializer type");
    }

    public void BeginSerialize(ComponentSystemBase system)
    {
        m_PlayerStateGhostSerializer.BeginSerialize(system);
        m_Char_TerraformerGhostSerializer.BeginSerialize(system);
        m_Weapon_TerraformerGhostSerializer.BeginSerialize(system);
        m_GameModeGhostSerializer.BeginSerialize(system);
        m_TeleporterGhostSerializer.BeginSerialize(system);
    }

    public int CalculateImportance(int serializer, ArchetypeChunk chunk)
    {
        switch (serializer)
        {
            case 0:
                return m_PlayerStateGhostSerializer.CalculateImportance(chunk);
            case 1:
                return m_Char_TerraformerGhostSerializer.CalculateImportance(chunk);
            case 2:
                return m_Weapon_TerraformerGhostSerializer.CalculateImportance(chunk);
            case 3:
                return m_GameModeGhostSerializer.CalculateImportance(chunk);
            case 4:
                return m_TeleporterGhostSerializer.CalculateImportance(chunk);
        }

        throw new ArgumentException("Invalid serializer type");
    }

    public bool WantsPredictionDelta(int serializer)
    {
        switch (serializer)
        {
            case 0:
                return m_PlayerStateGhostSerializer.WantsPredictionDelta;
            case 1:
                return m_Char_TerraformerGhostSerializer.WantsPredictionDelta;
            case 2:
                return m_Weapon_TerraformerGhostSerializer.WantsPredictionDelta;
            case 3:
                return m_GameModeGhostSerializer.WantsPredictionDelta;
            case 4:
                return m_TeleporterGhostSerializer.WantsPredictionDelta;
        }

        throw new ArgumentException("Invalid serializer type");
    }

    public int GetSnapshotSize(int serializer)
    {
        switch (serializer)
        {
            case 0:
                return m_PlayerStateGhostSerializer.SnapshotSize;
            case 1:
                return m_Char_TerraformerGhostSerializer.SnapshotSize;
            case 2:
                return m_Weapon_TerraformerGhostSerializer.SnapshotSize;
            case 3:
                return m_GameModeGhostSerializer.SnapshotSize;
            case 4:
                return m_TeleporterGhostSerializer.SnapshotSize;
        }

        throw new ArgumentException("Invalid serializer type");
    }

    public int Serialize(SerializeData data)
    {
        switch (data.ghostType)
        {
            case 0:
            {
                return GhostSendSystem<DotsSampleGhostSerializerCollection>.InvokeSerialize<PlayerStateGhostSerializer, PlayerStateSnapshotData>(m_PlayerStateGhostSerializer, data);
            }
            case 1:
            {
                return GhostSendSystem<DotsSampleGhostSerializerCollection>.InvokeSerialize<Char_TerraformerGhostSerializer, Char_TerraformerSnapshotData>(m_Char_TerraformerGhostSerializer, data);
            }
            case 2:
            {
                return GhostSendSystem<DotsSampleGhostSerializerCollection>.InvokeSerialize<Weapon_TerraformerGhostSerializer, Weapon_TerraformerSnapshotData>(m_Weapon_TerraformerGhostSerializer, data);
            }
            case 3:
            {
                return GhostSendSystem<DotsSampleGhostSerializerCollection>.InvokeSerialize<GameModeGhostSerializer, GameModeSnapshotData>(m_GameModeGhostSerializer, data);
            }
            case 4:
            {
                return GhostSendSystem<DotsSampleGhostSerializerCollection>.InvokeSerialize<TeleporterGhostSerializer, TeleporterSnapshotData>(m_TeleporterGhostSerializer, data);
            }
            default:
                throw new ArgumentException("Invalid serializer type");
        }
    }
    private PlayerStateGhostSerializer m_PlayerStateGhostSerializer;
    private Char_TerraformerGhostSerializer m_Char_TerraformerGhostSerializer;
    private Weapon_TerraformerGhostSerializer m_Weapon_TerraformerGhostSerializer;
    private GameModeGhostSerializer m_GameModeGhostSerializer;
    private TeleporterGhostSerializer m_TeleporterGhostSerializer;
}

public struct EnableDotsSampleGhostSendSystemComponent : IComponentData
{}
public class DotsSampleGhostSendSystem : GhostSendSystem<DotsSampleGhostSerializerCollection>
{
    protected override void OnCreate()
    {
        base.OnCreate();
        RequireSingletonForUpdate<EnableDotsSampleGhostSendSystemComponent>();
    }
}
