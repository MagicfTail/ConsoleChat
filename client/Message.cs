using System.Text.RegularExpressions;

namespace ChatClient;

partial class Message
{
    [GeneratedRegex("(.*?):(.*)")]
    private static partial Regex SenderBodyRegex();

    public string Sender { get; private set; }
    public string Body { get; private set; }

    public Message(string response)
    {
        Match match = SenderBodyRegex().Match(response);

        if (!match.Success)
        {
            throw new MalformedMessageError("Malformed message received");
        }

        Sender = match.Groups[1].Value;
        Body = match.Groups[2].Value;
    }
}