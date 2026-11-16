using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace SourceGenerators;

public sealed class ImmutableArrayEqualityComparer<T> : IEqualityComparer<ImmutableArray<T>>
{
    private static readonly Lazy<ImmutableArrayEqualityComparer<T>> _lazy = new(() => new());

    public static ImmutableArrayEqualityComparer<T> Instance => _lazy.Value;

    private ImmutableArrayEqualityComparer()
    {
    }

    public bool Equals(ImmutableArray<T> x, ImmutableArray<T> y)
    {
        return x.SequenceEqual(y);
    }

    public int GetHashCode(ImmutableArray<T> obj)
    {
        HashCode hashCode = new();
        foreach (T item in obj)
        {
            hashCode.Add(item);
        }
        return hashCode.ToHashCode();
    }
}
