namespace Epubs;

public class ContainerXmlNotFoundException : EpubException
{
    public ContainerXmlNotFoundException()
        : base("Container XML could not be found!")
    {
    }
}
