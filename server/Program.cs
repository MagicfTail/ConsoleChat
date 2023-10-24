namespace ChatServer;

class Program
{
    const string SERVER_IP = "127.0.0.1";
    const int PORT_NO = 5000;

    static void Main(string[] args)
    {
        Host host = new(SERVER_IP, PORT_NO);
        host.Start();
    }
}
