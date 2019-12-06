using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Sample.Core;

[DisableAutoCreation]
[AlwaysSynchronizeSystem]
public class ResolvePlayerReference : JobComponentSystem
{
    EntityQuery Group;

    protected override void OnCreate()
    {
        base.OnCreate();
        Group = GetEntityQuery(typeof(Player.State));
    }

    public void SetLocalPlayer(Entity localPlayer)
    {
        m_LocalPlayer = localPlayer;
    }

    protected override JobHandle OnUpdate(JobHandle inputDeps)
    {
        inputDeps.Complete();

        if (m_LocalPlayer == Entity.Null)
            return default;

        var localPlayerState = EntityManager.GetComponentData<LocalPlayer>(m_LocalPlayer);

        // Find player with correct player id
        var playerStateArray = Group.ToComponentDataArray<Player.State>(Allocator.Persistent);
        var playerEntityArray = Group.ToEntityArray(Allocator.Persistent);
        for (var playerIndex = 0; playerIndex < playerStateArray.Length; playerIndex++)
        {
            if (playerStateArray[playerIndex].playerId == localPlayerState.playerId)
            {
                if(localPlayerState.playerEntity != Entity.Null)
                    EntityManager.RemoveComponent<PlayerModuleClient.LocalOwnedPlayer>(localPlayerState.playerEntity);


                localPlayerState.playerEntity = playerEntityArray[playerIndex];

                EntityManager.SetComponentData(m_LocalPlayer, localPlayerState);

                EntityManager.AddComponent<PlayerModuleClient.LocalOwnedPlayer>(localPlayerState.playerEntity);
                break;
            }
        }
        playerStateArray.Dispose();
        playerEntityArray.Dispose();

        return default;
    }

    Entity m_LocalPlayer;
}

[DisableAutoCreation]
[AlwaysSynchronizeSystem]
public class ResolvePlayerCharacterReference : JobComponentSystem
{
    List<Entity> characters = new List<Entity>();
    List<int> playerIds = new List<int>();

    protected override JobHandle OnUpdate(JobHandle inputDeps)
    {
        inputDeps.Complete();

        var charactersVar = characters;
        var playerIdsVar = playerIds;

        // Collect all characters and their player id's
        charactersVar.Clear();
        playerIds.Clear();
        Entities
            .WithoutBurst()
            .ForEach((Entity e, ref Player.OwnerPlayerId ownerPlayerIds) =>
        {
            charactersVar.Add(e);
            playerIdsVar.Add(ownerPlayerIds.Value);
        }).Run();

        // Update PlayerState ref to controlledEntity
        Entities
            .WithoutBurst()
            .ForEach((Entity e, ref Player.State playerState) =>
        {
            int idx = playerIdsVar.IndexOf(playerState.playerId);
            var old = playerState.controlledEntity;
            if (idx > -1)
                playerState.controlledEntity = charactersVar[idx];
            else
                playerState.controlledEntity = Entity.Null;

            if (old != playerState.controlledEntity)
            {
                GameDebug.Log("All char ids:" + string.Join(",", playerIdsVar));
                GameDebug.Log("PLayer " + e + " NEW CONTROLLED ENT:" + playerState.playerId + " : controlling : " + old + " -> " + playerState.controlledEntity);
            }
        }).Run();

        return default;
    }
}

// TODO (mogensh) rename this. Or can we get rid of it as it not only sets controlled entity on localPlayer?
[DisableAutoCreation]
[AlwaysSynchronizeSystem]
public class UpdateServerEntityComponent : JobComponentSystem
{
    protected override JobHandle OnUpdate(JobHandle inputDeps)
    {
        inputDeps.Complete();

        var playerStateFromEntity = GetComponentDataFromEntity<Player.State>(true);

        // Update local player ref to controlled entity
        Entities
            .ForEach((Entity e, ref LocalPlayer localPlayer) =>
        {
            if (localPlayer.playerEntity == Entity.Null)
                return;

            var playerState = playerStateFromEntity[localPlayer.playerEntity];

            if (playerState.controlledEntity != localPlayer.controlledEntity)
            {
                localPlayer.controlledEntity = playerState.controlledEntity;
            }

        }).Run();

        return default;
    }
}
