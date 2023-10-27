namespace ChatServer;

class Program
{
    const int PORT_NO = 8300;

    static void Main(string[] args)
    {
        Host host = new(PORT_NO);
        host.Start();
    }
}
