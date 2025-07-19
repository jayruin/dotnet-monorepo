using System.Collections.Immutable;

namespace Epubs;

internal sealed class EpubContents
{
    public required int Version { get; init; }
    public required ImmutableArray<string> MimetypeFilePath { get; init; }
    public required ImmutableArray<string> OpfFilePath { get; init; }
    public required ImmutableArray<string> CoverFilePath { get; init; }
    public required ImmutableArray<ImmutableArray<string>> DirectoryPaths { get; init; }
    public required ImmutableArray<ImmutableArray<string>> FilePaths { get; init; }
}
