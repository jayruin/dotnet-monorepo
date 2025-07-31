using System.Collections.Frozen;

namespace umm.ExportCache;

public sealed class FilestorageExportCacheVendorOverrideOptions
{
    public required FrozenSet<string> MediaTypes { get; init; }
}
