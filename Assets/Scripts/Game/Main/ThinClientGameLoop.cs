using System.Collections.Generic;
using UnityEngine;
using Unity.Entities;
using UnityEngine.Profiling;
using System;
using Unity.Collections;
using Unity.Sample.Core;

public class NullSnapshotConsumer : ISnapshotConsumer
{
    public void ProcessEntityDespawns(int serverTime, List<int> despawns)
    {
    }

    public void ProcessEntitySpawn(int serverTime, int id, ushort typeId)
    {
    }

    public void ProcessEntityUpdate(int serverTime, int id, ref NetworkReader reader)
    {
    }
}

public class ThinClientGameWorld
{
    public bool PredictionEnabled = true;

    public float frameTimeScale = 1.0f;


    public GameTime PredictedTime
    {
        get { return m_PredictedTime; }
    }

    public GameTime RenderTime
    {
        get { return m_RenderTime; }
    }

    public ThinClientGameWorld(World world, NetworkClient networkClient, NetworkStatisticsClient networkStatistics)
    {
        m_NetworkClient = networkClient;
        m_NetworkStatistics = networkStatistics;

        m_NullSnapshotConsumer = new NullSnapshotConsumer();

        m_GameWorld = world;
    }

    public void Shutdown()
    {
    }

    // This is called at the actual client frame rate, so may be faster or slower than tickrate.
    public void Update(float frameDuration)
    {
        // Advances time and accumulate input into the UserCommand being generated
        HandleTime(frameDuration);

        var gameTimeSystem = m_GameWorld.GetExistingSystem<GameTimeSystem>();

        gameTimeSystem.SetWorldTime(m_RenderTime);
        gameTimeSystem.frameDuration = frameDuration;

        // Prediction
        gameTimeSystem.SetWorldTime(m_PredictedTime);

        // Update Presentation
        gameTimeSystem.SetWorldTime(m_PredictedTime);

        gameTimeSystem.SetWorldTime(m_RenderTime);

#if UNITY_EDITOR

        var localPlayerState = m_GameWorld.EntityManager.GetComponentData<LocalPlayer>(m_localPlayer);

        if (m_GameWorld.EntityManager.Exists(localPlayerState.controlledEntity) &&
            m_GameWorld.EntityManager.HasComponent<PlayerControlled.State>(localPlayerState.controlledEntity))
        {
            //var userCommand = m_GameWorld.GetEntityManager().GetComponentData<PlayerControlled.UserCommandComponentData>(m_localPlayer.controlledEntity);
            //m_ReplicatedEntityModule.FinalizedStateHistory(m_PredictedTime.tick-1, m_NetworkClient.serverTime, ref userCommand.command);
        }
#endif
    }

    public void LateUpdate(ChatSystemClient chatSystem, float frameDuration)
    {
    }

    public Entity RegisterLocalPlayer(int playerId)
    {

        // Create player state
        var settings = Resources.Load<PlayerModuleSettings>("PlayerModuleSettings");
        var playerEntity = PrefabAssetManager.CreateEntity(m_GameWorld.EntityManager, settings.playerStatePrefab);
        var playerState = m_GameWorld.EntityManager.GetComponentData<Player.State>(playerEntity);
        playerState.playerId = playerId;
        playerState.playerName = new NativeString64("asdf");
        m_GameWorld.EntityManager.SetComponentData(playerEntity,playerState);


        // Create local player
        m_localPlayer = PrefabAssetManager.CreateEntity(m_GameWorld.EntityManager, settings.localPlayerPrefab);
        var localPlayerState = m_GameWorld.EntityManager.GetComponentData<LocalPlayer>(m_localPlayer);

        localPlayerState.playerId = playerId;
        localPlayerState.command.lookPitch = 90;
        localPlayerState.playerEntity = playerEntity;

        m_GameWorld.EntityManager.SetComponentData(m_localPlayer, localPlayerState);

        return m_localPlayer;
    }

    public ISnapshotConsumer GetSnapshotConsumer()
    {
        return m_NullSnapshotConsumer;
    }

