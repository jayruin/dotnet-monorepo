using System;
using System.IO.Compression;

namespace Epubs;

public sealed class EpubWriterOptions
{
    public EpubVersion Version { get; init; } = EpubVersion.Epub3;
    public CompressionLevel Compression { get; init; } = CompressionLevel.NoCompression;
    public DateTimeOffset Modified { get; init; } = DateTimeOffset.UtcNow;
    public string ReservedPrefix { get; init; } = ".";
    public string ContentDirectory { get; init; } = "OEBPS";
}
