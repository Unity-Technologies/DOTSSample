using UnityEngine;
using Unity.Entities;
using UnityEngine.Profiling;
using SQP;
using Unity.Animation;
using Unity.Collections;
using Unity.Jobs;
using Unity.Networking.Transport;
using Unity.Scenes;
using Unity.NetCode;
using Unity.Sample.Core;

[UpdateInGroup(typeof(ServerSimulationSystemGroup))]
[UpdateBefore(typeof(GhostSimulationSystemGroup))]
[AlwaysUpdateSystem]
[AlwaysSynchronizeSystem]
public class BeforeServerPredictionSystem : JobComponentSystem
{
    public ServerGameWorld GameWorld;
    protected override JobHandle OnUpdate(JobHandle inputDeps)
    {
        var gameWorld = GameWorld;
        var PostUpdateCommands = new EntityCommandBuffer(Allocator.TempJob);
        Entities
            .WithNone<NetworkStreamConnection>()
            .WithAll<AcceptedConnectionStateComponent>()
            .WithNativeDisableContainerSafetyRestriction(PostUpdateCommands)
            .WithoutBurst() // Captures managed data
            .ForEach((Entity entity) =>
            {
                if (gameWorld != null)
                    gameWorld.HandleClientDisconnect(PostUpdateCommands,entity);
                PostUpdateCommands.RemoveComponent<AcceptedConnectionStateComponent>(entity);
            }).Run();
        PostUpdateCommands.Playback(EntityManager);
        PostUpdateCommands.Dispose();

        if (GameWorld != null)
            GameWorld.BeforePredictionUpdate();
        return default;
    }
}
[UpdateInGroup(typeof(ServerSimulationSystemGroup))]
[UpdateAfter(typeof(GhostSimulationSystemGroup))]
[UpdateBefore(typeof(AnimationSystemGroup))]
[AlwaysSynchronizeSystem]
public class AfterServerPredictionSystem : JobComponentSystem
{
    public ServerGameWorld GameWorld;
    protected override JobHandle OnUpdate(JobHandle inputDeps)
    {
        if (GameWorld != null)
            GameWorld.AfterPredictionUpdate();
        return default;
    }
}
[UpdateInGroup(typeof(GhostPredictionSystemGroup))]
[AlwaysSynchronizeSystem]
public class ServerPredictionSystem : JobComponentSystem
{
    public ServerGameWorld GameWorld;
    protected override JobHandle OnUpdate(JobHandle inputDeps)
    {
        if (GameWorld != null)
            GameWorld.PredictionUpdate();
        return default;
    }
}

struct AcceptedConnectionStateComponent : ISystemStateComponentData
{
    public Entity playerEntity;
    public bool isReady;
}

public class ServerGameWorld
{
    public ServerGameWorld(World world)
    {
        m_GameWorld = world;

        m_PlayerModule = new PlayerModuleServer(m_GameWorld);

#pragma warning disable 618
        // we're keeping World.Active until we can properly remove them all
        var defaultWorld = World.Active;
        try
        {
            m_GameModeSystem = m_GameWorld.CreateSystem<GameModeSystemServer>(m_GameWorld, m_GameWorld.GetExistingSystem<ChatSystemServer>());

            m_HandleDamageGroup = m_GameWorld.CreateSystem<HandleDamageSystemGroup>();

            m_TeleporterSystem = m_GameWorld.CreateSystem<TeleporterSystemServer>();

            m_DamageAreaSystem = m_GameWorld.CreateSystem<DamageAreaSystemServer>();

            m_HandleControlledEntityChangedGroup = m_GameWorld.CreateSystem<ManualComponentSystemGroup>();
            m_HandleControlledEntityChangedGroup.AddSystemToUpdateList(m_GameWorld.CreateSystem<PlayerCharacterControl.PlayerCharacterControlSystem>());

            m_PredictedUpdateGroup = m_GameWorld.CreateSystem<ManualComponentSystemGroup>();
            m_PredictedUpdateGroup.AddSystemToUpdateList(CharacterModule.CreateServerUpdateSystemGroup(world));
            m_PredictedUpdateGroup.AddSystemToUpdateList(world.CreateSystem<AbilityUpdateSystemGroup>());

            m_AfterPredictionUpdateGroup = m_GameWorld.CreateSystem<ManualComponentSystemGroup>();
            m_AfterPredictionUpdateGroup.AddSystemToUpdateList(CharacterModule.CreateServerPresentationSystemGroup(world));
            m_AfterPredictionUpdateGroup.AddSystemToUpdateList(m_GameWorld.GetOrCreateSystem(typeof(PartSystemUpdateGroup)));

        }
        finally
        {
            World.Active = defaultWorld;
        }
#pragma warning restore 618

        m_MoveableSystem = new MovableSystemServer(m_GameWorld);
        m_CameraSystem = new ServerCameraSystem();
    }

