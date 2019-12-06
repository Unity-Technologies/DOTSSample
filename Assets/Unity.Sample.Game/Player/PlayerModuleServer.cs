using Unity.Collections;
using Unity.Entities;
using UnityEngine;

public class PlayerModuleServer
{
    public PlayerModuleServer(World gameWorld)
    {
        m_settings = Resources.Load<PlayerModuleSettings>("PlayerModuleSettings");

        m_world = gameWorld;
    }

    public void Shutdown()
    {
        Resources.UnloadAsset(m_settings);
    }

    public Entity CreatePlayerEntity(World world, int playerId, int teamIndex, string playerName, bool isReady)
    {
        var playerEntity = PrefabAssetManager.CreateEntity(m_world.EntityManager, m_settings.playerStatePrefab);

        var playerState = world.EntityManager.GetComponentData<Player.State>(playerEntity);
        playerState.playerId = playerId;
        playerState.playerName = new NativeString64(playerName);
        playerState.teamIndex = teamIndex;
        world.EntityManager.SetComponentData(playerEntity, playerState);

        return playerEntity;
    }

    public void CleanupPlayer(Entity player)
    {
        PrefabAssetManager.DestroyEntity(m_world.EntityManager, player);
    }

    readonly World m_world;
    readonly PlayerModuleSettings m_settings;
}
