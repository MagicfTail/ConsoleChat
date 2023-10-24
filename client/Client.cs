using ConsoleIOUI;
using System.Net.Sockets;
using System.Text;
using ConsoleChat.Common;

namespace ChatClient;

partial class Client : ConsoleInterface
{
    private readonly TcpClient? _tcpClient;

    public Client(string displayName, string room, string ip, int port) : base(15)
    {
        // Create a TCPClient object at the IP and port no
        try
        {
            _tcpClient = new(ip, port);
        }
        catch (SocketException)
        {
            Console.WriteLine("Connection failed");
            Stop();
            return;
        }

        Thread thread = new(() =>
        {
            // Get the stream to read from and write to
            NetworkStream nwStream = _tcpClient.GetStream();

            // Handshake joining room
            byte[] bytesToSend = Encoding.ASCII.GetBytes($"room:{room}-disp:{displayName}");
            nwStream.Write(bytesToSend, 0, bytesToSend.Length);

            byte[] bytesToRead = new byte[_tcpClient.ReceiveBufferSize];
            int bytesRead = nwStream.Read(bytesToRead, 0, _tcpClient.ReceiveBufferSize);

            string response = Encoding.ASCII.GetString(bytesToRead, 0, bytesRead);

            if (response != "Accept")
            {
                Stop();
                return;
            }

            while (true)
            {
                try
                {
                    bytesToRead = new byte[_tcpClient.ReceiveBufferSize];
                    bytesRead = nwStream.Read(bytesToRead, 0, _tcpClient.ReceiveBufferSize);
                    response = Encoding.ASCII.GetString(bytesToRead, 0, bytesRead);

                    ChatMessage message = new(response);

                    AddMessage(message.Body, message.Sender);
                }
                catch
                {
                    Stop();
                    return;
                }
            }
        });

        thread.Start();
    }

    public override void UserInputHandler(string input)
    {
        byte[] data = Encoding.ASCII.GetBytes(input);

        ThreadPool.QueueUserWorkItem((x) =>
        {
            _tcpClient!.GetStream().Write(data, 0, input.Length);
        }, data);
    }

    public override void ExitHandler()
    {
        _tcpClient?.Close();
    }
}