    public void Shutdown(bool isDestroyingWorld)
    {
        m_PlayerModule.Shutdown();

        // When destroying the world all systems will be torn down - so no need to do it manually
        if (!isDestroyingWorld)
        {
            m_HandleDamageGroup.DestroyGroup();

            m_GameWorld.DestroySystem(m_TeleporterSystem);
            m_GameWorld.DestroySystem(m_HandleControlledEntityChangedGroup);
            m_GameWorld.DestroySystem(m_PredictedUpdateGroup);
            m_GameWorld.DestroySystem(m_AfterPredictionUpdateGroup);
            m_GameWorld.DestroySystem(m_DamageAreaSystem);
        }

        m_CameraSystem.Shutdown();
        m_MoveableSystem.Shutdown();

        AnimationGraphHelper.Shutdown(m_GameWorld);

        m_GameWorld = null;
    }

    public void RespawnPlayer(Entity playerEntity)
    {
        var playerState = m_GameWorld.EntityManager.GetComponentData<Player.State>(playerEntity);
        if (playerState.controlledEntity == Entity.Null)
            return;

        if (m_GameWorld.EntityManager.HasComponent<Character.State>(playerState.controlledEntity))
            CharacterDespawnRequest.Create(m_GameWorld, playerState.controlledEntity);

        playerState.controlledEntity = Entity.Null;

        m_GameWorld.EntityManager.SetComponentData(playerEntity, playerState);
    }

    char[] _msgBuf = new char[256];
    public void HandlePlayerSetupEvent(Entity playerEntity, PlayerSettings settings, ChatSystemServer chatSystem)
    {
        var playerState = m_GameWorld.EntityManager.GetComponentData<Player.State>(playerEntity);
        if (playerState.playerName.ToString() !=  settings.playerName)
        {
            int l = 0;
            if (playerState.playerName.ToString() == "")
                l = StringFormatter.Write(ref _msgBuf, 0, "{0} joined", settings.playerName);
            else
                l = StringFormatter.Write(ref _msgBuf, 0, "{0} is now known as {1}", playerState.playerName.ToString(), settings.playerName);
            chatSystem.SendChatAnnouncement(new CharBufView(_msgBuf, l));
            playerState.playerName = new NativeString64(settings.playerName);

            m_GameWorld.EntityManager.SetComponentData(playerEntity, playerState);
        }

        var charControl = m_GameWorld.EntityManager.GetComponentData<PlayerCharacterControl.State>(playerEntity);
        charControl.requestedCharacterType = settings.characterType;

        m_GameWorld.EntityManager.SetComponentData(playerEntity,charControl);
    }

