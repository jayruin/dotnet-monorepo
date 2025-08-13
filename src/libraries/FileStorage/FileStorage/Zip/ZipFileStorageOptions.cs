using System;
using System.IO.Compression;

namespace FileStorage.Zip;

public sealed class ZipFileStorageOptions
{
    public ZipArchiveMode Mode { get; init; } = ZipArchiveMode.Update;
    public CompressionLevel Compression { get; init; } = CompressionLevel.NoCompression;
    public DateTimeOffset? FixedTimestamp { get; init; } = null;
}
