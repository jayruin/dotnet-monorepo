using System.Collections.Immutable;

namespace ImgProj;

public interface IPageSpread
{
    public ImmutableArray<int> Left { get; }

    public ImmutableArray<int> Right { get; }

    public IPageSpread? RelativeTo(ImmutableArray<int> coordinates);
}
