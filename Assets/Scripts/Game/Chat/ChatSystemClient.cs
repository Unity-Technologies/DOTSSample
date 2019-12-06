using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.NetCode;

[UpdateInGroup(typeof(ClientPresentationSystemGroup))]
[AlwaysSynchronizeSystem]
public class ChatSystemClient : JobComponentSystem
{
    public Queue<string> incomingMessages = new Queue<string>();
    private RpcQueue<RpcChatMessage> m_RpcChatQueue;

    int m_LocalTeamIndex;
    public void UpdateLocalTeamIndex(int teamIndex)
    {
        m_LocalTeamIndex = teamIndex;
    }

    public void ReceiveMessage(string message)
    {
        // TODO (petera) this garbage factory must be killed with fire
        if (m_LocalTeamIndex == 1)
        {
            message = message.Replace("#1EA00001", "#1D89CCFF");
            message = message.Replace("#1EA00000", "#FF3E3EFF");
        }
        if (m_LocalTeamIndex == 0)
        {
            message = message.Replace("#1EA00000", "#1D89CCFF");
            message = message.Replace("#1EA00001", "#FF3E3EFF");
        }
        incomingMessages.Enqueue(message);
    }

    public void SendMessage(string message)
    {
        var connectionEntity = GetSingletonEntity<NetworkIdComponent>();
        m_RpcChatQueue.Schedule(EntityManager.GetBuffer<OutgoingRpcDataStreamBufferComponent>(connectionEntity),
            new RpcChatMessage { Message = new NativeString512(message) });
    }
    protected override void OnCreate()
    {
        m_RpcChatQueue = World.GetOrCreateSystem<RpcSystem>().GetRpcQueue<RpcChatMessage>();
    }
    protected override JobHandle OnUpdate(JobHandle inputDeps)
    {
        // The chat system receive message handling has to happen on the main thread (so can't be jobified atm)
        var PostUpdateCommands = new EntityCommandBuffer(Allocator.TempJob);
        Entities
            .WithNativeDisableContainerSafetyRestriction(PostUpdateCommands)
            .WithoutBurst() // managed data
            .ForEach((Entity entity, ref IncomingChatMessageComponent state) =>
        {
            ReceiveMessage(state.Message.ToString());
            PostUpdateCommands.RemoveComponent(entity, typeof(IncomingChatMessageComponent));
        }).Run();
        PostUpdateCommands.Playback(EntityManager);
        PostUpdateCommands.Dispose();
        return default;
    }
}
