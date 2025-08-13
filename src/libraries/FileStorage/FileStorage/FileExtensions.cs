using System.IO;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace FileStorage;

public static class FileExtensions
{
    public static void CopyTo(this IFile source, IFile destination)
    {
        using Stream sourceStream = source.OpenRead();
        using Stream destinationStream = destination.OpenWrite();
        sourceStream.CopyTo(destinationStream);
    }

    public static async Task CopyToAsync(this IFile source, IFile destination, CancellationToken cancellationToken = default)
    {
        Stream sourceStream = await source.OpenReadAsync(cancellationToken).ConfigureAwait(false);
        await using ConfiguredAsyncDisposable configuredSourceStream = sourceStream.ConfigureAwait(false);
        Stream destinationStream = await destination.OpenWriteAsync(cancellationToken).ConfigureAwait(false);
        await using ConfiguredAsyncDisposable configuredDestinationStream = destinationStream.ConfigureAwait(false);
        await sourceStream.CopyToAsync(destinationStream, cancellationToken).ConfigureAwait(false);
    }
}
