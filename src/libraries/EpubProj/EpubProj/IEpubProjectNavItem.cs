using System.Collections.Immutable;

namespace EpubProj;

public interface IEpubProjectNavItem
{
    string Text { get; }
    string Href { get; }
    IImmutableList<IEpubProjectNavItem> Children { get; }
}
