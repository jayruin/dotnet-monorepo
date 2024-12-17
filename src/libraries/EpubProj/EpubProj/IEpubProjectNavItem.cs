using System.Collections.Immutable;

namespace EpubProj;

public interface IEpubProjectNavItem
{
    string Text { get; }
    string Href { get; }
    ImmutableArray<IEpubProjectNavItem> Children { get; }
}