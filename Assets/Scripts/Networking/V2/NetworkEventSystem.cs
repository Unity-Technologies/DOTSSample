using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.NetCode;
using Unity.Sample.Core;

public struct PlayerSettingsComponent : IComponentData
{
    public NativeString64 PlayerName;
    public int CharacterType;
    public short TeamId;
}

public struct PlayerReadyComponent : IComponentData
{
    // Prevent this from becoming a tag
    public int egh;
}

public struct RemoteCommandComponent : IComponentData
{
    public NativeString64 Command;
}

public struct IncomingRemoteCommandComponent : IComponentData
{
    public NativeString64 Command;
    public Entity Connection;
}

public struct ChatMessageComponent : IComponentData
{
    public NativeString512 Message;
    public byte Announcement;
    public int ClientId;
}

public struct IncomingChatMessageComponent : IComponentData
{
    public NativeString512 Message;
    public Entity Connection;
}

[UpdateInGroup(typeof(ClientSimulationSystemGroup))]
public class ClientEventSystem : JobComponentSystem
{
    private BeginSimulationEntityCommandBufferSystem m_CommandBuffer;
    private JobHandle m_JobHandle;
    private RpcQueue<RpcPlayerSetup> m_RpcSettingsQueue;
    private RpcQueue<RpcRemoteCommand> m_RpcRemoteCmdQueue;
    private EntityQuery m_ConnectionQuery;

    protected override void OnCreate()
    {
        m_CommandBuffer = World.GetOrCreateSystem<BeginSimulationEntityCommandBufferSystem>();
        m_RpcSettingsQueue = World.GetOrCreateSystem<RpcSystem>().GetRpcQueue<RpcPlayerSetup>();
        m_RpcRemoteCmdQueue = World.GetOrCreateSystem<RpcSystem>().GetRpcQueue<RpcRemoteCommand>();
        m_ConnectionQuery = GetEntityQuery(ComponentType.ReadOnly<NetworkStreamConnection>());
    }

    protected override void OnDestroy()
    {
    }

    struct SendPlayerSetupJob : IJobForEachWithEntity<PlayerSettingsComponent>
    {
        public EntityCommandBuffer CommandBuffer;
        public RpcQueue<RpcPlayerSetup> RpcQueue;
        public BufferFromEntity<OutgoingRpcDataStreamBufferComponent> RpcBuffer;
        // Deallocated in second job
        public NativeArray<Entity> ConnectionEntity;

        public void Execute(Entity entity, int index, ref PlayerSettingsComponent state)
        {
            GameDebug.Assert(ConnectionEntity.Length == 1, "Only one connection per client supported");
            var connectionEntity = ConnectionEntity[0];
            RpcQueue.Schedule(RpcBuffer[connectionEntity], new RpcPlayerSetup { TeamId = state.TeamId, CharacterType = state.CharacterType, PlayerName = state.PlayerName });
            CommandBuffer.DestroyEntity(entity);
        }
    }

    struct SendRemoteCommandJob : IJobForEachWithEntity<RemoteCommandComponent>
    {
        public EntityCommandBuffer CommandBuffer;
        public RpcQueue<RpcRemoteCommand> RpcQueue;
        public BufferFromEntity<OutgoingRpcDataStreamBufferComponent> RpcBuffer;
        [DeallocateOnJobCompletion] public NativeArray<Entity> ConnectionEntity;

        public void Execute(Entity entity, int index, ref RemoteCommandComponent state)
        {
            GameDebug.Assert(ConnectionEntity.Length == 1, "Only one connection per client supported");
            var connectionEntity = ConnectionEntity[0];
            RpcQueue.Schedule(RpcBuffer[connectionEntity], new RpcRemoteCommand { Command = state.Command });
            CommandBuffer.DestroyEntity(entity);
        }
    }

    public void SendPlayerSettingsRPC(PlayerSettings settings)
    {
        var entity = World.EntityManager.CreateEntity();
        GameDebug.Assert(settings.playerName.Length <= NativeString64.MaxLength);
        var playerSettingsComponent = new PlayerSettingsComponent
        {
            CharacterType = settings.characterType, TeamId = settings.teamId
        };
        playerSettingsComponent.PlayerName.CopyFrom(settings.playerName);

        World.EntityManager.AddComponentData(entity, playerSettingsComponent);
    }