    void HandleTime(float frameDuration)
    {
        // Update tick rate (this will only change runtime in test scenarios)
        // TODO (petera) consider use ConfigVars with Server flag for this
        if (m_NetworkClient.serverTickRate != m_PredictedTime.tickRate)
        {
            m_PredictedTime.tickRate = m_NetworkClient.serverTickRate;
            m_RenderTime.tickRate = m_NetworkClient.serverTickRate;
        }

        // Sample input into current command
        //  The time passed in here is used to calculate the amount of rotation from stick position
        //  The command stores final view direction
        bool userInputEnabled = false;
        PlayerModuleClient.SampleInput(m_GameWorld, m_localPlayer, userInputEnabled, Time.deltaTime, m_RenderTime.tick);

        int prevTick = m_PredictedTime.tick;

        // Increment time
        var deltaPredictedTime = frameDuration * frameTimeScale;
        m_PredictedTime.AddDuration(deltaPredictedTime);

        var gameTimeSystem = m_GameWorld.GetExistingSystem<GameTimeSystem>();

        // Adjust time to be synchronized with server
        int preferredBufferedCommandCount = 2;
        int preferredTick = m_NetworkClient.serverTime + (int)(((m_NetworkClient.timeSinceSnapshot + m_NetworkStatistics.rtt.average) / 1000.0f) * gameTimeSystem.GetWorldTime().tickRate) + preferredBufferedCommandCount;

        bool resetTime = false;
        if (!resetTime && m_PredictedTime.tick < preferredTick - 3)
        {
            GameDebug.Log(string.Format("Client hard catchup ... "));
            resetTime = true;
        }

        if (!resetTime && m_PredictedTime.tick > preferredTick + 6)
        {
            GameDebug.Log(string.Format("Client hard slowdown ... "));
            resetTime = true;
        }

        frameTimeScale = 1.0f;
        if (resetTime)
        {
            GameDebug.Log(string.Format("CATCHUP ({0} -> {1})", m_PredictedTime.tick, preferredTick));

            m_NetworkStatistics.notifyHardCatchup = true;
            m_PredictedTime.tick = preferredTick;
            m_PredictedTime.SetTime(preferredTick, 0);
        }
        else
        {
            int bufferedCommands = m_NetworkClient.lastAcknowlegdedCommandTime - m_NetworkClient.serverTime;
            if (bufferedCommands < preferredBufferedCommandCount)
                frameTimeScale = 1.01f;

            if (bufferedCommands > preferredBufferedCommandCount)
                frameTimeScale = 0.99f;
        }

        // Increment interpolation time
        m_RenderTime.AddDuration(frameDuration * frameTimeScale);

        // Force interp time to not exeede server time
        if (m_RenderTime.tick >= m_NetworkClient.serverTime)
        {
            m_RenderTime.SetTime(m_NetworkClient.serverTime, 0);
        }

        // hard catchup
        if (m_RenderTime.tick < m_NetworkClient.serverTime - 10)
        {
            m_RenderTime.SetTime(m_NetworkClient.serverTime - 8, 0);
        }

        // Throttle up to catch up
        if (m_RenderTime.tick < m_NetworkClient.serverTime - 1)
        {
            m_RenderTime.AddDuration(frameDuration * 0.01f);
        }

        // If predicted time has entered a new tick the stored commands should be sent to server
        if (m_PredictedTime.tick > prevTick)
        {
            var oldestCommandToSend = Mathf.Max(prevTick, m_PredictedTime.tick - PlayerModuleClient.commandClientBufferSize);
            for (int tick = oldestCommandToSend; tick < m_PredictedTime.tick; tick++)
            {
                PlayerModuleClient.StoreCommand(m_GameWorld, m_localPlayer, tick);
            }

            //m_PlayerModule.ResetInput(userInputEnabled);
            PlayerModuleClient.StoreCommand(m_GameWorld, m_localPlayer, m_PredictedTime.tick);
        }

        // Store command
        PlayerModuleClient.StoreCommand(m_GameWorld, m_localPlayer, m_PredictedTime.tick);
    }

    World m_GameWorld;
    GameTime m_PredictedTime = new GameTime(60);
    GameTime m_RenderTime = new GameTime(60);

    // External systems
    NetworkClient m_NetworkClient;
    NetworkStatisticsClient m_NetworkStatistics;
    ClientFrontendUpdate m_ClientFrontendUpdate;


    //readonly UpdateNamePlates m_UpdateNamePlates;
    //readonly SpinSystem m_SpinSystem;
    //readonly TeleporterSystemClient m_TeleporterSystemClient;

    Entity m_localPlayer;
    private ISnapshotConsumer m_NullSnapshotConsumer;
}


