using FileStorage;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Epubs;

public sealed class EpubCover
{
    internal EpubCover(IFile coverFile, string relativePath, string mediaType)
    {
        CoverFile = coverFile;
        RelativePath = relativePath;
        MediaType = mediaType;
    }

    internal IFile CoverFile { get; }
    internal string RelativePath { get; }
    public string MediaType { get; }

    public Task<Stream> OpenReadAsync(CancellationToken cancellationToken = default)
        => CoverFile.OpenReadAsync(cancellationToken);
}
