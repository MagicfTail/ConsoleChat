namespace ChatServer;

class Program
{
    const string SERVER_IP = "::";
    const int PORT_NO = 8300;

    static void Main(string[] args)
    {
        Host host = new(SERVER_IP, PORT_NO);
        host.Start();
    }
}
