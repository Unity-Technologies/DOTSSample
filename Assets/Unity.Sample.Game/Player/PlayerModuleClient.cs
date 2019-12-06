using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;
using Unity.Entities;
using Unity.NetCode;
using Unity.Sample.Core;

public class PlayerModuleClient
{
    // Number of commands the client stores - also maximum number of predictive steps the client can take
    public const int commandClientBufferSize = 32;
    
    // Placed on player entities on client that are controlled locally
    public struct LocalOwnedPlayer : IComponentData
    {}

    public struct FakeLocalPlayer : IComponentData
    {}

    public PlayerModuleClient(World world)
    {
        m_world = world;

#pragma warning disable 618
        // we're keeping World.Active until we can properly remove them all
        var defaultWorld = World.Active;
        try
        {
            World.Active = m_world;
            m_settings = Resources.Load<PlayerModuleSettings>("PlayerModuleSettings");
            m_HandlePlayerCameraControlSpawn = m_world.CreateSystem<PlayerCameraControl.HandlePlayerCameraControlSpawn>();
            m_UpdatePlayerCameras = m_world.CreateSystem<PlayerCameraControl.UpdatePlayerCameras>();
            m_ResolvePlayerReference = m_world.CreateSystem<ResolvePlayerReference>();
            m_ResolvePlayerCharacterReference = m_world.CreateSystem<ResolvePlayerCharacterReference>();
            m_UpdateServerEntityComponent = m_world.CreateSystem<UpdateServerEntityComponent>();
        }
        finally
        {
            World.Active = defaultWorld;
        }
#pragma warning restore 618
    }

    public void Shutdown()
    {
        Resources.UnloadAsset(m_settings);

        if (World.AllWorlds.Contains(m_world))
        {
            m_world.DestroySystem(m_HandlePlayerCameraControlSpawn);
            m_world.DestroySystem(m_UpdatePlayerCameras);
            m_world.DestroySystem(m_ResolvePlayerCharacterReference);
            m_world.DestroySystem(m_ResolvePlayerReference);
            m_world.DestroySystem(m_UpdateServerEntityComponent);
        }

        if (m_LocalPlayer != Entity.Null)
            PrefabAssetManager.DestroyEntity(m_world.EntityManager, m_LocalPlayer);
    }

    public Entity RegisterLocalPlayer(int playerId)
    {
        m_LocalPlayer = PrefabAssetManager.CreateEntity(m_world.EntityManager,m_settings.localPlayerPrefab);

        var query = m_world.EntityManager.CreateEntityQuery(typeof(ThinClientComponent));
        if (query.CalculateEntityCount() == 1)
            m_world.EntityManager.AddComponent<FakeLocalPlayer>(m_LocalPlayer);
        var localPlayerState = m_world.EntityManager.GetComponentData<LocalPlayer>(m_LocalPlayer);

        localPlayerState.playerId = playerId;
        localPlayerState.command.lookPitch = 90;

        m_world.EntityManager.SetComponentData(m_LocalPlayer, localPlayerState);

        m_ResolvePlayerReference.SetLocalPlayer(m_LocalPlayer);
        m_world.EntityManager.GetBuffer<UserCommand>(m_LocalPlayer).Clear();
        // Find the correct thing
        var q = m_world.EntityManager.CreateEntityQuery(ComponentType.ReadWrite<CommandTargetComponent>(),
            ComponentType.ReadWrite<NetworkStreamConnection>(), ComponentType.ReadWrite<NetworkIdComponent>());
        var ents = q.ToEntityArray(Allocator.TempJob);
        var ids = q.ToComponentDataArray<NetworkIdComponent>(Allocator.TempJob);
        for (int i = 0; i < ents.Length; ++i)
        {
            if (ids[i].Value == playerId)
            {
                UnityEngine.Debug.Log("Assign command on client");
                m_world.EntityManager.SetComponentData(ents[i], new CommandTargetComponent{targetEntity = m_LocalPlayer});
            }
        }
        ents.Dispose();
        ids.Dispose();
        return m_LocalPlayer;
    }

