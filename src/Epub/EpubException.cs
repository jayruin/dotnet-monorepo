using System;

namespace Epub;

public class EpubException : Exception
{
    public EpubException()
    {
    }

    public EpubException(string message)
        : base(message)
    {
    }

    public EpubException(string message, Exception inner)
        : base(message, inner)
    {
    }
}
