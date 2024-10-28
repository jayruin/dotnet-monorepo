using System.Collections.Immutable;

namespace ImgProj;

public interface IPageSpread
{
    ImmutableArray<int> Left { get; }
    ImmutableArray<int> Right { get; }
    IPageSpread? RelativeTo(ImmutableArray<int> coordinates);
}
