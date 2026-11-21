using MediaTypes;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using umm.Library;

namespace umm.Vendors.Common;

public static class SharedMetadataSearchFields
{
    public static ImmutableArray<MetadataSearchField> GetAllSharedFields(MediaFullId mediaFullId,
        IReadOnlySet<string> tags, IReadOnlyCollection<MediaExportTarget> exportTargets) =>
    [
        GetTagField(tags),
        ..GetMediaIdFields(mediaFullId),
        ..GetExportTargetFields(exportTargets),
    ];

    public static MetadataSearchField GetTagField(IReadOnlySet<string> tags) => new()
    {
        Aliases = ["tag"],
        Values = [.. tags],
        ExactMatch = true,
    };

    public static ImmutableArray<MetadataSearchField> GetMediaIdFields(MediaFullId mediaFullId) =>
    [
        GetVendorIdField(mediaFullId.VendorId),
        GetContentIdField(mediaFullId.ContentId),
        GetPartIdField(mediaFullId.PartId),
    ];

    public static MetadataSearchField GetVendorIdField(string vendorId) => new()
    {
        Aliases = [nameof(MediaFullId.VendorId)],
        Values = [vendorId],
        ExactMatch = true,
    };

    public static MetadataSearchField GetContentIdField(string contentId) => new()
    {
        Aliases = [nameof(MediaFullId.ContentId)],
        Values = [contentId],
        ExactMatch = true,
    };

    public static MetadataSearchField GetPartIdField(string partId) => new()
    {
        Aliases = [nameof(MediaFullId.PartId)],
        Values = [partId],
        ExactMatch = true,
    };

    public static ImmutableArray<MetadataSearchField> GetExportTargetFields(IReadOnlyCollection<MediaExportTarget> exportTargets) =>
    [
        new()
        {
            Aliases = [nameof(MediaFormat)],
            Values = [.. exportTargets.SelectMany(et => et.MediaFormats).Select(mf => mf.ToString())],
            ExactMatch = true,
        },
        new()
        {
            Aliases = [nameof(MediaType)],
            Values = [.. exportTargets.Select(et => et.MediaType)],
            ExactMatch = true,
        },
    ];
}
