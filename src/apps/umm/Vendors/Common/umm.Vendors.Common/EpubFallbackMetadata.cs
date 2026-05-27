using System;
using System.Collections.Immutable;
using System.Linq;
using umm.Library;

namespace umm.Vendors.Common;

public sealed class EpubFallbackMetadata<TMetadata> : ISearchableMetadata, IUniversalizableMediaMetadata
    where TMetadata : ISearchableMetadata, IUniversalizableMediaMetadata
{
    public EpubFallbackMetadata(TMetadata metadata, EpubMetadataAdapter epubMetadata)
    {
        Metadata = metadata;
        EpubMetadata = epubMetadata;
    }

    public TMetadata Metadata { get; }

    public EpubMetadataAdapter EpubMetadata { get; }

    public ImmutableArray<MetadataSearchField> GetSearchFields()
    {
        ImmutableArray<MetadataSearchField> searchFields = Metadata.GetSearchFields();
        ImmutableArray<MetadataSearchField> fallbackSearchFields = EpubMetadata.GetSearchFields();
        ImmutableArray<MetadataSearchField> extraSearchFields = fallbackSearchFields
            .Where(fsf =>
                !fsf.Aliases.Any(a =>
                    searchFields.Any(sf =>
                        sf.Aliases.Contains(a, StringComparer.OrdinalIgnoreCase))))
            .ToImmutableArray();
        return searchFields.AddRange(extraSearchFields);
    }

    public UniversalMediaMetadata Universalize()
    {
        UniversalMediaMetadata universalMetadata = Metadata.Universalize();
        UniversalMediaMetadata fallbackUniversalMetadata = EpubMetadata.Universalize();
        string title = !string.IsNullOrWhiteSpace(universalMetadata.Title)
            ? universalMetadata.Title
            : fallbackUniversalMetadata.Title;
        ImmutableArray<string> creators = !universalMetadata.Creators.IsEmpty
            ? universalMetadata.Creators
            : fallbackUniversalMetadata.Creators;
        string description = !string.IsNullOrWhiteSpace(universalMetadata.Description)
            ? universalMetadata.Description
            : fallbackUniversalMetadata.Description;
        ImmutableArray<string> identifiers = !universalMetadata.Identifiers.IsEmpty
            ? universalMetadata.Identifiers
            : fallbackUniversalMetadata.Identifiers;
        return new(title, creators, description, identifiers);
    }
}