    public void HandleClientCommands()
    {
        var connectionQuery = m_GameWorld.EntityManager.CreateEntityQuery(
            ComponentType.ReadWrite<NetworkIdComponent>(),
            ComponentType.ReadWrite<CommandTargetComponent>());
        var commandTargets = connectionQuery.ToComponentDataArray<CommandTargetComponent>(Allocator.TempJob);
        for (int i = 0; i < commandTargets.Length; ++i)
        {
            var targetEntity = commandTargets[i].targetEntity;
            if (targetEntity == Entity.Null)
                continue;
            m_GameWorld.EntityManager.GetBuffer<UserCommand>(targetEntity)
                .GetDataAtTick(m_GameWorld.GetExistingSystem<ServerSimulationSystemGroup>().ServerTick, out var latestCommand);

            // Pass on command to controlled entity
            var playerState = m_GameWorld.EntityManager.GetComponentData<Player.State>(targetEntity);
            if (playerState.controlledEntity != Entity.Null)
            {
                var userCommand = m_GameWorld.EntityManager.GetComponentData<PlayerControlled.State>(
                    playerState.controlledEntity);

                userCommand.prevCommand = userCommand.command;
                userCommand.command = latestCommand;

                m_GameWorld.EntityManager.SetComponentData(playerState.controlledEntity, userCommand);
            }
        }
        commandTargets.Dispose();
    }

    public bool HandleClientCommand(Entity client, string v)
    {
        if (v == "nextchar")
        {
            GameDebug.Log("nextchar for client " + m_GameWorld.EntityManager.GetComponentData<NetworkIdComponent>(client).Value);
            m_GameModeSystem.RequestNextChar(m_GameWorld.EntityManager.GetComponentData<CommandTargetComponent>(client).targetEntity);
        }
        else
        {
            return false;
        }
        return true;
    }

    public void BeforePredictionUpdate()
    {
        var gameTimeSystem = m_GameWorld.GetExistingSystem<GameTimeSystem>();
        var time = gameTimeSystem.GetWorldTime();
        time.tick = (int)m_GameWorld.GetExistingSystem<ServerSimulationSystemGroup>().ServerTick;
        time.tickDuration = time.tickInterval;
        gameTimeSystem.SetWorldTime(time);
        gameTimeSystem.frameDuration = time.tickInterval;

        Profiler.BeginSample("HandleClientCommands");

        // This call backs into ProcessCommand
        HandleClientCommands();

        Profiler.EndSample();

        GameTime gameTime = new GameTime(gameTimeSystem.GetWorldTime().tickRate);
        gameTime.SetTime(gameTimeSystem.GetWorldTime().tick, gameTimeSystem.GetWorldTime().tickInterval);

        // Handle controlled entity changed
        m_HandleControlledEntityChangedGroup.Update();

        // Start movement of scene objects. Scene objects that player movement
        // depends on should finish movement in this phase
        m_MoveableSystem.Update();
        m_CameraSystem.Update();

        // Update movement of player controlled units
        m_TeleporterSystem.Update();
    }

    public void PredictionUpdate()
    {
        m_PredictedUpdateGroup.Update();
    }

    public void AfterPredictionUpdate()
    {

        m_DamageAreaSystem.Update();

        // Handle damage
        m_HandleDamageGroup.Update();

        // TODO (mogensh) for now we upadte this AFTER CharacterModule as we depend on AnimSourceCtrl to run before bodypart. Sort this out
        m_AfterPredictionUpdateGroup.Update();

        // Update gamemode. Run last to allow picking up deaths etc.
        m_GameModeSystem.Update();
    }

    public void HandleClientConnect(Entity client)
    {
        var entityManager = m_GameWorld.EntityManager;
        bool isReady = entityManager.GetComponentData<AcceptedConnectionStateComponent>(client).isReady;
        var playerEntity = m_PlayerModule.CreatePlayerEntity(m_GameWorld, entityManager.GetComponentData<NetworkIdComponent>(client).Value, 0, "", isReady);
        entityManager.AddBuffer<UserCommand>(playerEntity);
        entityManager.SetComponentData(client, new CommandTargetComponent{targetEntity = playerEntity});
        entityManager.SetComponentData(client, new AcceptedConnectionStateComponent {playerEntity = playerEntity, isReady = isReady});
    }

