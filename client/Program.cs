using System.CommandLine;
using System.Text.RegularExpressions;

namespace ChatClient;

partial class Program
{
    [GeneratedRegex("^[\\w]+$")]
    private static partial Regex ValidCharsOnlyRegex();

    static void Main(string[] args)
    {
        RootCommand rootCommand = new("Console Chat Client");

        Option<string> displayName = new(
            name: "name",
            description: "Your display name"
        );
        displayName.AddAlias("-d");

        Option<string> roomName = new(
            name: "room",
            description: "The name of the room to join/create"
        );
        roomName.AddAlias("-r");

        Option<string> server = new(
            name: "--server",
            description: "The url of the server. Don't set unless you're trying to join a custom host",
            getDefaultValue: () => "127.0.0.1"
        )
        { IsHidden = true };
        server.AddAlias("-s");

        Option<int> port = new(
            name: "--port",
            description: "The port on the server. Don't set unless you're trying to join a custom host",
            getDefaultValue: () => 5000
        )
        { IsHidden = true };
        port.AddAlias("-p");

        rootCommand.Add(displayName);
        rootCommand.Add(roomName);
        rootCommand.Add(server);
        rootCommand.Add(port);

        rootCommand.SetHandler((displayNameValue, roomValue, serverValue, portValue) =>
        {
            if (!ValidInput(ref displayNameValue, "Display") || !ValidInput(ref roomValue, "Room"))
            {
                return;
            }

            Client client = new(displayNameValue, roomValue, serverValue, portValue);

            client.Start();
        }, displayName, roomName, server, port);

        rootCommand.Invoke(args);
    }

    private static bool ValidInput(ref string input, string type)
    {
        if (string.IsNullOrEmpty(input))
        {
            Console.WriteLine($"{type} Name:");
            input = Console.ReadLine() ?? "";
        }

        if (string.IsNullOrEmpty(input) || !ValidCharsOnlyRegex().IsMatch(input))
        {
            Console.WriteLine($"Invalid {type} Name. Must only contain letters, numbers, and underscores");
            return false;
        }

        return true;
    }
}
