namespace umm.Library;

public sealed class MediaExportTarget
{
    public MediaExportTarget(string mediaType, bool supportsFile, bool supportsDirectory)
    {
        MediaType = mediaType;
        SupportsFile = supportsFile;
        SupportsDirectory = supportsDirectory;
    }

    public string MediaType { get; }
    public bool SupportsFile { get; }
    public bool SupportsDirectory { get; }
}
