using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using UnityEngine;
using Unity.NetCode;
using Unity.Sample.Core;

[UpdateInGroup(typeof(ServerSimulationSystemGroup))]
[AlwaysSynchronizeSystem]
public class ChatSystemServer : JobComponentSystem
{
    // TODO : The integration is annoying because we don't have a proper permanent place for player
    // info (world is destroyed for each level). We should try to make this smoother

    public void ResetChatTime()
    {
        m_StartTime = Game.Clock.ElapsedMilliseconds;
    }

    char[] _msgBuf = new char[256];
    public void SendChatAnnouncement(string message)
    {
        var c = Mathf.Min(256, message.Length);
        message.CopyTo(0, _msgBuf, 0, c);
        SendChatAnnouncement(new CharBufView(_msgBuf, c));
    }

    char[] _buf = new char[256];
    public void SendChatAnnouncement(CharBufView message)
    {
        var time = (Game.Clock.ElapsedMilliseconds - m_StartTime) / 1000;
        var minutes = (int)time / 60;
        var seconds = (int)time % 60;

        var formatted_length = StringFormatter.Write(ref _buf, 0, "<color=#ffffffff>[{0}:{1:00}]</color><color=#ffa500ff> {2}</color>", minutes, seconds, message);

        unsafe
        {
            fixed (char* ptr = message.buf)
            {
                GameDebug.Assert(message.length <= NativeString512.MaxLength);
                var msg = new NativeString512();
                msg.CopyFrom(ptr, (ushort)message.length);

                var connectionEntities = m_ConnectionQuery.ToEntityArray(Allocator.TempJob);
                for (int i = 0; i < connectionEntities.Length; ++i)
                {
                    var connectionEntity = connectionEntities[i];
                    m_RpcChatQueue.Schedule(EntityManager.GetBuffer<OutgoingRpcDataStreamBufferComponent>(connectionEntity), new RpcChatMessage { Message = msg });
                }

                connectionEntities.Dispose();
            }
        }
    }

    protected override void OnCreate()
    {
        m_ClientsQuery = EntityManager.CreateEntityQuery(ComponentType.ReadWrite<AcceptedConnectionStateComponent>(), ComponentType.ReadWrite<NetworkStreamConnection>());
        m_ConnectionQuery = GetEntityQuery(ComponentType.ReadOnly<NetworkStreamConnection>());
        m_RpcChatQueue = World.GetOrCreateSystem<RpcSystem>().GetRpcQueue<RpcChatMessage>();
    }
    protected override JobHandle OnUpdate(JobHandle inputDeps)
    {
        var connectionEntity = m_ConnectionQuery.ToEntityArray(Allocator.TempJob);

        // The chat system receive message handling has to happen on the main thread (so can't be jobified atm)
        var PostUpdateCommands = new EntityCommandBuffer(Allocator.TempJob);
        Entities
            .WithNativeDisableContainerSafetyRestriction(PostUpdateCommands)
            .WithoutBurst() // uses strings
            .ForEach((Entity entity, ref IncomingChatMessageComponent state) =>
        {
            for (int i = 0; i < connectionEntity.Length; ++i)
            {
                if (connectionEntity[i] == state.Connection)
                {
                    ReceiveMessage(connectionEntity[i], state.Message.ToString());
                }
            }
            PostUpdateCommands.RemoveComponent(entity, typeof(IncomingChatMessageComponent));
        }).Run();
        connectionEntity.Dispose();
        PostUpdateCommands.Playback(EntityManager);
        PostUpdateCommands.Dispose();
        return default;
    }
    private void ReceiveMessage(Entity from, string message)
    {
        ChatMessageType type;
        Entity target;

        var time = (Game.Clock.ElapsedMilliseconds - m_StartTime) / 1000;
        var minutes = time / 60;
        var seconds = time % 60;

        var text = ParseMessage(from, message, out type, out target);

        var fromPlayer = EntityManager.GetComponentData<AcceptedConnectionStateComponent>(from).playerEntity;
        var fromState = EntityManager.GetComponentData<Player.State>(fromPlayer);
        var fromName = fromState.playerName.ToString();

        if (type == ChatMessageType.Whisper)
        {
            if (target != Entity.Null)
            {
                var targetPlayer = EntityManager.GetComponentData<AcceptedConnectionStateComponent>(target).playerEntity;
                var targetState = EntityManager.GetComponentData<Player.State>(targetPlayer);
                var targetName = targetState.playerName.ToString();

                var fromLine = string.Format("<color=#ffffffff>[{0}:{1:00}]</color><color=#ff00ffff> [From {2}] {3}</color>", minutes, seconds, fromName, text);
                SendChatMessage(EntityManager.GetComponentData<NetworkIdComponent>(target).Value, fromLine);

                var toLine = string.Format("<color=#ffffffff>[{0}:{1:00}]</color><color=#ff00ffff> [To {2}] {3}</color>", minutes, seconds, targetName, text);
                SendChatMessage(EntityManager.GetComponentData<NetworkIdComponent>(from).Value, toLine);
            }
            else
                SendChatMessage(EntityManager.GetComponentData<NetworkIdComponent>(from).Value, string.Format("<color=#ff0000ff> Player not found</color>"));
        }
        else if (type == ChatMessageType.All || type == ChatMessageType.Team)
        {
            var marker = type == ChatMessageType.All ? "[All] " : "";

            var friendly = string.Format("[{0}:{1:00}] <color=#1D89CC>{2}{3}</color> {4}", minutes, seconds, marker, fromName, text);
            var hostile = string.Format("[{0}:{1:00}] <color=#FF3E3E>{2}{3}</color> {4}", minutes, seconds, marker, fromName, text);


            var fromTeamIndex = fromPlayer != Entity.Null ? fromState.teamIndex : -1;
            var clients = m_ClientsQuery.ToEntityArray(Allocator.TempJob);
            for (int i = 0; i < clients.Length; ++i)
            {
                var playerEntity = EntityManager.GetComponentData<AcceptedConnectionStateComponent>(clients[i])
                    .playerEntity;
                var netId = EntityManager.GetComponentData<NetworkIdComponent>(clients[i]).Value;
                var toState = EntityManager.GetComponentData<Player.State>(playerEntity);
                var targetTeamIndex = playerEntity != Entity.Null ? toState.teamIndex : -1;
                if (fromTeamIndex == targetTeamIndex)
                    SendChatMessage(netId, friendly);
                else if (type == ChatMessageType.All)
                    SendChatMessage(netId, hostile);
            }
            clients.Dispose();
        }
    }

