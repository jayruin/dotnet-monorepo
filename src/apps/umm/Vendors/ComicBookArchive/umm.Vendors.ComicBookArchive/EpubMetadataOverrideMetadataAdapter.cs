using System;
using System.Collections.Immutable;
using umm.Library;
using umm.Vendors.Common;
using Utils;

namespace umm.Vendors.ComicBookArchive;

internal sealed class EpubMetadataOverrideMetadataAdapter : ISearchableMetadata, IUniversalizableMediaMetadata
{
    public EpubMetadataOverrideMetadataAdapter(string contentId, BasicEpubMetadataOverride epubMetadataOverride)
    {
        ContentId = contentId;
        EpubMetadataOverride = epubMetadataOverride;
    }

    public string ContentId { get; }

    public BasicEpubMetadataOverride EpubMetadataOverride { get; }

    public DateTimeOffset Timestamp
    {
        get
        {
            DateTimeOffset timestamp = EpubMetadataOverride.Date.ToDateTimeOffsetNullable() ?? DateTimeOffset.MinValue;
            return timestamp.Clamp(ZipConstants.MinLastWriteTime, ZipConstants.MaxLastWriteTime);
        }
    }

    public ImmutableArray<MetadataSearchField> GetSearchFields()
    {
        return EpubMetadataOverride.GetSearchFields();
    }

    public UniversalMediaMetadata Universalize()
    {
        return EpubMetadataOverride.Universalize();
    }
}
