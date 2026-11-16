using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Utils;

namespace umm.Library;

public static class MainSearchableMediaEntries
{
    public static ImmutableArray<SearchableMediaEntry> ConvertToMainEntries(IReadOnlyCollection<SearchableMediaEntry> allEntries)
    {
        ReadOnlyCollectionEqualityComparer<ImmutableArray<string>, string> aliasesEqualityComparer = new(StringComparer.OrdinalIgnoreCase);
        return allEntries
            .GroupBy(e => e.MediaEntry.Id.ToMainId())
            .Select(idGrouping =>
            {
                MediaFullId fullId = idGrouping.Key.ToFullId();
                SearchableMediaEntry mainEntry = allEntries.First(e => e.MediaEntry.Id == fullId);
                ImmutableArray<SearchableMediaEntry> groupingEntries = [.. idGrouping];
                ImmutableArray<MetadataSearchField> searchFields = groupingEntries
                    .SelectMany(e => e.MetadataSearchFields)
                    .GroupBy(sf => sf.Aliases, aliasesEqualityComparer)
                    .Select(searchFieldGrouping =>
                    {
                        ImmutableArray<string> aliases = searchFieldGrouping.Key;
                        ImmutableArray<string> values = searchFieldGrouping
                            .SelectMany(sf => sf.Values)
                            .ToImmutableArray();
                        MetadataSearchField mainSearchField = mainEntry.MetadataSearchFields
                            .First(sf => aliasesEqualityComparer.Equals(sf.Aliases, aliases));
                        bool exactMatch = mainSearchField.ExactMatch;
                        return new MetadataSearchField()
                        {
                            Aliases = aliases,
                            Values = values,
                            ExactMatch = exactMatch,
                        };
                    })
                    .ToImmutableArray();
                return new SearchableMediaEntry()
                {
                    MediaEntry = mainEntry.MediaEntry,
                    MetadataSearchFields = searchFields,
                };
            })
            .ToImmutableArray();
    }
}