public class ThinClientGameLoop : Game.IGameLoop
{
    [ConfigVar(Name = "thinclient.requested", DefaultValue = "4", Description = "Number of thin clients wanted")]
    public static ConfigVar thinClientNum;

    List<ThinClient> thinClients = new List<ThinClient>();

    public void FixedUpdate()
    {
    }

    public bool Init(string[] args)
    {
        NetworkClient.m_DropSnapshots = true;

#if UNITY_EDITOR
        Game.game.levelManager.UnloadLevel();
#endif
        Console.AddCommand("disconnect", CmdDisconnect, "Disconnect from server if connected", this.GetHashCode());

        GameDebug.Log("ThinClient initialized");

        return true;
    }

    void CmdDisconnect(string[] args)
    {
        foreach (var c in thinClients)
            c.Disconnect();
    }

    public void LateUpdate()
    {
    }



    public void Shutdown()
    {
        NetworkClient.m_DropSnapshots = false;
    }

    public void Update()
    {
        if (targetServer != "" && (Time.frameCount % 10 == 0))
        {
            if (thinClients.Count < thinClientNum.IntValue)
            {
                GameDebug.Log("Creating new thin client:" + thinClients.Count);
                var c = new ThinClient();
                thinClients.Add(c);
                c.Connect(targetServer);
            }
            else if (thinClients.Count > thinClientNum.IntValue && thinClients.Count > 0)
            {
                GameDebug.Log("Removing thin client:" + thinClients.Count);
                var i = thinClients.Count - 1;
                thinClients[i].Disconnect();
                thinClients.RemoveAt(i);
            }
        }

        for (int i = 0; i < thinClients.Count; ++i)
        {
            thinClients[i].Update();
        }
    }

    public void CmdConnect(string[] args)
    {
        targetServer = args.Length > 0 ? args[0] : "127.0.0.1";
        GameDebug.Log("Will connect to: " + targetServer);
    }

    string targetServer = "";
}

public class ThinClient : INetworkCallbacks, INetworkClientCallbacks
{
    string targetServer = "";

    public ThinClient()
    {
        m_StateMachine = new StateMachine<ClientState>();
        m_StateMachine.Add(ClientState.Browsing,    EnterBrowsingState,     UpdateBrowsingState,    LeaveBrowsingState);
        m_StateMachine.Add(ClientState.Connecting,  EnterConnectingState,   UpdateConnectingState,  null);
        m_StateMachine.Add(ClientState.Loading,     EnterLoadingState,      UpdateLoadingState,     null);
        m_StateMachine.Add(ClientState.Playing,     EnterPlayingState,      UpdatePlayingState,     LeavePlayingState);
        m_StateMachine.SwitchTo(m_GameWorld,ClientState.Browsing);

#pragma warning disable 618
        // we're keeping World.Active until we can properly remove them all
        // TODO (timj) not compatible with dots netcode
        m_GameWorld = World.Active;
        World.Active.CreateSystem<GameTimeSystem>();
#pragma warning restore 618



        m_Transport = new SocketTransport();

        m_NetworkClient = new NetworkClient(m_Transport);

        if (Application.isEditor || Game.game.buildId == "AutoBuild")
            NetworkClient.clientVerifyProtocol.Value = "0";

        m_NetworkClient.UpdateClientConfig();
        m_NetworkStatistics = new NetworkStatisticsClient(m_NetworkClient);
        m_ChatSystem = new ChatSystemClient();

        GameDebug.Log("Network client initialized");

        m_requestedPlayerSettings.playerName = ClientGameLoop.clientPlayerName.Value;
        m_requestedPlayerSettings.teamId = -1;
    }

    public void Shutdown()
    {
        GameDebug.Log("ClientGameLoop shutdown");
        Console.RemoveCommandsWithTag(this.GetHashCode());

        m_StateMachine.Shutdown();

        m_NetworkClient.Shutdown();

        PrefabAssetManager.Shutdown();
        m_Transport.Shutdown();
    }

    public void OnConnect(int clientId) {}
    public void OnDisconnect(int clientId) {}

