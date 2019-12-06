using Unity.Collections;
using UnityEngine;
using Unity.Entities;
using Unity.Jobs;
using Unity.Networking.Transport;
using Unity.Scenes;
using UnityEngine.Profiling;
using Unity.NetCode;
using Unity.Sample.Core;


class A2DotsShooterSendSystem : CommandSendSystem<UserCommand>
{
}
class A2DotsShooterRecvSystem : CommandReceiveSystem<UserCommand>
{
}



[UpdateInGroup(typeof(ClientSimulationSystemGroup))]
[UpdateBefore(typeof(GhostSimulationSystemGroup))]
[UpdateBefore(typeof(A2DotsShooterSendSystem))]
[AlwaysUpdateSystem]
[AlwaysSynchronizeSystem]
public class BeforeClientPredictionSystem : JobComponentSystem
{
    public ClientGameWorld GameWorld;
    public int TickRate;
    protected override JobHandle OnUpdate(JobHandle inputDeps)
    {
        var tickRate = default(ClientServerTickRate);
        if (HasSingleton<ClientServerTickRate>())
            tickRate = GetSingleton<ClientServerTickRate>();
        tickRate.ResolveDefaults();
        TickRate = tickRate.SimulationTickRate;
        if (GameWorld != null)
            GameWorld.BeforePredictionUpdate(Time.DeltaTime,  HasSingleton<ThinClientComponent>());
        return default;
    }
}
[UpdateInGroup(typeof(ClientSimulationSystemGroup))]
[UpdateAfter(typeof(GhostSimulationSystemGroup))]
[UpdateBefore(typeof(Unity.Animation.AnimationSystemGroup))]
[AlwaysUpdateSystem]
[AlwaysSynchronizeSystem]
public class AfterClientPredictionSystem : JobComponentSystem
{
    public ClientGameWorld GameWorld;
    protected override JobHandle OnUpdate(JobHandle inputDeps)
    {
        if (GameWorld != null && !HasSingleton<ThinClientComponent>())
            GameWorld.AfterPredictionUpdate();
        return default;
    }
}

[UpdateInGroup(typeof(GhostPredictionSystemGroup))]
[AlwaysUpdateSystem]
[AlwaysSynchronizeSystem]
public class ClientPredictionSystem : JobComponentSystem
{
    public ClientGameWorld GameWorld;
    protected override JobHandle OnUpdate(JobHandle inputDeps)
    {
        if (GameWorld != null && !HasSingleton<ThinClientComponent>())
            GameWorld.PredictionUpdate(World.GetExistingSystem<GhostPredictionSystemGroup>().PredictingTick, Time.DeltaTime);
        return default;
    }
}
[UpdateInGroup(typeof(ClientPresentationSystemGroup))]
[AlwaysUpdateSystem]
[AlwaysSynchronizeSystem]
public class ClientLateUpdateSystem : JobComponentSystem
{
    public ClientGameWorld GameWorld;
    protected override JobHandle OnUpdate(JobHandle inputDeps)
    {
        if (GameWorld != null && !HasSingleton<ThinClientComponent>())
            GameWorld.LateUpdate(World.GetExistingSystem<ChatSystemClient>(), Time.DeltaTime);
        return default;
    }
}



public class ClientGameWorld
{
    public ClientGameWorld(World world, int localPlayerId)
    {
        m_GameWorld = world;

        m_PlayerModule = new PlayerModuleClient(m_GameWorld);

        m_HandleDamageGroup = m_GameWorld.CreateSystem<HandleDamageSystemGroup>();

        m_HandleControlledEntityChangedGroup = m_GameWorld.CreateSystem<ManualComponentSystemGroup>();
        m_HandleControlledEntityChangedGroup.AddSystemToUpdateList(m_GameWorld.CreateSystem<PlayerCharacterControl.PlayerCharacterControlSystem>());

        m_PredictedUpdateGroup = m_GameWorld.CreateSystem<ManualComponentSystemGroup>();
        m_PredictedUpdateGroup.AddSystemToUpdateList(CharacterModule.CreateClientUpdateSystemGroup(world));
        m_PredictedUpdateGroup.AddSystemToUpdateList(world.CreateSystem<AbilityUpdateSystemGroup>());

        m_AfterPredictionUpdateGroup = m_GameWorld.CreateSystem<ManualComponentSystemGroup>();
        m_AfterPredictionUpdateGroup.AddSystemToUpdateList(m_GameWorld.GetOrCreateSystem(typeof(PartSystemUpdateGroup)));
        m_AfterPredictionUpdateGroup.AddSystemToUpdateList(CharacterModule.CreateClientPresentationSystemGroup(world));
        m_AfterPredictionUpdateGroup.AddSystemToUpdateList(world.CreateSystem<UpdateCharacterUI>());


        m_GameModeSystem = m_GameWorld.CreateSystem<GameModeSystemClient>();
        m_ClientFrontendUpdate = m_GameWorld.CreateSystem<ClientFrontendUpdate>();
        m_TeleporterSystemClient = m_GameWorld.CreateSystem<TeleporterSystemClient>();
        m_ClientLateUpdate = m_GameWorld.CreateSystem<ClientLateUpdateGroup>();

        m_GameModeSystem.SetLocalPlayerId(localPlayerId);

        m_controlledEntityCameraUpdate = m_GameWorld.GetOrCreateSystem<ControlledEntityCameraUpdate>();
        m_controlledEntityCameraUpdate.SortSystemUpdateList();// TODO (mogensh) currently needed because of bug in entities preview.26

        //@TODO: Temp hack for unite keynote to hide error
        Debug.developerConsoleVisible = false;
    }

