using FileStorage;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace EpubProj;

public interface IEpubProject
{
    IEpubProjectMetadata Metadata { get; }
    IFile? CoverFile { get; }
    Task ExportEpub3Async(Stream stream, IReadOnlyCollection<IFile> globalFiles, CancellationToken cancellationToken = default);
    Task ExportEpub2Async(Stream stream, IReadOnlyCollection<IFile> globalFiles, CancellationToken cancellationToken = default);
}