    public void SampleInput(World world, bool userInputEnabled, float deltaTime, int renderTick)
    {
        SampleInput(world, m_LocalPlayer, userInputEnabled, deltaTime, renderTick);
    }

    public static void SampleInput(World world, Entity localPlayer, bool userInputEnabled, float deltaTime, int renderTick)
    {
        var localPlayerState = world.EntityManager.GetComponentData<LocalPlayer>(localPlayer);

        // Only sample input when cursor is locked to avoid affecting multiple clients running on same machine (TODO: find better handling of selected window)
        if (userInputEnabled)
            InputSystem.AccumulateInput(ref localPlayerState.command, deltaTime);
        else
            InputSystem.ClearInput(ref localPlayerState.command); // make sure no keys are stuck in 'down' when user input disabled

        if (m_debugMove.IntValue == 1)
        {
            localPlayerState.command.moveMagnitude = 1;
            localPlayerState.command.lookYaw += 70 * deltaTime;
        }

        if (m_debugMove.IntValue == 2 || world.EntityManager.HasComponent<FakeLocalPlayer>(localPlayer))
        {
            localPlayerState.m_debugMoveDuration += deltaTime;

            var fireDuration = 2.0f;
            var maxTurn = 70.0f;

            if (localPlayerState.m_debugMoveDuration > localPlayerState.m_debugMovePhaseDuration)
            {
                localPlayerState.m_debugMoveDuration = 0;
                localPlayerState.m_debugMovePhaseDuration = 4 + 2 * Random.value;
                localPlayerState.m_debugMoveTurnSpeed = maxTurn * 0.9f + Random.value * maxTurn * 0.1f;

                localPlayerState.m_debugMoveMag = Random.value > 0.5f ? 1.0f : 0.0f;
            }

            localPlayerState.command.moveMagnitude = localPlayerState.m_debugMoveMag;
            localPlayerState.command.lookYaw += localPlayerState.m_debugMoveTurnSpeed * deltaTime;
            localPlayerState.command.lookYaw = localPlayerState.command.lookYaw % 360;
            while (localPlayerState.command.lookYaw < 0.0f)
                localPlayerState.command.lookYaw += 360.0f;
            var firePrimary = localPlayerState.m_debugMoveDuration < fireDuration;
            //localPlayerState.command.buttons.Set(UserCommand.Button.PrimaryFire, firePrimary);
            //localPlayerState.command.buttons.Set(UserCommand.Button.SecondaryFire, !firePrimary);
            //localPlayerState.command.buttons.Set(UserCommand.Button.Jump, localPlayerState.m_debugMoveDuration < jumpDuration);
        }

        localPlayerState.command.renderTick = renderTick;

        world.EntityManager.SetComponentData(localPlayer, localPlayerState);
    }

    public void ResetInput(bool userInputEnabled)
    {
        var localPlayerState = m_world.EntityManager.GetComponentData<LocalPlayer>(m_LocalPlayer);

        // Clear keys and resample to make sure released keys gets detected.
        // Pass in 0 as deltaTime to make mouse input and view stick do nothing
        InputSystem.ClearInput(ref localPlayerState.command);

        if (userInputEnabled)
            InputSystem.AccumulateInput(ref localPlayerState.command, 0.0f);

        m_world.EntityManager.SetComponentData(m_LocalPlayer, localPlayerState);
    }

    public void HandleCommandReset()
    {
        var localPlayerState = m_world.EntityManager.GetComponentData<LocalPlayer>(m_LocalPlayer);

        if (localPlayerState.playerEntity == Entity.Null)
            return;
        var playerState = m_world.EntityManager.GetComponentData<Player.State>(localPlayerState.playerEntity);
        if (playerState.controlledEntity == Entity.Null)
            return;

        var controlledEntity = playerState.controlledEntity;
        var commandComponent = m_world.EntityManager.GetComponentData<PlayerControlled.State>(controlledEntity);
        if (commandComponent.resetCommandTick > commandComponent.lastResetCommandTick)
        {
            commandComponent.lastResetCommandTick = commandComponent.resetCommandTick;
            m_world.EntityManager.SetComponentData(controlledEntity, commandComponent);

            localPlayerState.command.lookYaw = commandComponent.resetCommandLookYaw;
            localPlayerState.command.lookPitch = commandComponent.resetCommandLookPitch;
        }

        m_world.EntityManager.SetComponentData(m_LocalPlayer, localPlayerState);
    }

