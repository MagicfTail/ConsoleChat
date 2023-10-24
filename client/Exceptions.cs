namespace ChatClient;

class HandshakeFailedException : Exception
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

class MalformedMessageError : Exception
{
    public MalformedMessageError()
    {
    }

    public MalformedMessageError(string message)
        : base(message)
    {
    }

    public MalformedMessageError(string message, Exception inner)
        : base(message, inner)
    {
    }
}