    public void Shutdown(bool isDestroyingWorld)
    {
        m_PlayerModule.Shutdown();

        // When destroying the world all systems will be torn down - so no need to do it manually
        if (!isDestroyingWorld)
        {
            m_HandleDamageGroup.DestroyGroup();
            m_GameWorld.DestroySystem(m_HandleControlledEntityChangedGroup);
            m_GameWorld.DestroySystem(m_PredictedUpdateGroup);
            m_GameWorld.DestroySystem(m_AfterPredictionUpdateGroup);
            m_GameWorld.DestroySystem(m_GameModeSystem);
            m_GameWorld.DestroySystem(m_TeleporterSystemClient);
            m_GameWorld.DestroySystem(m_ClientLateUpdate);

            m_controlledEntityCameraUpdate.DestroyGroup();
        }

        AnimationGraphHelper.Shutdown(m_GameWorld);
    }

    public void BeforePredictionUpdate(float frameDuration, bool thinClient)
    {
        // Advances time and accumulate input into the UserCommand being generated
        HandleTime(frameDuration, thinClient);
        var gameTimeSystem = m_GameWorld.GetExistingSystem<GameTimeSystem>();
        gameTimeSystem.SetWorldTime(m_RenderTime);
        gameTimeSystem.frameDuration = frameDuration;
        if (thinClient)
            return;

        m_PlayerModule.ResolveReferenceFromLocalPlayerToPlayer();
        m_PlayerModule.HandleCommandReset();


        // Handle spawning
        m_PlayerModule.HandleSpawn();

        // Handle controlled entity changed
        m_HandleControlledEntityChangedGroup.Update();
        m_PlayerModule.HandleControlledEntityChanged();

        // Prediction
        gameTimeSystem.SetWorldTime(m_PredictedTime);
    }

