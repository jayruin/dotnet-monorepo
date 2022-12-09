using System.Collections.Immutable;

namespace ImgProj.Models;

public sealed class Spread
{
    public required ImmutableArray<int> Left { get; init; }

    public required ImmutableArray<int> Right { get; init; }
}