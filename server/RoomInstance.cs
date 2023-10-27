using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace ChatServer;

public class RoomInstance
{
    private readonly byte[] ConnectionSucceeded = Encoding.ASCII.GetBytes("Accept");
    private readonly byte[] DupeDisplayNameError = Encoding.ASCII.GetBytes("User with chosen Display Name already exists in Room");

    private byte[] CurrentUsersString() => Encoding.ASCII.GetBytes($""":Current Users: {string.Join(", ", _clients.Keys)}""");

    private readonly Dictionary<string, ConnectedClient> _clients;
    private readonly object _clientsLock = new();

    public string Id { get; private set; }

    public RoomInstance(string id)
    {
        Id = id;
        _clients = new();
    }

    public void CloseRoom()
    {
        foreach (var c in _clients.Values)
        {
            c.Kick();
        }
    }

    public bool AddClient(ConnectedClient cClient)
    {
        lock (_clientsLock)
        {
            foreach (var c in _clients.Values)
            {
                if (c.Id == cClient.Id)
                {
                    cClient.Send(DupeDisplayNameError);
                    cClient.Kick();

                    return false;
                }
            }

            _clients.Add(cClient.Id, cClient);
        }

        cClient.Send(ConnectionSucceeded);

        SendMessage("", $"New user connected: {cClient.Id}");

        return true;
    }

    public void RemoveClient(string id)
    {
        lock (_clientsLock)
        {
            _clients.Remove(id);
        }
    }

    public bool IsEmpty()
    {
        return _clients.Count == 0;
    }

    public void SendMessage(string sender, string body)
    {
        byte[] data = Encoding.ASCII.GetBytes($"{sender}:{body}");

        foreach (ConnectedClient client in _clients.Values)
        {
            if (client.IsOpen())
            {
                ThreadPool.QueueUserWorkItem(client.Send!, data);
            }
            else
            {
                _clients.Remove(client.Id);
            }
        }
    }
}