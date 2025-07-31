using FileStorage;
using System.Collections.Frozen;

namespace umm.ExportCache;

public sealed class FilestorageExportCacheOptions
{
    public required IDirectory RootDirectory { get; init; }
    public required bool HandleFiles { get; init; }
    public required bool HandleDirectories { get; init; }
    public required FrozenSet<string> MediaTypes { get; init; }
    public required FrozenDictionary<string, FilestorageExportCacheVendorOverrideOptions> VendorOverrides { get; init; }
}
