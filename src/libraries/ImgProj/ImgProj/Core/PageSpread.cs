using System;
using System.Collections.Immutable;

namespace ImgProj.Core;

internal sealed class PageSpread : IPageSpread
{
    private readonly ImmutableArray<int> _left;

    private readonly ImmutableArray<int> _right;

    public required ImmutableArray<int> Left
    {
        get => _left;
        init
        {
            if (value.Length == 0)
            {
                throw new ArgumentException("page coordinates cannot have length 0!", nameof(value));
            }
            _left = value;
        }
    }

    public required ImmutableArray<int> Right
    {
        get => _right;
        init
        {
            if (value.Length == 0)
            {
                throw new ArgumentException("page coordinates cannot have length 0!", nameof(value));
            }
            _right = value;
        }
    }

    public IPageSpread? RelativeTo(ImmutableArray<int> coordinates)
    {
        if (Math.Min(Left.Length, Right.Length) - 1 < coordinates.Length)
        {
            return null;
        }
        for (int i = 0; i < coordinates.Length; i++)
        {
            if (Left[i] != coordinates[i] || Right[i] != coordinates[i])
            {
                return null;
            }
        }
        return new PageSpread
        {
            Left = Left[coordinates.Length..],
            Right = Right[coordinates.Length..],
        };
    }
}