    unsafe public void OnEvent(int clientId, NetworkEvent info)
    {
        Profiler.BeginSample("-ProcessEvent");
        switch ((GameNetworkEvents.EventType)info.type.typeId)
        {
            case GameNetworkEvents.EventType.Chat:
                fixed(uint* data = info.data)
                {
                    var reader = new NetworkReader(data, info.type.schema);
                    //m_ChatSystem.ReceiveMessage(reader.ReadString(256));
                }
                break;
        }
        Profiler.EndSample();
    }

    public void OnMapUpdate(ref NetworkReader data)
    {
        m_LevelName = data.ReadString();
        if (m_StateMachine.CurrentState() != ClientState.Loading)
            m_StateMachine.SwitchTo(m_GameWorld,ClientState.Loading);
    }

    public void Update()
    {
        Profiler.BeginSample("ClientGameLoop.Update");

        Profiler.BeginSample("-NetworkClientUpdate");
        m_NetworkClient.Update(this, m_clientWorld?.GetSnapshotConsumer());
        Profiler.EndSample();

        Profiler.BeginSample("-StateMachine update");
        m_StateMachine.Update();
        Profiler.EndSample();

        m_NetworkClient.SendData();

        if (m_NetworkClient.isConnected && m_playerSettingsUpdated)
        {
            m_playerSettingsUpdated = false;
            SendPlayerSettings();
        }

        if (m_clientWorld != null)
            m_NetworkStatistics.Update(m_clientWorld.frameTimeScale, GameTime.GetDuration(m_clientWorld.RenderTime, m_clientWorld.PredictedTime));

        Profiler.EndSample();
    }

    void EnterBrowsingState()
    {
        GameDebug.Assert(m_clientWorld == null);
        m_ClientState = ClientState.Browsing;
    }

    void UpdateBrowsingState()
    {
    }

    void LeaveBrowsingState()
    {
    }

    int connectRetryCount;
    void EnterConnectingState()
    {
        GameDebug.Assert(m_ClientState == ClientState.Browsing, "Expected ClientState to be browsing");
        GameDebug.Assert(m_clientWorld == null, "Expected ClientWorld to be null");
        GameDebug.Assert(m_NetworkClient.connectionState == NetworkClient.ConnectionState.Disconnected, "Expected network connectionState to be disconnected");

        m_ClientState = ClientState.Connecting;
        connectRetryCount = 0;
    }

    void UpdateConnectingState()
    {
        switch (m_NetworkClient.connectionState)
        {
            case NetworkClient.ConnectionState.Connected:
                break;
            case NetworkClient.ConnectionState.Connecting:
                // Do nothing; just wait for either success or failure
                break;
            case NetworkClient.ConnectionState.Disconnected:
                if (connectRetryCount < 2)
                {
                    connectRetryCount++;
                    var msg = string.Format("Trying to connect to {0} (attempt #{1})...", targetServer, connectRetryCount);
                    GameDebug.Log(msg);
                    m_NetworkClient.Connect(targetServer);
                }
                else
                {
                    var msg = "Failed to connect to server";
                    GameDebug.Log(msg);
                    m_NetworkClient.Disconnect();
                    m_StateMachine.SwitchTo(m_GameWorld,ClientState.Browsing);
                }
                break;
        }
    }

    void EnterLoadingState()
    {
        GameDebug.Assert(m_clientWorld == null);
        GameDebug.Assert(m_NetworkClient.isConnected);

        m_requestedPlayerSettings.playerName = "ThinPlayer";
        m_requestedPlayerSettings.characterType = (short)Game.characterType.IntValue;
        m_playerSettingsUpdated = true;

        if(Game.game.levelManager.currentLevel == null)
            Game.game.levelManager.LoadLevel("testlevel");

        m_ClientState = ClientState.Loading;
    }

    void UpdateLoadingState()
    {
        // Handle disconnects
        if (!m_NetworkClient.isConnected)
        {
            var msg = "Disconnected from server (lost connection)";
            GameDebug.Log(msg);
            m_StateMachine.SwitchTo(m_GameWorld,ClientState.Browsing);
        }

        if (Game.game.levelManager.IsCurrentLevelLoaded())
        {
            // TODO (mogensh) we should find a better way to make sure subscene is loaded (this uses knowledge of what is in subscene)
            var query = m_GameWorld.EntityManager.CreateEntityQuery(typeof(HeroRegistry.RegistryEntity));
            var ready = query.CalculateEntityCount() > 0;
            query.Dispose();
            if (ready)
                m_StateMachine.SwitchTo(m_GameWorld,ClientState.Playing);

            GameDebug.Log("Waiting for HeroRegistry");
        }
    }

