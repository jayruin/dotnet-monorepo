namespace Epubs;

public sealed class EpubPackageInfo
{
    public required int Version { get; init; }
    public required EpubPath OpfFilePath { get; init; }
    public required EpubCover? Cover { get; init; }
    public required IEpubMetadata Metadata { get; init; }
    public required EpubManifest Manifest { get; init; }
    public required EpubSpine Spine { get; init; }
}
