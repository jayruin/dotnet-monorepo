using FileStorage;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Threading;
using System.Threading.Tasks;

namespace EpubProj;

public interface IEpubProject
{
    IEpubProjectMetadata Metadata { get; }
    IFile? CoverFile { get; }
    Task ExportEpub3Async(Stream stream, IReadOnlyCollection<IFile>? globalFiles = null, CompressionLevel compression = CompressionLevel.NoCompression, CancellationToken cancellationToken = default);
    Task ExportEpub3Async(IDirectory directory, IReadOnlyCollection<IFile>? globalFiles = null, CancellationToken cancellationToken = default);
    Task ExportEpub2Async(Stream stream, IReadOnlyCollection<IFile>? globalFiles = null, CompressionLevel compression = CompressionLevel.NoCompression, CancellationToken cancellationToken = default);
    Task ExportEpub2Async(IDirectory directory, IReadOnlyCollection<IFile>? globalFiles = null, CancellationToken cancellationToken = default);
}
