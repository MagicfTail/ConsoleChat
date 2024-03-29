using System.Text.RegularExpressions;

namespace ConsoleChat.Common;

public partial class ChatMessage
{
    [GeneratedRegex("(.*?):(.*)")]
    private static partial Regex SenderBodyRegex();

    public string Sender { get; private set; }
    public string Body { get; private set; }

    public ChatMessage(string response)
    {
        Match match = SenderBodyRegex().Match(response);

        if (!match.Success)
        {
            throw new MalformedMessageException("Malformed message received");
        }

        Sender = match.Groups[1].Value;
        Body = match.Groups[2].Value;
    }
}