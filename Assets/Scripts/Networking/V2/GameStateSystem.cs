using Unity.Collections;
using Unity.Entities;
using Unity.NetCode;

public struct InitializedPlayerEntity : IComponentData
{ }

public struct ActiveStateComponentData : IComponentData
{
    public NativeString64 MapName;
}

public struct InActiveStateTag : IComponentData
{
}

public struct MapAckedTag : IComponentData
{
}

//public struct GameState : IComponentData
//{}

public class NullCallbacks : INetworkCallbacks
{
    public void OnConnect(int clientId)
    {
    }

    public void OnDisconnect(int clientId)
    {
    }

    public void OnEvent(int clientId, NetworkEvent info)
    {
    }
}

[UpdateInGroup(typeof(ServerSimulationSystemGroup))]
[AlwaysUpdateSystem]
public class GameStateSystem : ComponentSystem
{
    private EntityQuery m_mapAckQuery;
    protected override void OnCreate()
    {
        //EntityManager.CreateEntity(typeof(GameMode));
        m_mapAckQuery = EntityManager.CreateEntityQuery(ComponentType.ReadWrite<MapAckedTag>());
    }

    protected override void OnUpdate()
    {
        // Creates player entity (Player.State) for new client
        Entities.WithNone<InitializedPlayerEntity>().ForEach((Entity entity, ref NetworkIdComponent networkId, ref NetworkStreamConnection connection) =>
        {
            UnityEngine.Debug.Log(">>>>> New network ID " + networkId.Value + " for " + connection.Value.InternalId);
            var serverLoop = World.GetExistingSystem<ServerGameLoopSystem>();

            serverLoop.OnConnect(entity);

            PostUpdateCommands.AddComponent(entity, typeof(InitializedPlayerEntity));
        });
        if (HasSingleton<ActiveStateComponentData>())
        {
            // Creates player entity (Player.State) for existing clients after level was loaded
            // TODO: Map name has to be sent with the netcode differently
            Entities.WithNone<InActiveStateTag>().ForEach((Entity entity, ref ActiveStateComponentData init) =>
            {
                // All connected clients must receive new map info
                PostUpdateCommands.RemoveComponent(m_mapAckQuery, ComponentType.ReadWrite<MapAckedTag>());

                PostUpdateCommands.AddComponent(entity, typeof(InActiveStateTag));
            });

            var activeState = GetSingleton<ActiveStateComponentData>();
            var rpcQueue = World.GetExistingSystem<RpcSystem>().GetRpcQueue<RpcInitializeMap>();
            Entities.WithNone<MapAckedTag>().WithAll<InitializedPlayerEntity>().ForEach(
                (Entity entity, DynamicBuffer<OutgoingRpcDataStreamBufferComponent> rpcBuffer, ref NetworkIdComponent networkId, ref NetworkStreamConnection connection) =>
                {
                    rpcQueue.Schedule(rpcBuffer, new RpcInitializeMap {MapName = activeState.MapName});
                    PostUpdateCommands.AddComponent(entity, typeof(MapAckedTag));
                });
        }
    }
}
