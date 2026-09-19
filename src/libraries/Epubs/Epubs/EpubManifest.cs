using System.Collections.Immutable;

namespace Epubs;

public sealed class EpubManifest
{
    public required ImmutableArray<EpubManifestItem> Items { get; init; }
}
