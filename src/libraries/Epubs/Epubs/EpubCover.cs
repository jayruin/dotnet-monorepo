using FileStorage;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Epubs;

public sealed class EpubCover
{
    internal EpubCover(IFile coverFile, EpubPath path, string mediaType)
    {
        CoverFile = coverFile;
        Path = path;
        MediaType = mediaType;
    }

    internal IFile CoverFile { get; }
    internal EpubPath Path { get; }
    public string MediaType { get; }

    public Task<Stream> OpenReadAsync(CancellationToken cancellationToken = default)
        => CoverFile.OpenReadAsync(cancellationToken);
}