    public void HandleClientDisconnect(EntityCommandBuffer ecb, Entity client)
    {
        var entityManager = m_GameWorld.EntityManager;
        var playerEntity = entityManager.GetComponentData<AcceptedConnectionStateComponent>(client).playerEntity;
        if (playerEntity == Entity.Null)
            return;

        CharacterModule.ServerCleanupPlayer(m_GameWorld, ecb, playerEntity);
        m_PlayerModule.CleanupPlayer(playerEntity);
    }

    // Internal systems
    World m_GameWorld;
    readonly PlayerModuleServer m_PlayerModule;

    readonly ServerCameraSystem m_CameraSystem;
    readonly GameModeSystemServer m_GameModeSystem;

    readonly ManualComponentSystemGroup m_HandleControlledEntityChangedGroup;
    readonly ManualComponentSystemGroup m_PredictedUpdateGroup;
    readonly ManualComponentSystemGroup m_AfterPredictionUpdateGroup;

    readonly DamageAreaSystemServer m_DamageAreaSystem;
    readonly TeleporterSystemServer m_TeleporterSystem;

    readonly HandleDamageSystemGroup m_HandleDamageGroup;

    readonly MovableSystemServer m_MoveableSystem;
}


public class ServerGameLoop : Game.IGameLoop
{
    [ConfigVar(Name = "server.maxclients", DefaultValue = "100", Description = "Maximum allowed clients")]
    public static ConfigVar serverMaxClients;

    [ConfigVar(Name = "server.disconnecttimeout", DefaultValue = "30000", Description = "Timeout in ms. Server will kick clients after this interval if nothing has been heard.")]
    public static ConfigVar serverDisconnectTimeout;

    public static string[] CurrentArgs;
    private World m_World;
    public bool Init(string[] args)
    {
        // TODO (timj) create singleton for parameters instead
        CurrentArgs = args;
        m_World = ClientServerBootstrap.CreateServerWorld(GameBootStrap.DefaultWorld, "ServerWorld");
        CurrentArgs = null;
        return true;
    }
    public void Shutdown()
    {
        // TODO (timj) world shutdown order - the ecs worlds are already torn down when application quit is called
        if (m_World == null)
            return;
        m_World.GetExistingSystem<ServerGameLoopSystem>().Shutdown();
        m_World.Dispose();
    }

    public void Update()
    {
    }
    public void FixedUpdate()
    {
    }
    public void LateUpdate()
    {
    }
}
[UpdateInGroup(typeof(ServerInitializationSystemGroup))]
[AlwaysSynchronizeSystem]
public class ServerGameLoopSystem : JobComponentSystem
{
    [ConfigVar(Name = "server.printstatus", DefaultValue = "0", Description = "Print status line every <n> ticks")]
    public static ConfigVar serverPrintStatus;

