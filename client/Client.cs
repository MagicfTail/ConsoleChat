using ConsoleIOUI;
using System.Net.Sockets;
using System.Text;
using ConsoleChat.Common;

namespace ChatClient;

partial class Client : ConsoleInterface
{
    private const string localAbortString = "An established connection was aborted by the software in your host machine.";

    private TcpClient? _tcpClient;
    private NetworkStream? _nwStream;
    private readonly Thread _thread;

    private readonly string _ip;
    private readonly int _port;

    private string error = "";

    public Client(string displayName, string room, string ip, int port) : base(15)
    {
        _ip = ip;
        _port = port;

        _thread = new(() =>
        {
            if (_tcpClient == null || _nwStream == null)
            {
                return;
            }

            // Handshake joining room
            byte[] bytesToSend = Encoding.ASCII.GetBytes($"room:{room}-disp:{displayName}");
            _nwStream.Write(bytesToSend, 0, bytesToSend.Length);

            byte[] bytesToRead = new byte[_tcpClient.ReceiveBufferSize];
            int bytesRead = _nwStream.Read(bytesToRead, 0, _tcpClient.ReceiveBufferSize);

            string response = Encoding.ASCII.GetString(bytesToRead, 0, bytesRead);

            if (response != "Accept")
            {
                Stop();
                error = response;
                return;
            }

            try
            {
                while (true)
                {
                    bytesToRead = new byte[_tcpClient.ReceiveBufferSize];
                    bytesRead = _nwStream.Read(bytesToRead, 0, _tcpClient.ReceiveBufferSize);
                    response = Encoding.ASCII.GetString(bytesToRead, 0, bytesRead);

                    ChatMessage message = new(response);
                    AddMessage(message.Body, message.Sender);
                }
            }
            catch (IOException ex)
            {
                if (ex.InnerException?.Message != localAbortString)
                {
                    error = "Connection to server dropped";
                }

                Stop();
            }
        })
        {
            IsBackground = true
        };
    }

    public override void UserInputHandler(string input)
    {
        byte[] data = Encoding.ASCII.GetBytes(input);

        ThreadPool.QueueUserWorkItem((x) =>
        {
            _tcpClient?.GetStream().Write(data, 0, input.Length);
        }, data);
    }

    public override void EntranceHandler()
    {
        // Create a TCPClient object at the IP and port no, and get stream
        try
        {
            _tcpClient = new(_ip, _port);
            _nwStream = _tcpClient.GetStream();
        }
        catch (SocketException ex)
        {
            error = "Connection failed: " + ex.Message;
        }

        if (_tcpClient == null || _nwStream == null || _thread == null)
        {
            Stop();
            return;
        }

        _thread.Start();
    }

    public override void ExitHandler()
    {
        _nwStream?.Close();
        _tcpClient?.Close();

        if (_thread.IsAlive)
        {
            _thread.Join();
        }

        if (error != "")
        {
            Console.WriteLine(error);
        }
    }
}