using System.Net;
using Unity.Networking.Transport;
using Unity.Collections;
using Unity.Sample.Core;
using EventType = Unity.Networking.Transport.NetworkEvent.Type;

public class SocketTransport : INetworkTransport
{
    public SocketTransport(int port = 0, int maxConnections = 16)
    {
        m_IdToConnection = new NativeArray<NetworkConnection>(maxConnections, Allocator.Persistent);

        var configParams = default(NetworkConfigParameter);
        configParams.disconnectTimeoutMS = ServerGameLoop.serverDisconnectTimeout.IntValue;
        configParams.connectTimeoutMS = NetworkParameterConstants.ConnectTimeoutMS;
        configParams.maxConnectAttempts = NetworkParameterConstants.MaxConnectAttempts;
        m_Socket = new UdpNetworkDriver(configParams);

        m_Socket.Bind(NetworkEndPoint.CreateIpv4(0, (ushort)port));
    }

    public int Connect(string ip, int port)
    {
        var connection = m_Socket.Connect(NetworkEndPoint.Parse(ip, (ushort)port));
        m_IdToConnection[connection.InternalId] = connection;
        return connection.InternalId;
    }

    public void Disconnect(int connection)
    {
        m_Socket.Disconnect(m_IdToConnection[connection]);
        m_IdToConnection[connection] = default(NetworkConnection);
    }

    public void Update()
    {
        m_Socket.ScheduleUpdate().Complete();
    }

    public bool NextEvent(ref TransportEvent e)
    {
        NetworkConnection connection;

        connection = m_Socket.Accept();
        if (connection.IsCreated)
        {
            e.type = TransportEvent.Type.Connect;
            e.connectionId = connection.InternalId;
            m_IdToConnection[connection.InternalId] = connection;
            return true;
        }

        DataStreamReader reader;
        var context = default(DataStreamReader.Context);
        var ev = m_Socket.PopEvent(out connection, out reader);

        if (ev == EventType.Empty)
            return false;

        int size = 0;
        if (reader.IsCreated)
        {
            GameDebug.Assert(m_Buffer.Length >= reader.Length);
            reader.ReadBytesIntoArray(ref context, ref m_Buffer, reader.Length);
            size = reader.Length;
        }

        switch (ev)
        {
            case EventType.Data:
                e.type = TransportEvent.Type.Data;
                e.data = m_Buffer;
                e.dataSize = size;
                e.connectionId = connection.InternalId;
                break;
            case EventType.Connect:
                e.type = TransportEvent.Type.Connect;
                e.connectionId = connection.InternalId;
                m_IdToConnection[connection.InternalId] = connection;
                break;
            case EventType.Disconnect:
                e.type = TransportEvent.Type.Disconnect;
                e.connectionId = connection.InternalId;
                break;
            default:
                return false;
        }

        return true;
    }

    public void SendData(int connectionId, byte[] data, int sendSize)
    {
        using (var sendStream = new DataStreamWriter(sendSize, Allocator.Persistent))
        {
            sendStream.Write(data, sendSize);
            m_IdToConnection[connectionId].Send(m_Socket, sendStream);
        }
    }

    public string GetConnectionDescription(int connectionId)
    {
        return ""; // TODO enable this once RemoteEndPoint is implemented m_Socket.RemoteEndPoint(m_IdToConnection[connectionId]).GetIp();
    }

    public void Shutdown()
    {
        m_Socket.Dispose();
        m_IdToConnection.Dispose();
    }

    byte[] m_Buffer = new byte[1024 * 8];
    UdpNetworkDriver m_Socket;
    NativeArray<NetworkConnection> m_IdToConnection;
}
