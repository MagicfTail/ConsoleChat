namespace ConsoleChat.Common;

public class HandshakeFailedException : Exception
{
    public HandshakeFailedException()
    {
    }

    public HandshakeFailedException(string message)
        : base(message)
    {
    }

    public HandshakeFailedException(string message, Exception inner)
        : base(message, inner)
    {
    }
}

public class MalformedMessageException : Exception
{
    public MalformedMessageException()
    {
    }

    public MalformedMessageException(string message)
        : base(message)
    {
    }

    public MalformedMessageException(string message, Exception inner)
        : base(message, inner)
    {
    }
}