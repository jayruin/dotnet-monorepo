using Microsoft.Extensions.Primitives;
using System;
using System.Collections.Generic;
using System.Linq;

namespace umm.Library;

public static class SearchQuery
{
    public static bool Matches(IReadOnlyDictionary<string, StringValues> searchQuery, IEnumerable<MetadataSearchField> searchFields)
    {
        return searchFields.All(f => f.ExactMatch
            ? MatchesExactly(searchQuery, f.Aliases, f.Values)
            : MatchesPartially(searchQuery, f.Aliases, f.Values));
    }

    public static bool MatchesExactly(IReadOnlyDictionary<string, StringValues> searchQuery, IEnumerable<string> searchKeys, IEnumerable<string> values)
        => Matches(searchQuery, searchKeys, values, (v, s) => v.Equals(s, StringComparison.Ordinal));

    public static bool MatchesPartially(IReadOnlyDictionary<string, StringValues> searchQuery, IEnumerable<string> searchKeys, IEnumerable<string> values)
        => Matches(searchQuery, searchKeys, values, (v, s) => v.Contains(s, StringComparison.OrdinalIgnoreCase));

    private static bool Matches(IReadOnlyDictionary<string, StringValues> searchQuery,
        IEnumerable<string> searchKeys,
        IEnumerable<string> values,
        Func<string, string, bool> valueMatchesSearchValue)
    {
        if (searchQuery.Count == 0) return true;
        StringValues allSearchValues = new();
        foreach (string searchKey in searchKeys)
        {
            if (!searchQuery.TryGetValue(searchKey, out StringValues searchValues)) continue;
            allSearchValues = StringValues.Concat(allSearchValues, searchValues);
        }
        if (allSearchValues.Count == 0) return true;
        foreach (string value in values)
        {
            if (string.IsNullOrWhiteSpace(value)) continue;
            foreach (string? searchValue in allSearchValues)
            {
                if (string.IsNullOrWhiteSpace(searchValue)) continue;
                if (valueMatchesSearchValue(value, searchValue)) return true;
            }
        }
        return false;
    }
}
