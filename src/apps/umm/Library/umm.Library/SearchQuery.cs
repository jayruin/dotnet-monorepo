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
        StringValues allPositiveSearchValues = new();
        StringValues allNegativeSearchValues = new();
        foreach (string searchKey in searchKeys)
        {
            if (searchQuery.TryGetValue(searchKey, out StringValues positiveSearchValues))
            {
                allPositiveSearchValues = StringValues.Concat(allPositiveSearchValues, positiveSearchValues);
            }
            if (searchQuery.TryGetValue($"-{searchKey}", out StringValues negativeSearchValues))
            {
                allNegativeSearchValues = StringValues.Concat(allNegativeSearchValues, negativeSearchValues);
            }
        }
        IReadOnlyCollection<string> valuesList = values as IReadOnlyCollection<string> ?? [.. values];
        return MatchesPositively(valuesList, allPositiveSearchValues, valueMatchesSearchValue)
            && MatchesNegatively(valuesList, allNegativeSearchValues, valueMatchesSearchValue);
    }

    private static bool MatchesPositively(IEnumerable<string> values, StringValues allPositiveSearchValues, Func<string, string, bool> valueMatchesSearchValue)
    {
        if (allPositiveSearchValues.Count == 0) return true;
        foreach (string value in values)
        {
            if (string.IsNullOrWhiteSpace(value)) continue;
            foreach (string? searchValue in allPositiveSearchValues)
            {
                if (string.IsNullOrWhiteSpace(searchValue)) continue;
                if (valueMatchesSearchValue(value, searchValue)) return true;
            }
        }
        return false;
    }

    private static bool MatchesNegatively(IEnumerable<string> values, StringValues allNegativeSearchValues, Func<string, string, bool> valueMatchesSearchValue)
    {
        if (allNegativeSearchValues.Count == 0) return true;
        foreach (string value in values)
        {
            if (string.IsNullOrWhiteSpace(value)) continue;
            foreach (string? searchValue in allNegativeSearchValues)
            {
                if (string.IsNullOrWhiteSpace(searchValue)) continue;
                if (valueMatchesSearchValue(value, searchValue)) return false;
            }
        }
        return true;
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
