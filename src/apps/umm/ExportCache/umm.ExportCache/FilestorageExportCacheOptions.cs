using FileStorage;
using System.Collections.Immutable;

namespace umm.ExportCache;

public sealed class FilestorageExportCacheOptions
{
    public required IDirectory RootDirectory { get; init; }
    public required bool HandleFiles { get; init; }
    public required bool HandleDirectories { get; init; }
    public required ImmutableHashSet<string> MediaTypes { get; init; }
}
