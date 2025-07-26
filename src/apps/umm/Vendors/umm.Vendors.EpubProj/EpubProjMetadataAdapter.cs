using EpubProj;
using System.Collections.Immutable;
using System.Linq;
using umm.Library;
using umm.Vendors.Common;

namespace umm.Vendors.EpubProj;

public sealed class EpubProjMetadataAdapter : ISearchableMetadata, IUniversalizableMediaMetadata
{
    private readonly IEpubProjectMetadata _epubProjectMetadata;

    public EpubProjMetadataAdapter(IEpubProjectMetadata epubProjectMetadata)
    {
        _epubProjectMetadata = epubProjectMetadata;
    }

    public ImmutableArray<MetadataSearchField> GetSearchFields() => [
        new()
        {
            Aliases = ["title"],
            Values = [_epubProjectMetadata.Title],
            ExactMatch = false,
        },
        new()
        {
            Aliases = ["creator"],
            Values = [.._epubProjectMetadata.Creators.Select(c => c.Name)],
            ExactMatch = false,
        },
        new()
        {
            Aliases = ["series"],
            Values = _epubProjectMetadata.Series is null
                ? []
                : [_epubProjectMetadata.Series.Name],
            ExactMatch = false,
        },
    ];

    public UniversalMediaMetadata Universalize()
    {
        string title = _epubProjectMetadata.Title;
        ImmutableArray<string> creators = _epubProjectMetadata.Creators
            .Select(c => c.Roles.Length > 0
                ? $"{c.Name} ({string.Join(", ", c.Roles)})"
                : c.Name)
            .ToImmutableArray();
        string description = string.Empty;
        return new(title, creators, description);
    }
}