    public void SendRemoteCommandRpc(NativeString64 command)
    {
        var entity = World.EntityManager.CreateEntity();
        var remoteCommand = new RemoteCommandComponent
        {
            Command = command
        };
        World.EntityManager.AddComponentData(entity, remoteCommand);
    }

    protected override JobHandle OnUpdate(JobHandle inputDeps)
    {
        JobHandle queryJob;
        var connection = m_ConnectionQuery.ToEntityArray(Allocator.TempJob, out queryJob);
        var sendSetupJob = new SendPlayerSetupJob {
            CommandBuffer = m_CommandBuffer.CreateCommandBuffer(),
            RpcQueue = m_RpcSettingsQueue,
            ConnectionEntity = connection,
            RpcBuffer = GetBufferFromEntity<OutgoingRpcDataStreamBufferComponent>()
        };
        var jobHandle = sendSetupJob.ScheduleSingle(this, JobHandle.CombineDependencies(inputDeps, queryJob));

        var remoteCommandJob = new SendRemoteCommandJob() {
            CommandBuffer = m_CommandBuffer.CreateCommandBuffer(),
            RpcQueue = m_RpcRemoteCmdQueue,
            ConnectionEntity = connection,
            RpcBuffer = GetBufferFromEntity<OutgoingRpcDataStreamBufferComponent>()
        };
        jobHandle = remoteCommandJob.ScheduleSingle(this, jobHandle);

        m_CommandBuffer.AddJobHandleForProducer(jobHandle);
        return jobHandle;
    }
}

[UpdateInGroup(typeof(ClientSimulationSystemGroup))]
public class ClientReadyEventSystem : JobComponentSystem
{
    private BeginSimulationEntityCommandBufferSystem m_CommandBuffer;
    private JobHandle m_JobHandle;
    private RpcQueue<RpcPlayerReady> m_RpcReadyQueue;
    private EntityQuery m_ReadyQuery;

    protected override void OnCreate()
    {
        m_CommandBuffer = World.GetOrCreateSystem<BeginSimulationEntityCommandBufferSystem>();
        m_RpcReadyQueue = World.GetOrCreateSystem<RpcSystem>().GetRpcQueue<RpcPlayerReady>();
        m_ReadyQuery = GetEntityQuery(ComponentType.ReadOnly<NetworkStreamConnection>());
    }

    protected override void OnDestroy()
    {
    }

    struct SendPlayerReadyJob : IJobForEachWithEntity<PlayerReadyComponent>
    {
        public EntityCommandBuffer CommandBuffer;
        public RpcQueue<RpcPlayerReady> RpcQueue;
        public BufferFromEntity<OutgoingRpcDataStreamBufferComponent> RpcBuffer;
        [DeallocateOnJobCompletion] public NativeArray<Entity> ConnectionEntity;

        public void Execute(Entity entity, int index, ref PlayerReadyComponent state)
        {
            GameDebug.Assert(ConnectionEntity.Length == 1, "Only one connection per client supported");
            var connectionEntity = ConnectionEntity[0];
            RpcQueue.Schedule(RpcBuffer[connectionEntity], new RpcPlayerReady());
            CommandBuffer.DestroyEntity(entity);
            CommandBuffer.AddComponent<NetworkStreamInGame>(ConnectionEntity[0]);
        }
    }

    public void SendPlayerReadyRpc()
    {
        var entity = World.EntityManager.CreateEntity();
        World.EntityManager.AddComponentData(entity, new PlayerReadyComponent());
    }

    protected override JobHandle OnUpdate(JobHandle inputDeps)
    {
        JobHandle queryJob;
        var sendReadyJob = new SendPlayerReadyJob
        {
            CommandBuffer = m_CommandBuffer.CreateCommandBuffer(),
            RpcQueue = m_RpcReadyQueue,
            ConnectionEntity = m_ReadyQuery.ToEntityArray(Allocator.TempJob, out queryJob),
            RpcBuffer = GetBufferFromEntity<OutgoingRpcDataStreamBufferComponent>()
        };

        var jobHandle = sendReadyJob.ScheduleSingle(this, JobHandle.CombineDependencies(inputDeps, queryJob));

        m_CommandBuffer.AddJobHandleForProducer(jobHandle);
        return jobHandle;
    }
}

