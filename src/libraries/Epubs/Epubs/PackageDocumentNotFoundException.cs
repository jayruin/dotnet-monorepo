namespace Epubs;

public class PackageDocumentNotFoundException : EpubException
{
    public PackageDocumentNotFoundException()
        : base("Package document could not be found!")
    {
    }
}