    public void Shutdown()
    {
        m_StateMachine.SwitchTo(null, ServerState.Idle);
    }
    protected override void OnCreate()
    {
        var args = ServerGameLoop.CurrentArgs;
        if (args == null)
            args = new string[0];
        World.GetOrCreateSystem<SceneSystem>().BuildSettingsGUID = new Unity.Entities.Hash128("9635cffb5d7da422c922505e40219752");

        var tickRate = EntityManager.CreateEntity();
        EntityManager.AddComponentData(tickRate, new ClientServerTickRate
        {
            MaxSimulationStepsPerFrame = 4,
            // Hardcoded for now, should be a setting
            NetworkTickRate = Game.serverTickRate.IntValue / 3,
            SimulationTickRate = Game.serverTickRate.IntValue,
            TargetFrameRateMode = Game.IsHeadless() ? ClientServerTickRate.FrameRateMode.Sleep : ClientServerTickRate.FrameRateMode.BusyWait
        });

        m_ClientsQuery = EntityManager.CreateEntityQuery(ComponentType.ReadWrite<AcceptedConnectionStateComponent>(), ComponentType.ReadWrite<NetworkStreamConnection>());

        // Set up statemachine for ServerGame
        m_StateMachine = new StateMachine<ServerState>();
        m_StateMachine.Add(ServerState.Idle, null, UpdateIdleState, null);
        m_StateMachine.Add(ServerState.Loading, null, UpdateLoadingState, null);
        m_StateMachine.Add(ServerState.WaitSubscene, null, UpdateWaitSubscene, null);
        m_StateMachine.Add(ServerState.Active, EnterActiveState, UpdateActiveState, LeaveActiveState);

        m_StateMachine.SwitchTo(null,ServerState.Idle);

        var ep = NetworkEndPoint.AnyIpv4;
        ep.Port = (ushort) (NetworkConfig.serverPort.IntValue);
        World.GetOrCreateSystem<NetworkStreamReceiveSystem>().Listen(ep);

        var listenAddresses = NetworkUtils.GetLocalInterfaceAddresses();
        if (listenAddresses.Count > 0)
            Console.SetPrompt(listenAddresses[0] + ":" + NetworkConfig.serverPort.Value + "> ");
        GameDebug.Log("Listening on " + string.Join(", ", NetworkUtils.GetLocalInterfaceAddresses()) + " on port " + NetworkConfig.serverPort.IntValue);

#if UNITY_EDITOR
        if (Game.game.clientFrontend != null &&
            (!GameBootStrap.IsSingleLevelPlaymode ||
             ClientServerBootstrap.RequestedPlayType != ClientServerBootstrap.PlayType.ClientAndServer))
#else
        if (Game.game.clientFrontend != null)
#endif
        {
            var serverPanel = Game.game.clientFrontend.serverPanel;
            serverPanel.SetPanelActive(true);
            serverPanel.serverInfo.text += "Listening on:\n";
            foreach (var a in NetworkUtils.GetLocalInterfaceAddresses())
            {
                serverPanel.serverInfo.text += a + ":" + NetworkConfig.serverPort.IntValue + "\n";
            }
        }

        if (serverServerName.Value == "")
            serverServerName.Value = MakeServername();

        //m_ServerQueryProtocolServer = new SQP.SQPServer(NetworkConfig.serverSQPPort.IntValue > 0 ? NetworkConfig.serverSQPPort.IntValue : NetworkConfig.serverPort.IntValue + NetworkConfig.sqpPortOffset);


        m_GameWorld = World;
        World.CreateSystem<GameTimeSystem>();

        GameDebug.Log("Network server initialized");

        Console.AddCommand("load", CmdLoad, "Load a named scene", this.GetHashCode());
        Console.AddCommand("unload", CmdUnload, "Unload current scene", this.GetHashCode());
        Console.AddCommand("s_respawn", CmdRespawn, "Respawn character (usage : respawn playername|playerId)", this.GetHashCode());
        Console.AddCommand("servername", CmdSetServerName, "Set name of the server", this.GetHashCode());
        Console.AddCommand("list", CmdList, "List clients", this.GetHashCode());

#if UNITY_EDITOR
        if (GameBootStrap.IsSingleLevelPlaymode)
            m_StateMachine.SwitchTo(World, ServerState.Loading);
        else
#endif
            CmdLoad(args);
        InputSystem.SetMousePointerLock(false);

        m_ServerStartTime = (float)Time.ElapsedTime;

        GameDebug.Log("Server initialized");
        Console.SetOpen(false);
    }

    protected override void OnDestroy()
    {
        m_isDestroyingWorld = true;
        GameDebug.Log("ServerGameState shutdown");
        Console.RemoveCommandsWithTag(this.GetHashCode());

        m_StateMachine.Shutdown();

        #if UNITY_EDITOR
        if (!GameBootStrap.IsSingleLevelPlaymode)
        #endif
            Game.game.levelManager.UnloadLevel();

        PrefabAssetManager.Shutdown();
        m_GameWorld = null;
    }

