using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Networking.Transport;
using Unity.NetCode;
using Unity.Sample.Core;

[BurstCompile]
public struct RpcInitializeMap : IRpcCommand
{
    public NativeString64 MapName;

    public void Serialize(DataStreamWriter writer)
    {
        writer.Write(MapName.LengthInBytes);
        unsafe
        {
            fixed (byte* b = &MapName.buffer.byte0000)
            {
                writer.WriteBytes(b, MapName.LengthInBytes);
            }
        }
    }

    public void Deserialize(DataStreamReader reader, ref DataStreamReader.Context ctx)
    {
        var nameLength = reader.ReadUShort(ref ctx);
        GameDebug.Assert(nameLength <= NativeString64.MaxLength);
        MapName.LengthInBytes = nameLength;
        unsafe
        {
            fixed (byte* b = &MapName.buffer.byte0000)
            {
                reader.ReadBytes(ref ctx, b, MapName.LengthInBytes);
            }
        }
    }
    [BurstCompile]
    private static void InvokeExecute(ref RpcExecutor.Parameters parameters)
    {
        var rpcData = default(RpcInitializeMap);
        rpcData.Deserialize(parameters.Reader, ref parameters.ReaderContext);

        var ent = parameters.CommandBuffer.CreateEntity(parameters.JobIndex);
        parameters.CommandBuffer.AddComponent(parameters.JobIndex, ent,
            new ActiveStateComponentData {MapName = rpcData.MapName});
    }
    public PortableFunctionPointer<RpcExecutor.ExecuteDelegate> CompileExecute()
    {
        return new PortableFunctionPointer<RpcExecutor.ExecuteDelegate>(InvokeExecute);
    }
}
class InitializeMapRpcCommandRequestSystem : RpcCommandRequestSystem<RpcInitializeMap>
{
}

[BurstCompile]
public struct RpcPlayerSetup : IRpcCommand
{
    public NativeString64 PlayerName;
    public int CharacterType;
    public short TeamId;

    public void Serialize(DataStreamWriter writer)
    {
        writer.Write(TeamId);
        writer.Write(CharacterType);
        writer.Write(PlayerName.LengthInBytes);
        unsafe
        {
            fixed (byte* b = &PlayerName.buffer.byte0000)
            {
                writer.WriteBytes(b, PlayerName.LengthInBytes);
            }
        }
    }

    public void Deserialize(DataStreamReader reader, ref DataStreamReader.Context ctx)
    {
        TeamId = reader.ReadShort(ref ctx);
        CharacterType = reader.ReadInt(ref ctx);
        var nameLength = reader.ReadUShort(ref ctx);
        GameDebug.Assert(nameLength <= NativeString64.MaxLength);
        PlayerName.LengthInBytes = nameLength;
        unsafe
        {
            fixed (byte* b = &PlayerName.buffer.byte0000)
            {
                reader.ReadBytes(ref ctx, b, PlayerName.LengthInBytes);
            }
        }
    }
    [BurstCompile]
    private static void InvokeExecute(ref RpcExecutor.Parameters parameters)
    {
        var rpcData = default(RpcPlayerSetup);
        rpcData.Deserialize(parameters.Reader, ref parameters.ReaderContext);

        parameters.CommandBuffer.AddComponent(parameters.JobIndex, parameters.Connection,
            new PlayerSettingsComponent {CharacterType = rpcData.CharacterType, TeamId = rpcData.TeamId, PlayerName = rpcData.PlayerName});
    }
    public PortableFunctionPointer<RpcExecutor.ExecuteDelegate> CompileExecute()
    {
        return new PortableFunctionPointer<RpcExecutor.ExecuteDelegate>(InvokeExecute);
    }
}
class PlayerSetupRpcCommandRequestSystem : RpcCommandRequestSystem<RpcPlayerSetup>
{
}

[BurstCompile]
public struct RpcPlayerReady : IRpcCommand
{
    public void Serialize(DataStreamWriter writer)
    {
    }