    public void AfterPredictionUpdate()
    {
        var gameTimeSystem = m_GameWorld.GetExistingSystem<GameTimeSystem>();

        // TODO (timj) is this needed?
        gameTimeSystem.SetWorldTime(m_PredictedTime);
        m_PlayerModule.RetrieveCommand((uint)gameTimeSystem.GetWorldTime().tick);


        m_GameModeSystem.Update();

        // Update Presentation
        gameTimeSystem.SetWorldTime(m_PredictedTime);
        m_TeleporterSystemClient.Update();

        // TODO (mogensh) for now we upadte this AFTER CharacterModule as we depend on AnimSourceCtrl to run before bodypart. Sort this out
        m_AfterPredictionUpdateGroup.Update();


        gameTimeSystem.SetWorldTime(m_RenderTime);

        m_HandleDamageGroup.Update();


//#if UNITY_EDITOR
//
//        var localPlayerState = m_GameWorld.GetEntityManager().GetComponentObject<LocalPlayer>(m_localPlayer);
//
//        if (m_GameWorld.GetEntityManager().Exists(localPlayerState.controlledEntity) &&
//            m_GameWorld.GetEntityManager().HasComponent<PlayerControlled.State>(localPlayerState.controlledEntity))
//        {
//            var userCommand = m_GameWorld.GetEntityManager().GetComponentData<PlayerControlled.State>(localPlayerState.controlledEntity);
//            m_ReplicatedEntityModule.FinalizedStateHistory(m_PredictedTime.tick - 1, m_NetworkClient.serverTime, ref userCommand.command);
//        }
//#endif
    }
    public void LateUpdate(ChatSystemClient chatSystem, float frameDuration)
    {
        var gameTimeSystem = m_GameWorld.GetExistingSystem<GameTimeSystem>();
        gameTimeSystem.SetWorldTime(m_RenderTime);

        var localPlayerState = m_GameWorld.EntityManager.GetComponentData<LocalPlayer>(m_localPlayer);
        var teamId = -1;
        bool showScorePanel = false;
        if (localPlayerState.playerEntity != Entity.Null)
        {
            var playerState = m_GameWorld.EntityManager.GetComponentData<Player.State>(localPlayerState.playerEntity);
            if (playerState.controlledEntity != Entity.Null)
            {
                teamId = playerState.teamIndex;

                if (m_GameWorld.EntityManager.HasComponent<HealthStateData>(playerState.controlledEntity))
                {
                    var healthState = m_GameWorld.EntityManager.GetComponentData<HealthStateData>(playerState.controlledEntity);

                    // Only show score board when alive
                    showScorePanel = healthState.health <= 0;
                }

            }
        }
        // TODO (petera) fix this hack
        chatSystem.UpdateLocalTeamIndex(teamId);

        m_ClientLateUpdate.Update();

        m_controlledEntityCameraUpdate.Update();

        m_PlayerModule.CameraUpdate();

        gameTimeSystem.SetWorldTime(m_RenderTime);

        gameTimeSystem.SetWorldTime(m_PredictedTime);


        if (Game.game.clientFrontend != null)
        {
            m_ClientFrontendUpdate.Update();
            Game.game.clientFrontend.SetShowScorePanel(showScorePanel);
        }
    }

    public Entity RegisterLocalPlayer(int playerId)
    {
        m_localPlayer = m_PlayerModule.RegisterLocalPlayer(playerId);
        return m_localPlayer;
    }

    public void PredictionUpdate(uint tick, float deltaTime)
    {
        var gameTimeSystem = m_GameWorld.GetExistingSystem<GameTimeSystem>();

        var time = gameTimeSystem.GetWorldTime();
        // TODO (timj) tick should be uint
        time.SetTime((int)tick, deltaTime);
        gameTimeSystem.SetWorldTime(time);
        m_PlayerModule.RetrieveCommand(tick);
        gameTimeSystem.SetWorldTime(time);
        m_PredictedUpdateGroup.Update();
    }

    void HandleTime(float frameDuration, bool thinClient)
    {
        var simGroup = m_GameWorld.GetExistingSystem<ClientSimulationSystemGroup>();
        // Sample input into current command
        //  The time passed in here is used to calculate the amount of rotation from stick position
        //  The command stores final view direction
        bool chatOpen = Game.game.clientFrontend != null && Game.game.clientFrontend.chatPanel.isOpen;
        bool userInputEnabled = InputSystem.GetMousePointerLock() && !chatOpen && !thinClient;
        if (simGroup.ServerTick != m_lastCommandTick)
        {
            m_lastCommandTick = simGroup.ServerTick;
            m_PlayerModule.ResetInput(userInputEnabled);
        }

        m_PlayerModule.SampleInput(m_GameWorld, userInputEnabled, frameDuration, m_RenderTime.tick);

        var serverTickRate = m_GameWorld.GetExistingSystem<BeforeClientPredictionSystem>().TickRate;
        m_PredictedTime.tickRate = serverTickRate;
        m_RenderTime.tickRate = serverTickRate;
        // TODO (timj) tick should be uint for dots netcode
        m_RenderTime.tick = (int)simGroup.InterpolationTick;
        m_RenderTime.tickDuration = frameDuration;
        m_PredictedTime.tick = (int) simGroup.ServerTick;
        m_PredictedTime.tickDuration = frameDuration;

        m_PlayerModule.StoreCommand(m_PredictedTime.tick);
    }

    World m_GameWorld;
    GameTime m_PredictedTime = new GameTime(60);
    GameTime m_RenderTime = new GameTime(60);

    // External systems
    ClientFrontendUpdate m_ClientFrontendUpdate;

    // Internal systems
    readonly PlayerModuleClient m_PlayerModule;

    readonly HandleDamageSystemGroup m_HandleDamageGroup;

