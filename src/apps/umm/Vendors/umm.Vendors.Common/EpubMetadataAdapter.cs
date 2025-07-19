using Epubs;
using System.Collections.Immutable;
using System.Linq;
using umm.Library;

namespace umm.Vendors.Common;

public sealed class EpubMetadataAdapter : ISearchableMetadata, IUniversalizableMediaMetadata
{
    private readonly IEpubMetadata _metadata;

    public EpubMetadataAdapter(IEpubMetadata metadata)
    {
        _metadata = metadata;
    }

    public ImmutableArray<MetadataSearchField> GetSearchFields() => [
        new()
        {
            Aliases = ["title"],
            Values = [
                _metadata.Title,
            ],
            ExactMatch = false,
        },
        new()
        {
            Aliases = ["creator"],
            Values = [
                .._metadata.Creators.Select(c => c.Name),
            ],
            ExactMatch = false,
        },
        new()
        {
            Aliases = ["description"],
            Values = _metadata.Description is null
                ? []
                : [
                    _metadata.Description,
                ],
            ExactMatch = false,
        },
        new()
        {
            Aliases = ["series"],
            Values = _metadata.Series is null
                ? []
                : [
                    _metadata.Series.Name,
                ],
            ExactMatch = false,
        },
    ];

    public UniversalMediaMetadata Universalize()
    {
        string title = _metadata.Title;
        ImmutableArray<string> creators = _metadata.Creators
            .Select(c => c.ToString())
            .ToImmutableArray();
        string description = _metadata.Description ?? string.Empty;
        return new(title, creators, description);
    }
}