    public void Deserialize(DataStreamReader reader, ref DataStreamReader.Context ctx)
    {
    }
    [BurstCompile]
    private static void InvokeExecute(ref RpcExecutor.Parameters parameters)
    {
        parameters.CommandBuffer.AddComponent(parameters.JobIndex, parameters.Connection, new PlayerReadyComponent());
        parameters.CommandBuffer.AddComponent(parameters.JobIndex, parameters.Connection, new NetworkStreamInGame());
    }
    public PortableFunctionPointer<RpcExecutor.ExecuteDelegate> CompileExecute()
    {
        return new PortableFunctionPointer<RpcExecutor.ExecuteDelegate>(InvokeExecute);
    }
}
class PlayerReadyRpcCommandRequestSystem : RpcCommandRequestSystem<RpcPlayerReady>
{
}

[BurstCompile]
public struct RpcRemoteCommand : IRpcCommand
{
    public NativeString64 Command;

    public void Serialize(DataStreamWriter writer)
    {
        writer.Write(Command.LengthInBytes);
        unsafe
        {
            fixed (byte* b = &Command.buffer.byte0000)
            {
                writer.WriteBytes(b, Command.LengthInBytes);
            }
        }
    }

    public void Deserialize(DataStreamReader reader, ref DataStreamReader.Context ctx)
    {
        var msgLength = reader.ReadUShort(ref ctx);
        GameDebug.Assert(msgLength <= NativeString64.MaxLength);
        Command.LengthInBytes = msgLength;
        unsafe
        {
            fixed (byte* b = &Command.buffer.byte0000)
            {
                reader.ReadBytes(ref ctx, b, Command.LengthInBytes);
            }
        }
    }

    [BurstCompile]
    private static void InvokeExecute(ref RpcExecutor.Parameters parameters)
    {
        var rpcData = default(RpcRemoteCommand);
        rpcData.Deserialize(parameters.Reader, ref parameters.ReaderContext);

        var req = parameters.CommandBuffer.CreateEntity(parameters.JobIndex);
        parameters.CommandBuffer.AddComponent(parameters.JobIndex, req, new IncomingRemoteCommandComponent{Command = rpcData.Command, Connection = parameters.Connection});
    }
    public PortableFunctionPointer<RpcExecutor.ExecuteDelegate> CompileExecute()
    {
        return new PortableFunctionPointer<RpcExecutor.ExecuteDelegate>(InvokeExecute);
    }
}
class RemoteCommandRpcCommandRequestSystem : RpcCommandRequestSystem<RpcRemoteCommand>
{
}

[BurstCompile]
public struct RpcChatMessage : IRpcCommand
{
    public NativeString512 Message;

    public void Serialize(DataStreamWriter writer)
    {
        GameDebug.Assert(writer.Capacity-writer.Length > Message.LengthInBytes*2, "Chat message too large (writer=" + (writer.Capacity-writer.Length) + " msg=" + Message.LengthInBytes*2);
        writer.Write(Message.LengthInBytes);
        unsafe
        {
            fixed (byte* b = &Message.buffer.byte0000)
            {
                writer.WriteBytes(b, Message.LengthInBytes);
            }
        }
    }

    public void Deserialize(DataStreamReader reader, ref DataStreamReader.Context ctx)
    {
        var msgLength = reader.ReadUShort(ref ctx);
        GameDebug.Assert(msgLength <= NativeString512.MaxLength);
        Message.LengthInBytes = msgLength;
        unsafe
        {
            fixed (byte* b = &Message.buffer.byte0000)
            {
                reader.ReadBytes(ref ctx, b, Message.LengthInBytes);
            }
        }
    }

    [BurstCompile]
    private static void InvokeExecute(ref RpcExecutor.Parameters parameters)
    {
        var rpcData = default(RpcChatMessage);
        rpcData.Deserialize(parameters.Reader, ref parameters.ReaderContext);

        var req = parameters.CommandBuffer.CreateEntity(parameters.JobIndex);
        parameters.CommandBuffer.AddComponent(parameters.JobIndex, req, new IncomingChatMessageComponent{Message = rpcData.Message, Connection = parameters.Connection});
    }
    public PortableFunctionPointer<RpcExecutor.ExecuteDelegate> CompileExecute()
    {
        return new PortableFunctionPointer<RpcExecutor.ExecuteDelegate>(InvokeExecute);
    }
}
class ChatMessageRpcCommandRequestSystem : RpcCommandRequestSystem<RpcChatMessage>
{
}
