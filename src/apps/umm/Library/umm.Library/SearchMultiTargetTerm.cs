using System;
using System.Collections.Immutable;

namespace umm.Library;

public closed class SearchMultiTargetTerm : SearchTerm
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
