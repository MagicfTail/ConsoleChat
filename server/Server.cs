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

    private readonly Dictionary<string, RoomInstance> _rooms;
    private readonly object _roomsLock = new();

    private readonly int _port;

    public Host(int port)
    {
        _rooms = new();
        _port = port;
    }

    public void Start()
    {
        // Start the server and wait for connections
        IPAddress localAdd = IPAddress.Any;
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

                ConnectedClient connectedClient = new(client, sender);

                RoomInstance room = HandleRoom(roomIdentifier);

                if (!room.AddClient(connectedClient))
                {
                    return;
                }

                byte[] buffer = new byte[client.ReceiveBufferSize];
                try
                {
                    while (true)
                    {
                        // Read the incoming stream into the buffer
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

    private RoomInstance HandleRoom(string roomIdentifier)
    {
        lock (_rooms)
        {
            if (!_rooms.ContainsKey(roomIdentifier))
            {
                RoomInstance instance = new(roomIdentifier);
                _rooms[roomIdentifier] = instance;
            }
        }

        return _rooms[roomIdentifier];
    }
}