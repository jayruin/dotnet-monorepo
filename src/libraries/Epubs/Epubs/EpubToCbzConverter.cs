using FileStorage;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Utils;

namespace Epubs;

public sealed class EpubToCbzConverter
{
    private readonly EpubContainer _container;

    internal EpubToCbzConverter(EpubContainer container)
    {
        _container = container;
    }

    public async Task WriteAsync(Stream outputStream, CompressionLevel compressionLevel = CompressionLevel.NoCompression, CancellationToken cancellationToken = default)
    {
        DateTimeOffset timestamp = await GetTimestampAsync(cancellationToken).ConfigureAwait(false);
        // TODO LINQ
        List<IFile> imageFiles = await _container.GetPrePaginatedImageFilesAsync(cancellationToken)
            .ToListAsync(cancellationToken).ConfigureAwait(false);
        // TODO Async Zip
        using ZipArchive zip = new(outputStream, ZipArchiveMode.Create, true);
        for (int i = 0; i < imageFiles.Count; i++)
        {
            IFile imageFile = imageFiles[i];
            ZipArchiveEntry imageEntry = zip.CreateEntry($"{i.ToPaddedString(imageFiles.Count)}{imageFile.Extension}", compressionLevel);
            imageEntry.LastWriteTime = timestamp;
            Stream imageFileStream = await imageFile.OpenReadAsync(cancellationToken).ConfigureAwait(false);
            await using (imageFileStream.ConfigureAwait(false))
            {
                // TODO Async Zip
                Stream imageEntryStream = imageEntry.Open();
                await using (imageEntryStream.ConfigureAwait(false))
                {
                    await imageFileStream.CopyToAsync(imageEntryStream, cancellationToken).ConfigureAwait(false);
                }
            }
        }
    }

    public async Task WriteAsync(IDirectory outputDirectory, CancellationToken cancellationToken = default)
    {
        DateTimeOffset timestamp = await GetTimestampAsync(cancellationToken).ConfigureAwait(false);
        // TODO LINQ
        List<IFile> imageFiles = await _container.GetPrePaginatedImageFilesAsync(cancellationToken)
            .ToListAsync(cancellationToken).ConfigureAwait(false);
        for (int i = 0; i < imageFiles.Count; i++)
        {
            IFile imageFile = imageFiles[i];
            IFile outputFile = outputDirectory.GetFile($"{i.ToPaddedString(imageFiles.Count)}{imageFile.Extension}");
            await imageFile.CopyToAsync(outputFile, cancellationToken).ConfigureAwait(false);
        }
    }

    private async Task<DateTimeOffset> GetTimestampAsync(CancellationToken cancellationToken)
    {
        IEpubMetadata metadata = await _container.GetMetadataAsync(cancellationToken).ConfigureAwait(false);
        DateTimeOffset timestamp = metadata.LastModified ?? DateTimeOffset.MinValue;
        return timestamp.Clamp(ZipConstants.MinLastWriteTime, ZipConstants.MaxLastWriteTime);
    }
}
