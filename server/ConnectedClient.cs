using System.Net.Sockets;

namespace ChatServer;

public class ConnectedClient
{
    private readonly TcpClient _client;
    public string Id { get; private set; }

    public ConnectedClient(TcpClient client, string id)
    {
        _client = client;
        Id = id;
    }

    public void Kick()
    {
        _client.Close();
    }

    public void Send(object data)
    {
        byte[] message = (byte[])data;
        _client.GetStream().Write(message, 0, message.Length);
    }

    public bool IsOpen()
    {
        return _client.Connected;
    }
}