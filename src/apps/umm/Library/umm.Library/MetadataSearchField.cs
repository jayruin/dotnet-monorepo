using System.Collections.Immutable;

namespace umm.Library;

public sealed class MetadataSearchField
{
    public required ImmutableArray<string> Aliases { get; init; }
    public required ImmutableArray<string> Values { get; init; }
    public required bool ExactMatch { get; init; }
}