    void EnterPlayingState()
    {
        GameDebug.Assert(m_clientWorld == null);

        m_clientWorld = new ThinClientGameWorld(m_GameWorld, m_NetworkClient, m_NetworkStatistics);
        m_clientWorld.PredictionEnabled = m_predictionEnabled;

        m_LocalPlayer = m_clientWorld.RegisterLocalPlayer(m_NetworkClient.clientId);


        m_NetworkClient.QueueEvent((ushort)GameNetworkEvents.EventType.PlayerReady, true, (ref NetworkWriter data) => {});

        m_ClientState = ClientState.Playing;
    }

    void LeavePlayingState()
    {
        //Game.game.clientFrontend.Clear();

        PrefabAssetManager.DestroyEntity(m_GameWorld.EntityManager, m_LocalPlayer);
        m_LocalPlayer = Entity.Null;

        m_clientWorld.Shutdown();
        m_clientWorld = null;

        // TODO (petera) replace this with a stack of levels or similar thing. For now we just load the menu no matter what
        //Game.game.levelManager.UnloadLevel();
        //Game.game.levelManager.LoadLevel("level_menu");

        PrefabAssetManager.Shutdown();
#pragma warning disable 618
        // we're keeping World.Active until we can properly remove them all
        // FIXME: not compatible with dots netcode
        m_GameWorld = World.Active;
        World.Active.CreateSystem<GameTimeSystem>();
#pragma warning restore 618

        //Game.game.clientFrontend.ShowMenu(ClientFrontend.MenuShowing.None);

        //Game.game.levelManager.LoadLevel("level_menu");

        GameDebug.Log("Left playingstate");
    }

    void UpdatePlayingState()
    {
        // Handle disconnects
        if (!m_NetworkClient.isConnected)
        {
            var msg = "Disconnected from server (lost connection)";
            GameDebug.Log(msg);
            m_StateMachine.SwitchTo(m_GameWorld,ClientState.Browsing);
            return;
        }

        // (re)send client info if any of the configvars that contain clientinfo has changed
        if ((ConfigVar.DirtyFlags & ConfigVar.Flags.ClientInfo) == ConfigVar.Flags.ClientInfo)
        {
            m_NetworkClient.UpdateClientConfig();
            ConfigVar.DirtyFlags &= ~ConfigVar.Flags.ClientInfo;
        }

        float frameDuration = m_lastFrameTime != 0 ? (float)(Game.frameTime - m_lastFrameTime) : 0;
        m_lastFrameTime = Game.frameTime;

        m_clientWorld.Update(frameDuration);
    }

    public void RemoteConsoleCommand(string command)
    {
        m_NetworkClient.QueueEvent((ushort)GameNetworkEvents.EventType.RemoteConsoleCmd, true, (ref NetworkWriter writer) =>
        {
            writer.WriteString("args", command);
        });
    }

    public void Disconnect()
    {
        m_NetworkClient.Disconnect();
        m_StateMachine.SwitchTo(m_GameWorld,ClientState.Browsing);
    }

    void SendPlayerSettings()
    {
        m_NetworkClient.QueueEvent((ushort)GameNetworkEvents.EventType.PlayerSetup, true, (ref NetworkWriter writer) =>
        {
            m_requestedPlayerSettings.Serialize(ref writer);
        });
    }

    public void Connect(string targetServer)
    {
        if (m_StateMachine.CurrentState() != ClientState.Browsing)
            return;
        this.targetServer = targetServer;
        m_StateMachine.SwitchTo(m_GameWorld,ClientState.Connecting);
    }

    public enum ClientState
    {
        Browsing,
        Connecting,
        Loading,
        Playing,
    }
    StateMachine<ClientState> m_StateMachine;

    ClientState m_ClientState;

    World m_GameWorld;
    private SocketTransport m_Transport;
    NetworkClient m_NetworkClient;

    Entity m_LocalPlayer;
    PlayerSettings m_requestedPlayerSettings = new PlayerSettings();
    bool m_playerSettingsUpdated;


    NetworkStatisticsClient m_NetworkStatistics;
    ChatSystemClient m_ChatSystem;

    ThinClientGameWorld m_clientWorld;


    string m_LevelName;

    double m_lastFrameTime;
    bool m_predictionEnabled = true;
}