    public void ResolveReferenceFromLocalPlayerToPlayer()
    {
        m_ResolvePlayerCharacterReference.Update();
        var localPlayerState = m_world.EntityManager.GetComponentData<LocalPlayer>(m_LocalPlayer);
        if (localPlayerState.playerEntity == Entity.Null)
            m_ResolvePlayerReference.Update();
    }

    public void HandleControlledEntityChanged()
    {
        m_UpdateServerEntityComponent.Update();
    }

    public void StoreCommand(int tick)
    {
        StoreCommand(m_world, m_LocalPlayer, tick);
    }

    public static void StoreCommand(World world, Entity localPlayer, int tick)
    {
        var localPlayerState = world.EntityManager.GetComponentData<LocalPlayer>(localPlayer);
        if (localPlayerState.playerEntity == Entity.Null && !world.EntityManager.HasComponent<FakeLocalPlayer>(localPlayer))
            return;

        var buf = world.EntityManager.GetBuffer<UserCommand>(localPlayer);

        localPlayerState.command.checkTick = tick;
        localPlayerState.command.tick = world.GetExistingSystem<ClientSimulationSystemGroup>().ServerTick;
        buf.AddCommandData(localPlayerState.command);
        world.EntityManager.SetComponentData(localPlayer, localPlayerState);
    }


    public void RetrieveCommand(uint tick)
    {
        var localPlayerState = m_world.EntityManager.GetComponentData<LocalPlayer>(m_LocalPlayer);

        GameDebug.Assert(localPlayerState.playerEntity != null, "No player state set");
        if (localPlayerState.controlledEntity == Entity.Null)
            return;

        if (!m_world.EntityManager.HasComponent<PlayerControlled.State>(localPlayerState.controlledEntity))
            return;

        var buf = m_world.EntityManager.GetBuffer<UserCommand>(m_LocalPlayer);

        var userCommand = m_world.EntityManager.GetComponentData<PlayerControlled.State>(localPlayerState.controlledEntity);

        // Normally we can expect commands to be present, but if client has done hardcatchup commands might not have been generated yet
        // so we just use the defaultCommand

        var command = UserCommand.defaultCommand;
        var found = buf.GetDataAtTick(tick, out command);
        //GameDebug.Assert(found, "Failed to find command for tick:{0}", tick);

        userCommand.prevCommand = userCommand.command;
        userCommand.command = command;

        m_world.EntityManager.SetComponentData(localPlayerState.controlledEntity, userCommand);
    }

    public void HandleSpawn()
    {
        m_HandlePlayerCameraControlSpawn.Update();
    }

    public void CameraUpdate()
    {
        m_UpdatePlayerCameras.Update();
    }

    readonly PlayerModuleSettings m_settings;

    readonly World m_world;

    Entity m_LocalPlayer;

    readonly PlayerCameraControl.HandlePlayerCameraControlSpawn m_HandlePlayerCameraControlSpawn;
    readonly PlayerCameraControl.UpdatePlayerCameras m_UpdatePlayerCameras;
    readonly ResolvePlayerReference m_ResolvePlayerReference;
    readonly ResolvePlayerCharacterReference m_ResolvePlayerCharacterReference;
    readonly UpdateServerEntityComponent m_UpdateServerEntityComponent;

#pragma warning disable 649
    [ConfigVar(Name = "debugmove", DefaultValue = "0", Description = "Should client perform debug movement")]
    static ConfigVar m_debugMove;
#pragma warning restore 649
    float m_debugMoveDuration;
    float m_debugMovePhaseDuration;
    float m_debugMoveTurnSpeed;
    float m_debugMoveMag;
}