    protected override JobHandle OnUpdate(JobHandle inputDeps)
    {
        int clientCount = m_ClientsQuery.CalculateEntityCount();
        if (serverRecycleInterval.FloatValue > 0.0f)
        {
            // Recycle server if time is up and no clients connected
            if (clientCount == 0 && (float)Time.ElapsedTime > m_ServerStartTime + serverRecycleInterval.FloatValue)
            {
                GameDebug.Log("Server exiting because recycle timeout was hit.");
                Console.EnqueueCommandNoHistory("quit");
            }
        }

        if (clientCount > m_MaxClients)
            m_MaxClients = clientCount;

        if (serverQuitWhenEmpty.IntValue > 0 && m_MaxClients > 0 && clientCount == 0)
        {
            GameDebug.Log("Server exiting because last client disconnected");
            Console.EnqueueCommandNoHistory("quit");
        }

        UpdateNetwork();
        m_StateMachine.Update();

        if (showGameLoopInfo.IntValue > 0)
            OnDebugDrawGameloopInfo();

        return default;
    }

    public ServerGameWorld GetServerGameWorld()
    {
        return m_serverGameWorld;
    }

    public void OnConnect(Entity client)
    {
        // TODO (timj) disconnect if server is full
        m_GameWorld.EntityManager.AddComponent<AcceptedConnectionStateComponent>(client);

        if (m_serverGameWorld != null)
            m_serverGameWorld.HandleClientConnect(client);
    }

    void UpdateNetwork()
    {
        Profiler.BeginSample("ServerGameLoop.UpdateNetwork");

        if(serverPrintStatus.IntValue > 0)
        {
            if ((Game.frameCount % serverPrintStatus.IntValue) == 0)
            {
                var playerCount = m_ClientsQuery.CalculateEntityCount();
                GameDebug.Log("ServerStatus: Map: {0}  Players: {1}/{2} (max: {3})", Game.game.levelManager.currentLevel.name, playerCount, ServerGameLoop.serverMaxClients.IntValue, m_MaxClients);
            }
        }

        /*
        // Update SQP data with current values
        var sid = m_ServerQueryProtocolServer.ServerInfoData;
        sid.BuildId = Game.game.buildId;
        sid.Port = (ushort)NetworkConfig.serverPort.IntValue;
        sid.CurrentPlayers = (ushort)m_ClientsQuery.CalculateEntityCount();
        sid.GameType = GameModeSystemServer.modeName.Value;
        sid.Map = Game.game.levelManager.currentLevel.name;
        sid.MaxPlayers = (ushort)ServerGameLoop.serverMaxClients.IntValue;
        sid.ServerName = serverServerName.Value;

        m_ServerQueryProtocolServer.Update();
        */

        Profiler.EndSample();
    }

    /// <summary>
    /// Idle state, no level is loaded
    /// </summary>
    void UpdateIdleState()
    {
    }

    /// <summary>
    /// Loading state, load in progress
    /// </summary>
    void UpdateLoadingState()
    {
        if (Game.game.levelManager.IsCurrentLevelLoaded())
            m_StateMachine.SwitchTo(m_GameWorld,ServerState.WaitSubscene);
    }

    void UpdateWaitSubscene()
    {
        // TODO (mogensh) we should find a better way to make sure subscene is loaded (this uses knowledge of what is in subscene)
        var query = m_GameWorld.EntityManager.CreateEntityQuery(typeof(HeroRegistry.RegistryEntity));
        var ready = query.CalculateEntityCount() > 0;
        query.Dispose();
        if(ready)
            m_StateMachine.SwitchTo(m_GameWorld,ServerState.Active);
    }



    /// <summary>
    /// Active state, level loaded
    /// </summary>
    void EnterActiveState()
    {
        GameDebug.Assert(m_serverGameWorld == null);

        m_serverGameWorld = new ServerGameWorld(m_GameWorld);
        var clients = m_ClientsQuery.ToEntityArray(Allocator.TempJob);
        for (int i = 0; i < clients.Length; ++i)
        {
            m_serverGameWorld.HandleClientConnect(clients[i]);
        }
        clients.Dispose();

        var entity = m_GameWorld.EntityManager.CreateEntity();// Game state entity
        m_GameWorld.EntityManager.AddComponentData(entity, new ActiveStateComponentData{MapName = new NativeString64(Game.game.levelManager.currentLevel.name)});
        m_GameWorld.GetExistingSystem<BeforeServerPredictionSystem>().GameWorld = m_serverGameWorld;
        m_GameWorld.GetExistingSystem<ServerPredictionSystem>().GameWorld = m_serverGameWorld;
        m_GameWorld.GetExistingSystem<AfterServerPredictionSystem>().GameWorld = m_serverGameWorld;
    }

