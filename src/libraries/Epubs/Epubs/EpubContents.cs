using System.Collections.Immutable;

namespace Epubs;

internal sealed class EpubContents
{
    public required int Version { get; init; }
    public required EpubPath MimetypeFilePath { get; init; }
    public required EpubPath OpfFilePath { get; init; }
    public required EpubPath CoverFilePath { get; init; }
    public required EpubPath NcxFilePath { get; init; }
    public required ImmutableArray<EpubPath> XhtmlPaths { get; init; }
    public required ImmutableArray<EpubPath> FilePaths { get; init; }
}
