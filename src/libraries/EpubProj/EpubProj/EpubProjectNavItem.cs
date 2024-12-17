using System.Collections.Immutable;

namespace EpubProj;

internal sealed class EpubProjectNavItem : IEpubProjectNavItem
{
    public required string Text { get; init; }
    public required string Href { get; init; }
    public required ImmutableArray<IEpubProjectNavItem> Children { get; init; }
}
