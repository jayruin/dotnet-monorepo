using Microsoft.Extensions.Primitives;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace umm.Library;

public static class SearchTermExtensions
{
    extension(SearchTerm searchTerm)
    {
        public static SearchTerm From(IReadOnlyDictionary<string, StringValues> searchQuery)
            => searchQuery.Count == 0
                ? new SearchConstantTerm() { Constant = true }
                : new SearchAndTerm()
                {
                    Targets = searchQuery.Select(kvp =>
                    {
                        (string key, StringValues values) = kvp;
                        bool isNegative;
                        string actualKey;
                        if (key.StartsWith('-'))
                        {
                            isNegative = true;
                            actualKey = key[1..];
                        }
                        else
                        {
                            isNegative = false;
                            actualKey = key;
                        }
                        SearchOrTerm orTerm = new()
                        {
                            Targets = values
                                .Select(value => new SearchMatchTerm()
                                {
                                    Name = actualKey,
                                    Value = value ?? string.Empty,
                                })
                                .Where(t => !string.IsNullOrWhiteSpace(t.Name) && !string.IsNullOrWhiteSpace(t.Value))
                                .Cast<SearchTerm>()
                                .ToImmutableArray(),
                        };
                        SearchTerm result = isNegative
                            ? new SearchNotTerm() { Target = orTerm }
                            : orTerm;
                        return result;
                    }).ToImmutableArray(),
                };

        public bool Matches(params IReadOnlyCollection<MetadataSearchField> searchFields)
        {
            return searchTerm switch
            {
                SearchConstantTerm constantTerm => constantTerm.Constant,
                SearchBaseTerm baseTerm => MatchesBaseTerm(baseTerm, searchFields),
                SearchNotTerm notTerm => !notTerm.Target.Matches(searchFields),
                SearchAndTerm andTerm => andTerm.Targets.All(t => t.Matches(searchFields)),
                SearchOrTerm orTerm => orTerm.Targets.Any(t => t.Matches(searchFields)),
            };

            static bool MatchesBaseTerm(SearchBaseTerm baseTerm, IReadOnlyCollection<MetadataSearchField> searchFields)
            {
                return searchFields
                    .Where(f => string.IsNullOrWhiteSpace(baseTerm.Name) || f.Aliases.Any(a => a.Equals(baseTerm.Name, StringComparison.OrdinalIgnoreCase)))
                    .Any(f => f.Values.Any(v =>
                        (baseTerm is SearchEqualsTerm || baseTerm is SearchMatchTerm && f.ExactMatch) && v.Equals(baseTerm.Value, StringComparison.Ordinal)
                        || (baseTerm is SearchContainsTerm || baseTerm is SearchMatchTerm && !f.ExactMatch) && v.Contains(baseTerm.Value, StringComparison.OrdinalIgnoreCase)));
            }
        }
    }
}