    string ParseMessage(Entity from, string message, out ChatMessageType type, out Entity target)
    {
        type = ChatMessageType.All;
        target = Entity.Null;

        var match = m_CommandRegex.Match(message);
        if (match.Success)
        {
            var command = match.Groups[1].Value.ToLower();
            var actualMessage = match.Groups[2].Value;
            switch (command)
            {
                case "t":
                case "team":
                    type = ChatMessageType.Team;
                    return match.Groups[2].Value;

                case "w":
                case "whisper":
                    var match2 = m_TargetRegex.Match(actualMessage);
                    if (match2.Success)
                    {
                        type = ChatMessageType.Whisper;

                        // try to find client
                        var name =
 !String.IsNullOrEmpty(match2.Groups[1].Value) ? match2.Groups[1].Value : match2.Groups[2].Value;
                        var clients = m_ClientsQuery.ToEntityArray(Allocator.TempJob);
                        for (int i = 0; i < clients.Length; ++i)
                        {
                            var targetPlayer = EntityManager.GetComponentData<AcceptedConnectionStateComponent>(clients[i]).playerEntity;
                            var targetState = EntityManager.GetComponentData<Player.State>(targetPlayer);
                            var targetName = targetState.playerName.ToString();
                            if (targetName.Equals(name, StringComparison.InvariantCultureIgnoreCase))
                            {
                                target = clients[i];
                                m_ReplyTracker[target] = from;
                                return match2.Groups[3].Value;
                            }
                        }

                        clients.Dispose();
                    }
                    return actualMessage;

                case "r":
                case "reply":
                    if (m_ReplyTracker.TryGetValue(from, out target))
                    {
                        type = ChatMessageType.Whisper;
                        m_ReplyTracker[target] = from;
                    }
                    return actualMessage;
                case "a":
                case "all":
                default:
                    return actualMessage;
            }
        }
        return message;
    }

    public void SendChatMessage(int clientId, string message)
    {
        var connectionEntities = m_ConnectionQuery.ToEntityArray(Allocator.TempJob);
        for (int i = 0; i < connectionEntities.Length; ++i)
        {
            var connectionEntity = connectionEntities[i];
            if (EntityManager.GetComponentData<NetworkIdComponent>(connectionEntity).Value == clientId)
                m_RpcChatQueue.Schedule(EntityManager.GetBuffer<OutgoingRpcDataStreamBufferComponent>(connectionEntity), new RpcChatMessage { Message = new NativeString512(message) });
        }

        connectionEntities.Dispose();
    }

    long m_StartTime;

    Regex m_CommandRegex = new Regex(@"^/(\w+)\s+(.*)"); // e.g. "/all hey"
    Regex m_TargetRegex = new Regex(@"^(?:""(.*)""|([^\s]*))\s*(.+)"); // e.g. "some user" hey there

    Dictionary<Entity, Entity> m_ReplyTracker = new Dictionary<Entity, Entity>();
    EntityQuery m_ClientsQuery;
    private EntityQuery m_ConnectionQuery;
    private RpcQueue<RpcChatMessage> m_RpcChatQueue;
}
