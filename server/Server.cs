using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;

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
            //---incoming client connected---
            TcpClient client = listener.AcceptTcpClient();

            Thread thread = new(() =>
            {
                //---get the incoming data through a network stream---
                NetworkStream nwStream = client.GetStream();
                byte[] buffer = new byte[client.ReceiveBufferSize];

                //---read incoming stream---
                int bytesRead = nwStream.Read(buffer, 0, client.ReceiveBufferSize);

                //---convert the data received into a string---
                string serverIdentifierString = Encoding.ASCII.GetString(buffer, 0, bytesRead);

                Match match = HandshakeRegex().Match(serverIdentifierString);

                if (!match.Success)
                {
                    client.Close();
                    return;
                }

                string roomIdentifier = match.Groups[1].Value;
                string sender = match.Groups[2].Value;

                lock (_rooms)
                {
                    if (!_rooms.ContainsKey(roomIdentifier))
                    {
                        RoomInstance instance = new(roomIdentifier);
                        _rooms[roomIdentifier] = instance;
                    }
                }

                RoomInstance room = _rooms[roomIdentifier];

                if (!room.AddClient(client, sender))
                {
                    client.GetStream().Write(DupeDisplayNameError, 0, DupeDisplayNameError.Length);
                    client.Close();
                    return;
                }

                client.GetStream().Write(ConnectionSucceeded, 0, ConnectionSucceeded.Length);

                room.SendMessage("", $"New user connected: {sender}");

                try
                {
                    while (true)
                    {
                        //---read incoming stream---
                        bytesRead = nwStream.Read(buffer, 0, client.ReceiveBufferSize);

                        //---convert the data received into a string---
                        string message = Encoding.ASCII.GetString(buffer, 0, bytesRead);

                        room.SendMessage(sender, message);
                    }
                }
                catch (IOException)
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
            });

            thread.Start();
        }
    }
}