    readonly ManualComponentSystemGroup m_HandleControlledEntityChangedGroup;
    readonly ManualComponentSystemGroup m_PredictedUpdateGroup;
    readonly ManualComponentSystemGroup m_AfterPredictionUpdateGroup;

    readonly GameModeSystemClient m_GameModeSystem;
    readonly ControlledEntityCameraUpdate m_controlledEntityCameraUpdate;
    private readonly ClientLateUpdateGroup m_ClientLateUpdate;
    readonly TeleporterSystemClient m_TeleporterSystemClient;

    Entity m_localPlayer;
    uint m_lastCommandTick;
}


public class ClientGameLoop : Game.IGameLoop
{
    // Client vars
    [ConfigVar(Name = "client.updaterate", DefaultValue = "30000", Description = "Max bytes/sec client wants to receive", Flags = ConfigVar.Flags.ClientInfo)]
    public static ConfigVar clientUpdateRate;
    [ConfigVar(Name = "client.updateinterval", DefaultValue = "3", Description = "Snapshot sendrate requested by client", Flags = ConfigVar.Flags.ClientInfo)]
    public static ConfigVar clientUpdateInterval;

    [ConfigVar(Name = "client.playername", DefaultValue = "", Description = "Name of player", Flags = ConfigVar.Flags.ClientInfo | ConfigVar.Flags.Save)]
    public static ConfigVar clientPlayerName;

    public static string[] CurrentArgs;
    private World m_World;
    private bool m_NewWorld;
    public bool Init(string[] args)
    {
        // TODO (timj) create singleton for parameters instead
        CurrentArgs = args;
        m_World = ClientServerBootstrap.CreateClientWorld(GameBootStrap.DefaultWorld, "ClientWorld0");
        m_World.GetExistingSystem<ClientGameLoopSystem>().GameLoop = this;
        CurrentArgs = null;
        return true;
    }
    public void Shutdown()
    {
        // TODO (timj) world shutdown order - the ecs worlds are already torn down when application quit is called
        if (m_World == null)
            return;
        m_World.GetExistingSystem<ClientGameLoopSystem>().Shutdown();
        m_World.Dispose();
    }

    public void Update()
    {
        if (m_NewWorld)
        {
            // TODO (timj) copy some state from the world and restore after recreating it
            m_World.Dispose();
            Init(null);
        }
    }
    public void FixedUpdate()
    {
    }
    public void LateUpdate()
    {
    }

    public void CmdConnect(string[] args)
    {
        m_World.GetExistingSystem<ClientGameLoopSystem>().CmdConnect(args);
    }

    public void RequestNewWorld()
    {
        m_NewWorld = true;
    }
}

[UpdateInGroup(typeof(ClientInitializationSystemGroup))]
[AlwaysUpdateSystem]
public class ClientGameLoopSystem : ComponentSystem
{
    private Entity netCodePrefabs;
    private EntityQuery m_ConnectionQuery;
    private NetworkStreamReceiveSystem m_NetworkStreamReceiveSystem;
    public ClientGameLoop GameLoop;

    public static Unity.Entities.Hash128 ClientBuildSettingsGUID => new Unity.Entities.Hash128("53329cb030ec042419e0a8b2387da5c7");