    void UpdateActiveState()
    {
    }

    void LeaveActiveState()
    {
        if (Unity.Entities.World.AllWorlds.Contains(World))
        {
            m_GameWorld.GetExistingSystem<BeforeServerPredictionSystem>().GameWorld = null;
            m_GameWorld.GetExistingSystem<ServerPredictionSystem>().GameWorld = null;
            m_GameWorld.GetExistingSystem<AfterServerPredictionSystem>().GameWorld = null;
        }

        m_serverGameWorld.Shutdown(m_isDestroyingWorld);
        m_serverGameWorld = null;
    }

    void LoadLevel(string levelname, string gamemode = "deathmatch")
    {
        bool levelCanBeLoaded = Game.game.levelManager.CanLoadLevel(levelname);
        GameDebug.Assert(levelCanBeLoaded, "FATAL : Cannot load level : " + levelname);

        m_RequestedGameMode = gamemode;
        Game.game.levelManager.LoadLevel(levelname);

        m_StateMachine.SwitchTo(null,ServerState.Loading);
    }

    void UnloadLevel()
    {
        // TODO
    }

    void CmdSetServerName(string[] args)
    {
        if (args.Length > 0)
        {
            // TODO (petera) fix or remove this?
        }
        else
            Console.Write("Invalid argument to servername (usage : servername name)");
    }

    void CmdLoad(string[] args)
    {
        if (args.Length == 1)
            LoadLevel(args[0]);
        else if (args.Length == 2)
            LoadLevel(args[0], args[1]);
    }

    void CmdUnload(string[] args)
    {
        UnloadLevel();
    }

    void CmdRespawn(string[] args)
    {
        if (args.Length != 1)
        {
            Console.Write("Invalid argument for respawn command (usage : respawn playername|playerId)");
            return;
        }

        var playerId = -1;
        var playerName = args[0];
        var usePlayerId = int.TryParse(args[0], out playerId);

        var entityManager = m_GameWorld.EntityManager;
        var clients = m_ClientsQuery.ToEntityArray(Allocator.TempJob);
        bool found = false;
        for (int i = 0; i < clients.Length; ++i)
        {
            var playerEntity = entityManager.GetComponentData<AcceptedConnectionStateComponent>(clients[i]).playerEntity;
            if (playerEntity == Entity.Null)
                continue;

            var clientId = entityManager.GetComponentData<NetworkIdComponent>(clients[i]).Value;
            if (usePlayerId && clientId != playerId)
                continue;

            var playerState = m_GameWorld.EntityManager.GetComponentData<Player.State>(playerEntity);
            if (!usePlayerId && playerState.playerName.ToString() != playerName)
                continue;

            m_serverGameWorld.RespawnPlayer(playerEntity);
            found = true;
            break;
        }
        clients.Dispose();

        if(!found)
            GameDebug.Log("Could not find character. Unknown player, invalid character id or player doesn't have a character: " + args[0]);
    }

