using Microsoft.Extensions.Primitives;
using System;
using System.Collections.Generic;
using System.Linq;

namespace umm.Library;

public static class SearchQuery
{
    public static bool Matches(IReadOnlyDictionary<string, StringValues> searchQuery, IEnumerable<MetadataSearchField> searchFields)
        => searchFields.All(f => f.ExactMatch
            ? MatchesExactly(searchQuery, f.Aliases, f.Values)
            : MatchesPartially(searchQuery, f.Aliases, f.Values));

    public static bool Matches(string searchTerm, IEnumerable<MetadataSearchField> searchFields)
        => searchFields.Any(f => f.ExactMatch
            ? MatchesExactly(searchTerm, f.Values)
            : MatchesPartially(searchTerm, f.Values));

    public static bool MatchesExactly(IReadOnlyDictionary<string, StringValues> searchQuery, IEnumerable<string> searchKeys, IEnumerable<string> values)
        => Matches(searchQuery, searchKeys, values, (v, s) => v.Equals(s, StringComparison.Ordinal));

    public static bool MatchesExactly(string searchTerm, IEnumerable<string> values)
        => Matches(searchTerm, values, (v, s) => v.Equals(s, StringComparison.Ordinal));

    public static bool MatchesPartially(IReadOnlyDictionary<string, StringValues> searchQuery, IEnumerable<string> searchKeys, IEnumerable<string> values)
        => Matches(searchQuery, searchKeys, values, (v, s) => v.Contains(s, StringComparison.OrdinalIgnoreCase));

    public static bool MatchesPartially(string searchTerm, IEnumerable<string> values)
        => Matches(searchTerm, values, (v, s) => v.Contains(s, StringComparison.OrdinalIgnoreCase));

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

    private static bool Matches(string searchTerm,
        IEnumerable<string> values,
        Func<string, string, bool> valueMatchesSearchTerm)
    {
        if (string.IsNullOrEmpty(searchTerm)) return true;
        foreach (string value in values)
        {
            if (string.IsNullOrWhiteSpace(value)) continue;
            if (valueMatchesSearchTerm(value, searchTerm)) return true;
        }
        return false;
    }
}
