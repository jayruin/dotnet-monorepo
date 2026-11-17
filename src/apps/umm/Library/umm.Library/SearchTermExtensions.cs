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
                ? new ConstantSearchTerm() { Constant = true }
                : new AndSearchTerm()
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
                        OrSearchTerm orTerm = new()
                        {
                            Targets = values
                                .Select(value => new MatchSearchTerm()
                                {
                                    Name = actualKey,
                                    Value = value ?? string.Empty,
                                })
                                .Where(t => !string.IsNullOrWhiteSpace(t.Name) && !string.IsNullOrWhiteSpace(t.Value))
                                .Cast<SearchTerm>()
                                .ToImmutableArray(),
                        };
                        SearchTerm result = isNegative
                            ? new NotSearchTerm() { Target = orTerm }
                            : orTerm;
                        return result;
                    }).ToImmutableArray(),
                };

        public bool Matches(params IReadOnlyCollection<MetadataSearchField> searchFields)
        {
            return searchTerm switch
            {
                ConstantSearchTerm constantTerm => constantTerm.Constant,
                CompareSearchTerm compareTerm => MatchesCompareTerm(compareTerm, searchFields),
                HasSearchTerm hasTerm => MatchesHasTerm(hasTerm, searchFields),
                NotSearchTerm notTerm => !notTerm.Target.Matches(searchFields),
                AndSearchTerm andTerm => andTerm.Targets.All(t => t.Matches(searchFields)),
                OrSearchTerm orTerm => orTerm.Targets.Any(t => t.Matches(searchFields)),
            };

            static bool MatchesCompareTerm(CompareSearchTerm compareTerm, IReadOnlyCollection<MetadataSearchField> searchFields)
            {
                return searchFields
                    .Where(f => string.IsNullOrWhiteSpace(compareTerm.Name) || f.Aliases.Contains(compareTerm.Name, StringComparer.OrdinalIgnoreCase))
                    .Any(f => f.Values.Any(v =>
                        (compareTerm is EqualsSearchTerm || compareTerm is MatchSearchTerm && f.ExactMatch) && v.Equals(compareTerm.Value, StringComparison.Ordinal)
                        || (compareTerm is ContainsSearchTerm || compareTerm is MatchSearchTerm && !f.ExactMatch) && v.Contains(compareTerm.Value, StringComparison.OrdinalIgnoreCase)));
            }

            static bool MatchesHasTerm(HasSearchTerm hasTerm, IReadOnlyCollection<MetadataSearchField> searchFields)
            {
                return searchFields.Where(f => f.Aliases.Contains(hasTerm.Name, StringComparer.OrdinalIgnoreCase))
                    .SelectMany(f => f.Values)
                    .Any();
            }
        }
    }
}