    void CmdList(string[] args)
    {
        Console.Write("Players on server:");
        Console.Write("-------------------");
        Console.Write(string.Format("   {0,2} {1,-15}", "ID", "PlayerName"));
        Console.Write("-------------------");
        var entityManager = m_GameWorld.EntityManager;
        var clients = m_ClientsQuery.ToEntityArray(Allocator.TempJob);
        for (int i = 0; i < clients.Length; ++i)
        {
            var clientId = entityManager.GetComponentData<NetworkIdComponent>(clients[i]).Value;
            var playerEntity = entityManager.GetComponentData<AcceptedConnectionStateComponent>(clients[i]).playerEntity;
            var playerName = "";
            var playerState = m_GameWorld.EntityManager.GetComponentData<Player.State>(playerEntity);
            playerName = playerState.playerName.ToString();
            Console.Write(string.Format("   {0:00} {1,-15} score: {2}", clientId, playerName, playerState.score));
        }
        Console.Write("-------------------");
        Console.Write(string.Format("Total: {0}/{0} players connected", clients.Length, ServerGameLoop.serverMaxClients.IntValue));
        clients.Dispose();
    }

    string MakeServername()
    {
        var f = new string[] { "Ultimate", "Furry", "Quick", "Laggy", "Hot", "Curious", "Flappy", "Sneaky", "Nested", "Deep", "Blue", "Hipster", "Artificial" };
        var l = new string[] { "Speedrun", "Fragfest", "Win", "Exception", "Prefab", "Scene", "Garbage", "System", "Souls", "Whitespace", "Dolphin" };
        return f[Random.Range(0, f.Length)] + " " + l[Random.Range(0, l.Length)];
    }

    void OnDebugDrawGameloopInfo()
    {
        //DebugOverlay.Write(2,2,"Server Gameloop Info:");

        //var y = 3;
        //DebugOverlay.Write(2, y++, "  Simulation time average : {0}", m_NetworkServer.simStats.simTime);
        //DebugOverlay.Write(2, y++, "  Simulation time stdev : {0}", m_NetworkServer.simStats.simTimeStdDev);
        //DebugOverlay.Write(2, y++, "  Simulation time peek : {0}", m_NetworkServer.simStats.simTimeMax);

        //y++;
        //DebugOverlay.Write(2, y++, "  Delta time average : {0}", m_NetworkServer.simStats.deltaTime);
        //DebugOverlay.Write(2, y++, "  Delta time stdev : {0}", m_NetworkServer.simStats.deltaTimeStdDev);
        //DebugOverlay.Write(2, y++, "  Delta time peek : {0}", m_NetworkServer.simStats.deltaTimeMax);

        //y += 2;
        //foreach (var clientId in m_NetworkServer.clients)
        //{
        //    var info = m_NetworkServer.GetClientConnectionInfo(clientId);
        //    DebugOverlay.Write(2, y++, "  addr: {0}  port: {1}  rtt: {2} ms", info.address, info.port, info.rtt);
        //}
    }

    // Statemachine
    enum ServerState
    {
        Idle,
        Loading,
        WaitSubscene,
        Active,
    }
    StateMachine<ServerState> m_StateMachine;

    World m_GameWorld;

    ServerGameWorld m_serverGameWorld;
    public double m_nextTickTime = 0;
    string m_RequestedGameMode = "deathmatch";

    //SQPServer m_ServerQueryProtocolServer;

#pragma warning disable 649
    [ConfigVar(Name = "show.gameloopinfo", DefaultValue = "0", Description = "Show gameloop info")]
    static ConfigVar showGameLoopInfo;

    [ConfigVar(Name = "server.quitwhenempty", DefaultValue = "0", Description = "If enabled, quit when last client disconnects.")]
    static ConfigVar serverQuitWhenEmpty;

    [ConfigVar(Name = "server.recycleinterval", DefaultValue = "0", Description = "Exit when N seconds old AND when 0 players. 0 means never.")]
    static ConfigVar serverRecycleInterval;

    [ConfigVar(Name = "debug.servertickstats", DefaultValue = "0", Description = "Show stats about how many ticks we run per Unity update (headless only)")]
    static ConfigVar debugServerTickStats;

    [ConfigVar(Name = "server.servername", DefaultValue = "", Description = "Servername")]
    static ConfigVar serverServerName;
#pragma warning restore 649

    float m_ServerStartTime;
    int m_MaxClients;
    EntityQuery m_ClientsQuery;
    private bool m_isDestroyingWorld;
}
