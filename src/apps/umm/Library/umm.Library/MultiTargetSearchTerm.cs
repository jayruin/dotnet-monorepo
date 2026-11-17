using System;
using System.Collections.Immutable;

namespace umm.Library;

public closed class MultiTargetSearchTerm : SearchTerm
{
    public required ImmutableArray<SearchTerm> Targets
    {
        get;
        init
        {
            if (value.IsDefaultOrEmpty)
            {
                throw new ArgumentException("Collection cannot be empty.", nameof(Targets));
            }
            field = value;
        }
    }
}
