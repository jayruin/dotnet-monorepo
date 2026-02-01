using System;
using System.Collections.Generic;
using System.Linq;

namespace Utils;

public sealed class CollectionEqualityComparer<TCollection, TItem> : IEqualityComparer<TCollection>
    where TCollection : ICollection<TItem>
{
    private readonly IEqualityComparer<TItem> _itemComparer;

    public CollectionEqualityComparer(IEqualityComparer<TItem>? itemComparer = null)
    {
        _itemComparer = itemComparer ?? EqualityComparer<TItem>.Default;
    }

    public bool Equals(TCollection? x, TCollection? y)
    {
        if (ReferenceEquals(x, y)) return true;
        if (x is null || y is null) return false;
        return x.SequenceEqual(y, _itemComparer);
    }

    public int GetHashCode(TCollection obj)
    {
        return HashCode.Combine(obj);
    }
}