    public void Shutdown()
    {
        m_StateMachine.SwitchTo(World, ClientState.Browsing);
    }
    protected override void OnCreate()
    {
        var args = ClientGameLoop.CurrentArgs;
        if (args == null)
        {
            #if UNITY_EDITOR
            if (GameBootStrap.IsSingleLevelPlaymode)
                args = new[] {ClientServerBootstrap.RequestedAutoConnect};
            else
            #endif
                args = new string[0];
        }

        World.GetOrCreateSystem<SceneSystem>().BuildSettingsGUID = ClientBuildSettingsGUID;

        netCodePrefabs = Entity.Null;

        m_NetworkStreamReceiveSystem = World.GetOrCreateSystem<NetworkStreamReceiveSystem>();
        m_ConnectionQuery = EntityManager.CreateEntityQuery(ComponentType.ReadWrite<NetworkStreamConnection>());

        m_StateMachine = new StateMachine<ClientState>();
        m_StateMachine.Add(ClientState.Browsing,    EnterBrowsingState,     UpdateBrowsingState,    LeaveBrowsingState);
        m_StateMachine.Add(ClientState.Connecting,  EnterConnectingState,   UpdateConnectingState,  null);
        m_StateMachine.Add(ClientState.Loading,     EnterLoadingState,      UpdateLoadingState,     null);
        m_StateMachine.Add(ClientState.WaitSubscene,null,                  UpdateWaitSubsceneState,     null);
        m_StateMachine.Add(ClientState.Playing,     EnterPlayingState,      UpdatePlayingState,     LeavePlayingState);

        m_GameWorld = World;
        World.CreateSystem<GameTimeSystem>();

        GameDebug.Log("Network client initialized");

        if(ClientGameLoop.clientPlayerName.Value.Length == 0)
        {
            // Pick silly default name if none set
            ClientGameLoop.clientPlayerName.Value =
                new string[] { "Shiny", "Jumpy", "Fast", "Snappy", "Smooth", "First" }[Random.Range(0, 6)] + " " +
                new string[] { "Entity", "Component", "Buffer", "System", "Chunk", "Array" }[Random.Range(0, 6)];
        }

        m_requestedPlayerSettings.playerName = ClientGameLoop.clientPlayerName.Value;
        m_requestedPlayerSettings.teamId = -1;

        Console.AddCommand("disconnect", CmdDisconnect, "Disconnect from server if connected", this.GetHashCode());
        Console.AddCommand("runatserver", CmdRunAtServer, "Run command at server", this.GetHashCode());
        Console.AddCommand("respawn", CmdRespawn, "Force a respawn", this.GetHashCode());
        Console.AddCommand("nextchar", CmdNextChar, "Select next character", this.GetHashCode());
        Console.AddCommand("nextteam", CmdNextTeam, "Select next character", this.GetHashCode());

        if (args.Length > 0)
        {
            targetServer = args[0];
            m_StateMachine.SwitchTo(m_GameWorld, ClientState.Connecting);
        }
        else
            m_StateMachine.SwitchTo(m_GameWorld, ClientState.Browsing);

        GameDebug.Log("Client initialized");
    }

    protected override void OnDestroy()
    {
        m_isDestroyingWorld = true;
        GameDebug.Log("ClientGameLoop shutdown");
        Console.RemoveCommandsWithTag(this.GetHashCode());

        m_StateMachine.Shutdown();

        PrefabAssetManager.Shutdown();
    }

    bool IsConnected()
    {
        return m_ConnectionQuery.CalculateEntityCount() == 1 &&
               m_ConnectionQuery.GetSingleton<NetworkStreamConnection>().Value.GetState(m_NetworkStreamReceiveSystem.Driver) ==
               NetworkConnection.State.Connected;
    }

    void Disconnect()
    {
        m_NetworkStreamReceiveSystem.Driver.Disconnect(m_ConnectionQuery.GetSingleton<NetworkStreamConnection>().Value);
        m_NetworkStreamReceiveSystem.EntityManager.AddComponent<NetworkStreamDisconnected>(m_ConnectionQuery.GetSingletonEntity());
    }

