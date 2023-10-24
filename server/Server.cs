using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using ConsoleChat.Common;

namespace ChatServer;

partial class Host
{
    [GeneratedRegex("room:(.*)-disp:(.*)")]
    private static partial Regex HandshakeRegex();

    private readonly byte[] DupeDisplayNameError = Encoding.ASCII.GetBytes("User with chosen Display Name already exists in Room");
    private readonly byte[] ConnectionSucceeded = Encoding.ASCII.GetBytes("Accept");

    private readonly Dictionary<string, RoomInstance> _rooms;
    private readonly object _roomsLock = new();

    private readonly string _ip;
    private readonly int _port;

    public Host(string ip, int port)
    {
        _rooms = new();
        _ip = ip;
        _port = port;
    }

    public void Start()
    {
        //---listen at the specified IP and port no.---
        IPAddress localAdd = IPAddress.Parse(_ip);
        TcpListener listener = new(localAdd, _port);
        Console.WriteLine("Listening...");
        listener.Start();

        while (true)
        {
            // Wait for a client to connect
            TcpClient client = listener.AcceptTcpClient();

            Task.Factory.StartNew(() =>
            {
                // Open a stream for the data
                NetworkStream nwStream = client.GetStream();

                string hostIdentifierString = GetHostIdentifier(nwStream, client);

                // Match the message with the handshake
                Match match = HandshakeRegex().Match(hostIdentifierString);
                if (!match.Success)
                {
                    client.Close();
                    return;
                }

                // Get the Room and Sender
                string roomIdentifier = match.Groups[1].Value;
                string sender = match.Groups[2].Value;

                if (!HandleClientAndRoom(client, roomIdentifier, sender))
                {
                    client.GetStream().Write(DupeDisplayNameError, 0, DupeDisplayNameError.Length);
                    client.Close();
                    return;
                }

                RoomInstance room = _rooms[roomIdentifier];

                client.GetStream().Write(ConnectionSucceeded, 0, ConnectionSucceeded.Length);

                room.SendMessage("", $"New user connected: {sender}");

                try
                {
                    while (true)
                    {
                        byte[] buffer = new byte[client.ReceiveBufferSize];
                        //---read incoming stream---
                        int bytesRead = nwStream.Read(buffer, 0, client.ReceiveBufferSize);

                        //---convert the data received into a string---
                        string response = Encoding.ASCII.GetString(buffer, 0, bytesRead);

                        if (string.IsNullOrEmpty(response))
                        {
                            throw new MalformedMessageException();
                        }

                        room.SendMessage(sender, response);
                    }
                }
                catch (Exception ex) when (ex is IOException || ex is MalformedMessageException)
                {
                    lock (_roomsLock)
                    {
                        _rooms[roomIdentifier].RemoveClient(sender);

                        if (_rooms[roomIdentifier].IsEmpty())
                        {
                            _rooms.Remove(roomIdentifier);
                        }
                    }
                }
            }, TaskCreationOptions.LongRunning);
        }
    }

    private string GetHostIdentifier(NetworkStream nwStream, TcpClient client)
    {
        byte[] buffer = new byte[client.ReceiveBufferSize];

        // Read the message
        int bytesRead = nwStream.Read(buffer, 0, client.ReceiveBufferSize);

        // Convert the message into a string
        return Encoding.ASCII.GetString(buffer, 0, bytesRead);
    }

    private bool HandleClientAndRoom(TcpClient client, string roomIdentifier, string sender)
    {
        lock (_rooms)
        {
            if (!_rooms.ContainsKey(roomIdentifier))
            {
                RoomInstance instance = new(roomIdentifier);
                _rooms[roomIdentifier] = instance;
            }
        }

        // Get the room, and add the new client to it
        RoomInstance room = _rooms[roomIdentifier];
        return room.AddClient(client, sender);
    }
}