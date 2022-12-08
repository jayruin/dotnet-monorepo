namespace Epub;

public class InvalidEpubVersionException : EpubException
{
    public InvalidEpubVersionException()
        : base("Epub version is invalid!")
    {
    }
}