    protected override void OnUpdate()
    {
        Profiler.BeginSample("-StateMachine update");
        m_StateMachine.Update();
        Profiler.EndSample();

        // TODO (petera) change if we have a lobby like setup one day
        if (m_StateMachine.CurrentState() == ClientState.Playing && Game.game.clientFrontend != null)
            Game.game.clientFrontend.UpdateChat(World.GetExistingSystem<ChatSystemClient>());

        // TODO (petera) merge with clientinfo
        if (m_requestedPlayerSettings.playerName != ClientGameLoop.clientPlayerName.Value)
        {
            // Cap name length
            ClientGameLoop.clientPlayerName.Value = ClientGameLoop.clientPlayerName.Value.Substring(0, Mathf.Min(ClientGameLoop.clientPlayerName.Value.Length, 16));
            m_requestedPlayerSettings.playerName = ClientGameLoop.clientPlayerName.Value;
            m_playerSettingsUpdated = true;
        }

        if (IsConnected() && m_playerSettingsUpdated)
        {
            m_playerSettingsUpdated = false;
            SendPlayerSettings();
        }

        if (HasSingleton<NetworkSnapshotAckComponent>())
        {
            if (Game.game.m_GameStatistics != null)
            {
                Game.game.m_GameStatistics.rtt = Mathf.RoundToInt(GetSingleton<NetworkSnapshotAckComponent>().EstimatedRTT);
                Game.game.m_GameStatistics.commandAge = GetSingleton<NetworkSnapshotAckComponent>().ServerCommandAge / 256.0f;
            }
        }

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

    string targetServer = "";
    int connectRetryCount;
    void EnterConnectingState()
    {
        GameDebug.Assert(m_ClientState == ClientState.Browsing, "Expected ClientState to be browsing");
        GameDebug.Assert(m_clientWorld == null, "Expected ClientWorld to be null");
        GameDebug.Assert(!IsConnected(), "Expected network connectionState to be disconnected");

        m_ClientState = ClientState.Connecting;
        connectRetryCount = 0;
    }

    void UpdateConnectingState()
    {
        if (m_ConnectionQuery.CalculateEntityCount() == 0)
        {
            if (connectRetryCount < 2)
            {
                ++connectRetryCount;
                var gameMessage = string.Format("Trying to connect to {0} (attempt #{1})...", targetServer, connectRetryCount);
                GameDebug.Log(gameMessage);
                NetworkEndPoint ep;
                if (targetServer == "localhost")
                {
                    ep = NetworkEndPoint.LoopbackIpv4;
                    ep.Port = NetworkConfig.defaultServerPort;
                }
                else
                    ep = NetworkEndPoint.Parse(targetServer, NetworkConfig.defaultServerPort);
                m_NetworkStreamReceiveSystem.Connect(ep);
            }
            else
            {
                var gameMessage = "Failed to connect to server";
                GameDebug.Log(gameMessage);
                m_StateMachine.SwitchTo(m_GameWorld,ClientState.Browsing);
            }
        }
        else if (IsConnected())
        {
            m_GameMessage = "Waiting for map info";
        }

        var entityManager = m_GameWorld.EntityManager;
        var query = entityManager
            .CreateEntityQuery(ComponentType.ReadWrite<ActiveStateComponentData>());
        if (query.CalculateEntityCount() == 1)
        {
            var entity = query.GetSingletonEntity();
            if (!IsConnected())
            {
                EntityManager.DestroyEntity(entity);
                return;
            }
            var mapInfo = entityManager.GetComponentData<ActiveStateComponentData>(entity);
            m_LevelName = mapInfo.MapName.ToString();
            if (m_StateMachine.CurrentState() != ClientState.Loading)
                m_StateMachine.SwitchTo(m_GameWorld,ClientState.Loading);
            entityManager.DestroyEntity(entity);
        }
    }

    void EnterLoadingState()
    {
        if (Game.game.clientFrontend != null)
            Game.game.clientFrontend.ShowMenu(ClientFrontend.MenuShowing.None);

        Console.SetOpen(false);

        GameDebug.Assert(m_clientWorld == null);
        GameDebug.Assert(IsConnected());

        m_requestedPlayerSettings.playerName = ClientGameLoop.clientPlayerName.Value;
        m_requestedPlayerSettings.characterType = (short)Game.characterType.IntValue;
        m_playerSettingsUpdated = true;

        m_ClientState = ClientState.Loading;
    }

    void UpdateLoadingState()
    {
        // Handle disconnects
        if (!IsConnected())
        {
            m_GameMessage = m_DisconnectReason != null ? string.Format("Disconnected from server ({0})", m_DisconnectReason) : "Disconnected from server (lost connection)";
            m_DisconnectReason = null;
            m_StateMachine.SwitchTo(m_GameWorld,ClientState.Browsing);
        }

        // Wait until we got level info
        if (m_LevelName == null)
            return;

        // Load if we are not already loading
        var level = Game.game.levelManager.currentLevel;
        if (level == null || level.name != m_LevelName)
        {
            if (!Game.game.levelManager.LoadLevel(m_LevelName))
            {
                m_DisconnectReason = string.Format("could not load requested level '{0}'", m_LevelName);
                Disconnect();
                return;
            }
            level = Game.game.levelManager.currentLevel;
        }

        // Wait for level to be loaded
        if (Game.game.levelManager.IsCurrentLevelLoaded())
            m_StateMachine.SwitchTo(m_GameWorld,ClientState.WaitSubscene);
    }

    void UpdateWaitSubsceneState()
    {
        // TODO (mogensh) we should find a better way to make sure subscene is loaded (this uses knowledge of what is in subscene)
        var query = m_GameWorld.EntityManager.CreateEntityQuery(typeof(HeroRegistry.RegistryEntity));
        var ready = query.CalculateEntityCount() > 0;
        query.Dispose();
        if(ready)
            m_StateMachine.SwitchTo(m_GameWorld,ClientState.Playing);
    }

    void EnterPlayingState()
    {
        GameDebug.Assert(m_clientWorld == null && Game.game.levelManager.IsCurrentLevelLoaded());

        //if (Game.netUseDotsNetworking.IntValue > 0 && netCodePrefabs == Entity.Null)
        //    netCodePrefabs = PrefabAssetManager.CreateEntity(m_GameWorld.GetECSWorld(), Game.game.dotsNetCodePrefabs);

        var idQuery = m_NetworkStreamReceiveSystem.EntityManager
            .CreateEntityQuery(ComponentType.ReadWrite<NetworkIdComponent>(), ComponentType.Exclude<NetworkStreamDisconnected>());
        var clientId = m_NetworkStreamReceiveSystem.EntityManager.GetComponentData<NetworkIdComponent>(idQuery.GetSingletonEntity()).Value;

        m_clientWorld = new ClientGameWorld(m_GameWorld, clientId);

        m_LocalPlayer = m_clientWorld.RegisterLocalPlayer(clientId);

        var eventSystem = m_GameWorld.GetOrCreateSystem<ClientReadyEventSystem>();
        eventSystem.SendPlayerReadyRpc();

        m_ClientState = ClientState.Playing;

        m_GameWorld.GetExistingSystem<BeforeClientPredictionSystem>().GameWorld = m_clientWorld;
        m_GameWorld.GetExistingSystem<ClientPredictionSystem>().GameWorld = m_clientWorld;
        m_GameWorld.GetExistingSystem<AfterClientPredictionSystem>().GameWorld = m_clientWorld;
        var lateUpdate = m_GameWorld.GetExistingSystem<ClientLateUpdateSystem>();
        lateUpdate.GameWorld = m_clientWorld;
    }

    void LeavePlayingState()
    {
        if (!m_isDestroyingWorld)
        {
            if (IsConnected())
                Disconnect();
            m_GameWorld.GetExistingSystem<BeforeClientPredictionSystem>().GameWorld = null;
            m_GameWorld.GetExistingSystem<ClientPredictionSystem>().GameWorld = null;
            m_GameWorld.GetExistingSystem<AfterClientPredictionSystem>().GameWorld = null;
            m_GameWorld.GetExistingSystem<ClientLateUpdateSystem>().GameWorld = null;
        }

        m_LocalPlayer = Entity.Null;

        m_clientWorld.Shutdown(m_isDestroyingWorld);
        m_clientWorld = null;

        // TODO (petera) replace this with a stack of levels or similar thing. For now we just load the menu no matter what
        //Game.game.levelManager.UnloadLevel();
        //Game.game.levelManager.LoadLevel("level_menu");

        PrefabAssetManager.Shutdown();
        if (!m_isDestroyingWorld)
            GameLoop.RequestNewWorld();

        // Game can be null because destruction order when exiting playmode is undefined right now
        if (Game.game != null && Game.game.clientFrontend != null)
        {
            Game.game.clientFrontend.Clear();
            Game.game.clientFrontend.ShowMenu(ClientFrontend.MenuShowing.None);
        }

        #if UNITY_EDITOR
        // Game can be null because destruction order when exiting playmode is undefined right now
        if (Game.game != null && !GameBootStrap.IsSingleLevelPlaymode)
        #endif
            Game.game.levelManager.LoadLevel("level_menu");

        GameDebug.Log("Left playingstate");
    }

    void UpdatePlayingState()
    {
        // Handle disconnects
        if (!IsConnected())
        {
            m_GameMessage = m_DisconnectReason != null ? string.Format("Disconnected from server ({0})", m_DisconnectReason) : "Disconnected from server (lost connection)";
            m_StateMachine.SwitchTo(m_GameWorld,ClientState.Browsing);
            return;
        }

        if (GatedInput.GetKeyUp(KeyCode.H))
        {
            RemoteConsoleCommand("nextchar");
        }

        if (GatedInput.GetKeyUp(KeyCode.T))
            CmdNextTeam(null);

        float frameDuration = m_lastFrameTime != 0 ? (float)(Game.frameTime - m_lastFrameTime) : 0;
        m_lastFrameTime = Game.frameTime;
    }

    public void RemoteConsoleCommand(string command)
    {
        m_GameWorld.GetOrCreateSystem<ClientEventSystem>().SendRemoteCommandRpc(new NativeString64(command));
    }

    public void CmdConnect(string[] args)
    {
        if (m_StateMachine.CurrentState() == ClientState.Browsing)
        {
            targetServer = args.Length > 0 ? args[0] : "127.0.0.1";
            m_StateMachine.SwitchTo(m_GameWorld,ClientState.Connecting);
        }
        else if (m_StateMachine.CurrentState() == ClientState.Connecting)
        {
            Disconnect();
            targetServer = args.Length > 0 ? args[0] : "127.0.0.1";
            connectRetryCount = 0;
        }
        else
        {
            GameDebug.Log("Unable to connect from this state: " + m_StateMachine.CurrentState().ToString());
        }
    }

    void CmdDisconnect(string[] args)
    {
        m_DisconnectReason = "user manually disconnected";
        Disconnect();
        m_StateMachine.SwitchTo(m_GameWorld,ClientState.Browsing);
    }

    void CmdRunAtServer(string[] args)
    {
        RemoteConsoleCommand(string.Join(" ", args));
    }

    void CmdRespawn(string[] args)
    {
        if (m_LocalPlayer == Entity.Null)
            return;
        var localPlayerState = m_GameWorld.EntityManager.GetComponentData<LocalPlayer>(m_LocalPlayer);
        if (localPlayerState.playerEntity == Entity.Null)
            return;

        var playerState = m_GameWorld.EntityManager.GetComponentData<Player.State>(localPlayerState.playerEntity);
        if (playerState.controlledEntity == Entity.Null)
            return;

        // Request new char type
        if (args.Length == 1)
        {
            m_requestedPlayerSettings.characterType = short.Parse(args[0]);
            m_playerSettingsUpdated = true;
        }

        // Tell server who to respawn
        RemoteConsoleCommand(string.Format("s_respawn {0}", playerState.playerId));
    }

    void CmdNextChar(string[] args)
    {
        if (m_LocalPlayer == Entity.Null)
            return;
        var localPlayerState = m_GameWorld.EntityManager.GetComponentData<LocalPlayer>(m_LocalPlayer);
        if (localPlayerState.playerEntity == Entity.Null)
            return;

        var playerState = m_GameWorld.EntityManager.GetComponentData<Player.State>(localPlayerState.playerEntity);
        if (playerState.controlledEntity == Entity.Null)
            return;

        if (Game.allowCharChange.IntValue != 1)
            return;

        if (!m_GameWorld.EntityManager.HasComponent<Character.State>(playerState.controlledEntity))
            return;

        var heroRegistry = HeroRegistry.GetRegistry(m_GameWorld.EntityManager);
        var charSetupCount = heroRegistry.Value.Heroes.Length;

        m_requestedPlayerSettings.characterType = m_requestedPlayerSettings.characterType + 1;
        if (m_requestedPlayerSettings.characterType >= charSetupCount)
            m_requestedPlayerSettings.characterType = 0;
        m_playerSettingsUpdated = true;
    }

    void CmdNextTeam(string[] args)
    {
        if (m_LocalPlayer == Entity.Null)
            return;
        var localPlayerState = m_GameWorld.EntityManager.GetComponentData<LocalPlayer>(m_LocalPlayer);
        if (localPlayerState.playerEntity == Entity.Null)
            return;

        if (Game.allowCharChange.IntValue != 1)
            return;

        var playerState = m_GameWorld.EntityManager.GetComponentData<Player.State>(localPlayerState.playerEntity);

        m_requestedPlayerSettings.teamId = (short)(playerState.teamIndex + 1);
        if (m_requestedPlayerSettings.teamId > 1)
            m_requestedPlayerSettings.teamId = 0;
        m_playerSettingsUpdated = true;
    }

    void SendPlayerSettings()
    {
        var eventSystem = m_GameWorld.GetOrCreateSystem<ClientEventSystem>();
        eventSystem.SendPlayerSettingsRPC(m_requestedPlayerSettings);
    }

    enum ClientState
    {
        Browsing,
        Connecting,
        Loading,
        WaitSubscene,
        Playing,
    }
    StateMachine<ClientState> m_StateMachine;

    ClientState m_ClientState;

    World m_GameWorld;

    Entity m_LocalPlayer;
    PlayerSettings m_requestedPlayerSettings = new PlayerSettings();
    bool m_playerSettingsUpdated;

    ClientGameWorld m_clientWorld;

    string m_LevelName;

    string m_DisconnectReason = null;
    string m_GameMessage = "Welcome to the sample game!";

    double m_lastFrameTime;
    private bool m_isDestroyingWorld;

    [ConfigVar(Name = "client.showtickinfo", DefaultValue = "0", Description = "Show tick info")]
    static ConfigVar m_showTickInfo;
}

