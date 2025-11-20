using System;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace FileStorage;

public static class FileExtensions
{
    extension(IFile source)
    {
        public void CopyTo(IFile destination)
        {
            using Stream sourceStream = source.OpenRead();
            using Stream destinationStream = destination.OpenWrite();
            sourceStream.CopyTo(destinationStream);
        }

        public async Task CopyToAsync(IFile destination, CancellationToken cancellationToken = default)
        {
            Stream sourceStream = await source.OpenReadAsync(cancellationToken).ConfigureAwait(false);
            await using ConfiguredAsyncDisposable configuredSourceStream = sourceStream.ConfigureAwait(false);
            Stream destinationStream = await destination.OpenWriteAsync(cancellationToken).ConfigureAwait(false);
            await using ConfiguredAsyncDisposable configuredDestinationStream = destinationStream.ConfigureAwait(false);
            await sourceStream.CopyToAsync(destinationStream, cancellationToken).ConfigureAwait(false);
        }
    }

    extension(IFile file)
    {
        public void WriteText(string text, Encoding encoding)
        {
            using Stream stream = file.OpenWrite();
            using StreamWriter streamWriter = new(stream, encoding);
            streamWriter.Write(text);
        }

        public async Task WriteTextAsync(string text, Encoding encoding, CancellationToken cancellationToken = default)
        {
            Stream stream = await file.OpenWriteAsync(cancellationToken).ConfigureAwait(false);
            await using ConfiguredAsyncDisposable configuredStream = stream.ConfigureAwait(false);
            StreamWriter streamWriter = new(stream, encoding);
            await using ConfiguredAsyncDisposable configuredStreamWriter = streamWriter.ConfigureAwait(false);
            await streamWriter.WriteAsync(text.AsMemory(), cancellationToken).ConfigureAwait(false);
        }
    }
}
