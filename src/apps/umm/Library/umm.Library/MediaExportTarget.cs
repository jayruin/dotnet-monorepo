using System.Collections.Immutable;

namespace umm.Library;

public sealed class MediaExportTarget
{
    public required string ExportId { get; init; }
    public required string MediaType { get; init; }
    public required bool SupportsFile { get; init; }
    public required bool SupportsDirectory { get; init; }
    public required ImmutableSortedSet<MediaFormat> MediaFormats { get; init; }
}
