using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace EpubProj;

internal sealed class MutableNavItem
{
    public required string Text { get; set; }
    public required string Href { get; set; }
    public required List<MutableNavItem> Children { get; set; }

    public IEpubProjectNavItem ToImmutable() => new EpubProjectNavItem()
    {
        Text = Text,
        Href = Href,
        Children = Children.Select(ni => ni.ToImmutable()).ToImmutableArray(),
    };
}