[UpdateInGroup(typeof(ServerSimulationSystemGroup))]
[AlwaysSynchronizeSystem]
public class PlayerEventSystem : JobComponentSystem
{
    protected override JobHandle OnUpdate(JobHandle inputDeps)
    {
        var PostUpdateCommands = new EntityCommandBuffer(Allocator.TempJob);

        var serverGameLoop = World.GetExistingSystem<ServerGameLoopSystem>();

        // The player setup handling has to happen on the main thread (so can't be jobified atm)
        Entities.WithNativeDisableContainerSafetyRestriction(PostUpdateCommands)
            .WithoutBurst() // TODO: 'GetServerGameWorld' on a ServerGameLoopSystem which is a reference type not allowed
            .ForEach((Entity entity, ref PlayerSettingsComponent state,
            ref NetworkIdComponent netId, ref AcceptedConnectionStateComponent accepted) =>
        {
            var playerSettings = new PlayerSettings
                { characterType = state.CharacterType, teamId = state.TeamId, playerName = state.PlayerName.ToString() };

            serverGameLoop.GetServerGameWorld().HandlePlayerSetupEvent(accepted.playerEntity, playerSettings, World.GetExistingSystem<ChatSystemServer>());

            PostUpdateCommands.RemoveComponent(entity, typeof(PlayerSettingsComponent));
        }).Run();

        PostUpdateCommands.Playback(EntityManager);
        PostUpdateCommands.Dispose();

        PostUpdateCommands = new EntityCommandBuffer(Allocator.TempJob);

        Entities.WithNativeDisableContainerSafetyRestriction(PostUpdateCommands)
            .WithoutBurst() // TODO: Can be removed once Burst async compiles function pointers
            .ForEach((Entity entity, ref PlayerReadyComponent state,
            ref NetworkIdComponent netId, ref AcceptedConnectionStateComponent accepted) =>
        {
            accepted.isReady = true;
            PostUpdateCommands.RemoveComponent(entity, typeof(PlayerReadyComponent));
        }).Run();

        PostUpdateCommands.Playback(EntityManager);
        PostUpdateCommands.Dispose();


        PostUpdateCommands = new EntityCommandBuffer(Allocator.TempJob);

        Entities.WithNativeDisableContainerSafetyRestriction(PostUpdateCommands)
            .WithoutBurst()
            .ForEach((Entity entity, ref IncomingRemoteCommandComponent remoteCommand
            /*ref NetworkIdComponent netId, ref AcceptedConnectionStateComponent accepted*/) =>
        {
            Console.EnqueueCommandNoHistory(remoteCommand.Command.ToString());
            PostUpdateCommands.RemoveComponent(entity, typeof(IncomingRemoteCommandComponent));
        }).Run();

        PostUpdateCommands.Playback(EntityManager);
        PostUpdateCommands.Dispose();

        return default;
    }
}

[UpdateInGroup(typeof(ServerSimulationSystemGroup))]
[AlwaysSynchronizeSystem]
public class RemoteCommandSystem : JobComponentSystem
{
    protected override JobHandle OnUpdate(JobHandle inputDeps)
    {
        var PostUpdateCommands = new EntityCommandBuffer(Allocator.TempJob);

        Entities.WithNativeDisableContainerSafetyRestriction(PostUpdateCommands)
            .WithoutBurst() // TODO: Cannot access get world with Burst
            .ForEach((Entity entity, ref IncomingRemoteCommandComponent state) =>
        {
            var serverGameLoop = World.GetExistingSystem<ServerGameLoopSystem>();
            var netId = EntityManager.GetComponentData<NetworkIdComponent>(state.Connection);

            serverGameLoop.GetServerGameWorld().HandleClientCommand(entity, state.Command.ToString());

            PostUpdateCommands.RemoveComponent(entity, typeof(IncomingRemoteCommandComponent));
        }).Run();

        PostUpdateCommands.Playback(EntityManager);
        PostUpdateCommands.Dispose();

        return default;
    }
}

