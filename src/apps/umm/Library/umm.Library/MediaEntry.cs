using System.Collections.Immutable;

namespace umm.Library;

public sealed class MediaEntry
{
    public required string VendorId { get; init; }
    public required string ContentId { get; init; }
    public required string PartId { get; init; }
    public required UniversalMediaMetadata Metadata { get; init; }
    public required ImmutableArray<MediaExportTarget> ExportTargets { get; init; }
    public required ImmutableSortedSet<string> Tags { get; init; }
}
