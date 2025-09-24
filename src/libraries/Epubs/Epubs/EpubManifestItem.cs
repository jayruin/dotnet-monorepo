using System.Collections.Immutable;

namespace Epubs;

public sealed class EpubManifestItem
{
    public required string Id { get; init; }
    public required string AbsoluteHref { get; init; }
    public required string MediaType { get; init; }
    public required ImmutableArray<string> Properties { get; init; }
}
