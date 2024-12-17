using FileStorage;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace EpubProj;

public interface IEpubProject
{
    IEpubProjectMetadata Metadata { get; }
    IFile? CoverFile { get; }
    Task ExportEpub3Async(Stream stream, IReadOnlyCollection<IFile> globalFiles);
    Task ExportEpub2Async(Stream stream, IReadOnlyCollection<IFile> globalFiles);
}
