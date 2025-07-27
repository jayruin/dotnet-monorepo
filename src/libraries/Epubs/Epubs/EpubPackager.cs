using FileStorage;
using MediaTypes;
using System;
using System.Collections.Immutable;
using System.IO;
using System.IO.Compression;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;
using Utils;

namespace Epubs;

public sealed class EpubPackager
{
    private readonly EpubContainer _container;
    private readonly IMediaTypeFileExtensionsMapping _mediaTypeFileExtensionsMapping;

    internal EpubPackager(EpubContainer container, IMediaTypeFileExtensionsMapping mediaTypeFileExtensionsMapping)
    {
        _container = container;
        _mediaTypeFileExtensionsMapping = mediaTypeFileExtensionsMapping;
    }

    internal string? NewCoverMediaType { get; set; }
    internal Func<EpubCover?, Stream, CancellationToken, Task>? HandleCoverAsync { get; set; }
    internal Func<IEpubMetadata, CancellationToken, Task>? HandleMetadataAsync { get; set; }
    internal Action<XDocument>? HandleXhtml { get; set; }

    public EpubPackager WithCoverHandler(string newCoverMediaType, Func<EpubCover?, Stream, CancellationToken, Task> handleCoverAsync)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(newCoverMediaType);
        NewCoverMediaType = newCoverMediaType;
        HandleCoverAsync = handleCoverAsync;
        return this;
    }

    public EpubPackager WithMetadataHandler(Func<IEpubMetadata, CancellationToken, Task> handleMetadataAsync)
    {
        HandleMetadataAsync = handleMetadataAsync;
        return this;
    }

    public EpubPackager WithMetadataHandler(Action<IEpubMetadata> handleMetadata)
    {
        HandleMetadataAsync = (metadata, cancellationToken) =>
        {
            cancellationToken.ThrowIfCancellationRequested();
            handleMetadata(metadata);
            return Task.CompletedTask;
        };
        return this;
    }

    public EpubPackager WithXhtmlHandler(Action<XDocument> handleXhtml)
    {
        HandleXhtml = handleXhtml;
        return this;
    }

    public async Task PackageAsync(Stream outputStream, CompressionLevel compressionLevel = CompressionLevel.NoCompression, CancellationToken cancellationToken = default)
    {
        DateTimeOffset timestamp = await GetTimestampAsync(cancellationToken).ConfigureAwait(false);

        EpubContents contents = await _container.TraverseAsync(cancellationToken).ConfigureAwait(false);

        // TODO Async Zip
        using ZipArchive outputZip = new(outputStream, ZipArchiveMode.Create, true);

        await WriteMimetypeFileAsync(contents, outputZip, timestamp, cancellationToken).ConfigureAwait(false);
        await CopyRegularItemsAsync(contents, outputZip, compressionLevel, timestamp, cancellationToken).ConfigureAwait(false);
        string? newCoverName = await WriteCoverAsync(contents, outputZip, compressionLevel, timestamp, cancellationToken).ConfigureAwait(false);
        await WriteXhtmlFilesAsync(contents, outputZip, compressionLevel, timestamp, cancellationToken).ConfigureAwait(false);
        await WriteOpfFileAsync(contents, outputZip, compressionLevel, timestamp, newCoverName, cancellationToken).ConfigureAwait(false);
    }

    public async Task PackageAsync(IDirectory outputDirectory, CancellationToken cancellationToken = default)
    {
        EpubContents contents = await _container.TraverseAsync(cancellationToken).ConfigureAwait(false);

        await WriteMimetypeFileAsync(contents, outputDirectory, cancellationToken).ConfigureAwait(false);
        await CopyRegularItemsAsync(contents, outputDirectory, cancellationToken).ConfigureAwait(false);
        string? newCoverName = await WriteCoverAsync(contents, outputDirectory, cancellationToken).ConfigureAwait(false);
        await WriteXhtmlFilesAsync(contents, outputDirectory, cancellationToken).ConfigureAwait(false);
        await WriteOpfFileAsync(contents, outputDirectory, newCoverName, cancellationToken).ConfigureAwait(false);
    }

    private async Task<DateTimeOffset> GetTimestampAsync(CancellationToken cancellationToken)
    {
        IEpubMetadata metadata = await _container.GetMetadataAsync(cancellationToken).ConfigureAwait(false);
        if (HandleMetadataAsync is not null)
        {
            await HandleMetadataAsync(metadata, cancellationToken).ConfigureAwait(false);
        }
        DateTimeOffset timestamp = metadata.LastModified ?? DateTimeOffset.MinValue;
        return timestamp.Clamp(ZipConstants.MinLastWriteTime, ZipConstants.MaxLastWriteTime);
    }

    private async Task CopyFileAsync(ImmutableArray<string> path, ZipArchive outputZip, CompressionLevel compressionLevel, DateTimeOffset timestamp, CancellationToken cancellationToken)
    {
        IFile sourceFile = _container.RootDirectory.GetFile(path);
        ZipArchiveEntry zipEntry = outputZip.CreateEntry(string.Join('/', path), compressionLevel);
        zipEntry.LastWriteTime = timestamp;
        Stream sourceStream = await sourceFile.OpenReadAsync(cancellationToken).ConfigureAwait(false);
        await using (sourceStream.ConfigureAwait(false))
        {
            // TODO Async Zip
            Stream destinationStream = zipEntry.Open();
            await using (destinationStream.ConfigureAwait(false))
            {
                await sourceStream.CopyToAsync(destinationStream, cancellationToken).ConfigureAwait(false);
            }
        }
    }

    private Task CopyFileAsync(ImmutableArray<string> path, IDirectory outputDirectory, CancellationToken cancellationToken)
    {
        IFile sourceFile = _container.RootDirectory.GetFile(path);
        IFile destinationFile = outputDirectory.GetFile(path);
        return sourceFile.CopyToAsync(destinationFile, cancellationToken);
    }

    private Task WriteMimetypeFileAsync(EpubContents contents, ZipArchive outputZip, DateTimeOffset timestamp, CancellationToken cancellationToken)
    {
        return CopyFileAsync(contents.MimetypeFilePath, outputZip, CompressionLevel.NoCompression, timestamp, cancellationToken);
    }

    private Task WriteMimetypeFileAsync(EpubContents contents, IDirectory outputDirectory, CancellationToken cancellationToken)
    {
        return CopyFileAsync(contents.MimetypeFilePath, outputDirectory, cancellationToken);
    }

    private async Task CopyRegularItemsAsync(EpubContents contents, ZipArchive outputZip, CompressionLevel compressionLevel, DateTimeOffset timestamp, CancellationToken cancellationToken)
    {
        foreach (ImmutableArray<string> path in contents.DirectoryPaths)
        {
            ZipArchiveEntry entry = outputZip.CreateEntry($"{string.Join('/', path)}/", compressionLevel);
            entry.LastWriteTime = timestamp;
        }
        foreach (ImmutableArray<string> path in contents.FilePaths)
        {
            await CopyFileAsync(path, outputZip, compressionLevel, timestamp, cancellationToken).ConfigureAwait(false);
        }
    }

    private async Task CopyRegularItemsAsync(EpubContents contents, IDirectory outputDirectory, CancellationToken cancellationToken)
    {
        foreach (ImmutableArray<string> path in contents.DirectoryPaths)
        {
            await outputDirectory.GetDirectory(path).CreateAsync(cancellationToken).ConfigureAwait(false);
        }
        foreach (ImmutableArray<string> path in contents.FilePaths)
        {
            await CopyFileAsync(path, outputDirectory, cancellationToken).ConfigureAwait(false);
        }
    }

    private async Task<(string, ImmutableArray<string>)> GetNewCoverNameAndPathAsync(EpubContents contents, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(NewCoverMediaType))
        {
            throw new InvalidOperationException("No new cover media type was provided.");
        }
        string newCoverExtension = _mediaTypeFileExtensionsMapping.GetFileExtension(NewCoverMediaType)
            ?? throw new InvalidOperationException("Could not get new cover extension.");
        string newCoverName = $"cover{newCoverExtension}";
        ImmutableArray<string> opfDirectoryPath = contents.OpfFilePath.RemoveAt(contents.OpfFilePath.Length - 1);
        ImmutableArray<string> newCoverPath = opfDirectoryPath.Add(newCoverName);
        int coverCounter = 1;
        while (await _container.RootDirectory.GetFile(opfDirectoryPath.Add(newCoverName)).ExistsAsync(cancellationToken).ConfigureAwait(false))
        {
            newCoverName = $"cover{coverCounter}{newCoverExtension}";
            newCoverPath = opfDirectoryPath.Add(newCoverName);
            coverCounter += 1;
        }
        return (newCoverName, newCoverPath);
    }

    private async Task<string?> WriteCoverAsync(EpubContents contents, ZipArchive outputZip, CompressionLevel compressionLevel, DateTimeOffset timestamp, CancellationToken cancellationToken)
    {
        string? newCoverName = null;
        if (HandleCoverAsync is not null)
        {
            if (contents.CoverFilePath.IsDefaultOrEmpty)
            {
                (newCoverName, ImmutableArray<string> newCoverPath) = await GetNewCoverNameAndPathAsync(contents, cancellationToken).ConfigureAwait(false);
                ZipArchiveEntry coverEntry = outputZip.CreateEntry(string.Join('/', newCoverPath), compressionLevel);
                coverEntry.LastWriteTime = timestamp;
                // TODO Async Zip
                Stream destinationCoverStream = coverEntry.Open();
                await using (destinationCoverStream.ConfigureAwait(false))
                {
                    await HandleCoverAsync(null, destinationCoverStream, cancellationToken).ConfigureAwait(false);
                }
            }
            else
            {
                EpubCover sourceCover = await _container.GetCoverAsync(cancellationToken).ConfigureAwait(false)
                    ?? throw new InvalidOperationException("Could not get cover.");
                ZipArchiveEntry coverEntry = outputZip.CreateEntry(string.Join('/', contents.CoverFilePath), compressionLevel);
                coverEntry.LastWriteTime = timestamp;
                // TODO Async Zip
                Stream destinationCoverStream = coverEntry.Open();
                await using (destinationCoverStream.ConfigureAwait(false))
                {
                    await HandleCoverAsync(sourceCover, destinationCoverStream, cancellationToken).ConfigureAwait(false);
                }
            }
        }
        else if (!contents.CoverFilePath.IsDefaultOrEmpty)
        {
            await CopyFileAsync(contents.CoverFilePath, outputZip, compressionLevel, timestamp, cancellationToken).ConfigureAwait(false);
        }
        return newCoverName;
    }

    private async Task<string?> WriteCoverAsync(EpubContents contents, IDirectory outputDirectory, CancellationToken cancellationToken)
    {
        string? newCoverName = null;
        if (HandleCoverAsync is not null)
        {
            if (contents.CoverFilePath.IsDefaultOrEmpty)
            {
                (newCoverName, ImmutableArray<string> newCoverPath) = await GetNewCoverNameAndPathAsync(contents, cancellationToken).ConfigureAwait(false);
                Stream destinationCoverStream = await outputDirectory.GetFile(newCoverPath).OpenWriteAsync(cancellationToken).ConfigureAwait(false);
                await using (destinationCoverStream.ConfigureAwait(false))
                {
                    await HandleCoverAsync(null, destinationCoverStream, cancellationToken).ConfigureAwait(false);
                }
            }
            else
            {
                EpubCover sourceCover = await _container.GetCoverAsync(cancellationToken).ConfigureAwait(false)
                    ?? throw new InvalidOperationException("Could not get cover.");
                Stream destinationCoverStream = await outputDirectory.GetFile(contents.CoverFilePath).OpenWriteAsync(cancellationToken).ConfigureAwait(false);
                await using (destinationCoverStream.ConfigureAwait(false))
                {
                    await HandleCoverAsync(sourceCover, destinationCoverStream, cancellationToken).ConfigureAwait(false);
                }
            }
        }
        else if (!contents.CoverFilePath.IsDefaultOrEmpty)
        {
            await CopyFileAsync(contents.CoverFilePath, outputDirectory, cancellationToken).ConfigureAwait(false);
        }
        return newCoverName;
    }

    private async Task<XDocument> GetDocumentAsync(ImmutableArray<string> path, CancellationToken cancellationToken)
    {
        IFile sourceFile = _container.RootDirectory.GetFile(path);
        Stream sourceStream = await sourceFile.OpenReadAsync(cancellationToken).ConfigureAwait(false);
        XDocument document;
        await using (sourceStream.ConfigureAwait(false))
        {
            document = await XDocument.LoadAsync(sourceStream, default, cancellationToken).ConfigureAwait(false);
        }
        return document;
    }

    private async Task WriteXhtmlFilesAsync(EpubContents contents, ZipArchive outputZip, CompressionLevel compressionLevel, DateTimeOffset timestamp, CancellationToken cancellationToken)
    {
        foreach (ImmutableArray<string> path in contents.XhtmlPaths)
        {
            if (HandleXhtml is null)
            {
                await CopyFileAsync(path, outputZip, compressionLevel, timestamp, cancellationToken).ConfigureAwait(false);
            }
            else
            {
                XDocument xhtmlDocument = await GetDocumentAsync(path, cancellationToken).ConfigureAwait(false);
                HandleXhtml(xhtmlDocument);
                ZipArchiveEntry zipEntry = outputZip.CreateEntry(string.Join('/', path), compressionLevel);
                zipEntry.LastWriteTime = timestamp;
                // TODO Async Zip
                Stream destinationStream = zipEntry.Open();
                await using (destinationStream.ConfigureAwait(false))
                {
                    await EpubXml.SaveAsync(xhtmlDocument, destinationStream, cancellationToken).ConfigureAwait(false);
                }
            }
        }
    }

    private async Task WriteXhtmlFilesAsync(EpubContents contents, IDirectory outputDirectory, CancellationToken cancellationToken)
    {
        foreach (ImmutableArray<string> path in contents.XhtmlPaths)
        {
            if (HandleXhtml is null)
            {
                await CopyFileAsync(path, outputDirectory, cancellationToken).ConfigureAwait(false);
            }
            else
            {
                XDocument xhtmlDocument = await GetDocumentAsync(path, cancellationToken).ConfigureAwait(false);
                HandleXhtml(xhtmlDocument);
                IFile destinationFile = outputDirectory.GetFile(path);
                Stream destinationStream = await destinationFile.OpenWriteAsync(cancellationToken).ConfigureAwait(false);
                await using (destinationStream.ConfigureAwait(false))
                {
                    await EpubXml.SaveAsync(xhtmlDocument, destinationStream, cancellationToken).ConfigureAwait(false);
                }
            }
        }
    }

    private Task<XDocument> GetOpfDocumentAsync(EpubContents contents, CancellationToken cancellationToken)
    {
        return GetDocumentAsync(contents.OpfFilePath, cancellationToken);
    }

    private async Task WriteOpfMetadataAsync(EpubContents contents, string? newCoverName, XDocument opfDocument, CancellationToken cancellationToken)
    {
        IEpubOpfMetadata metadata = EpubMetadata.ReadFromOpf(contents.Version, opfDocument);
        if (HandleMetadataAsync is not null)
        {
            await HandleMetadataAsync(metadata, cancellationToken).ConfigureAwait(false);
        }
        metadata.WriteToOpf(opfDocument, newCoverName, _mediaTypeFileExtensionsMapping);
    }

    private async Task WriteOpfFileAsync(EpubContents contents, ZipArchive outputZip, CompressionLevel compressionLevel, DateTimeOffset timestamp, string? newCoverName, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(newCoverName) && HandleMetadataAsync is null)
        {
            await CopyFileAsync(contents.OpfFilePath, outputZip, compressionLevel, timestamp, cancellationToken).ConfigureAwait(false);
        }
        else
        {
            XDocument opfDocument = await GetOpfDocumentAsync(contents, cancellationToken).ConfigureAwait(false);
            await WriteOpfMetadataAsync(contents, newCoverName, opfDocument, cancellationToken).ConfigureAwait(false);
            ZipArchiveEntry opfEntry = outputZip.CreateEntry(string.Join('/', contents.OpfFilePath), compressionLevel);
            opfEntry.LastWriteTime = timestamp;
            // TODO Async Zip
            Stream destinationOpfStream = opfEntry.Open();
            await using (destinationOpfStream.ConfigureAwait(false))
            {
                await EpubXml.SaveAsync(opfDocument, destinationOpfStream, cancellationToken).ConfigureAwait(false);
            }
        }
    }

    private async Task WriteOpfFileAsync(EpubContents contents, IDirectory outputDirectory, string? newCoverName, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(newCoverName) && HandleMetadataAsync is null)
        {
            await CopyFileAsync(contents.OpfFilePath, outputDirectory, cancellationToken).ConfigureAwait(false);
        }
        else
        {
            XDocument opfDocument = await GetOpfDocumentAsync(contents, cancellationToken).ConfigureAwait(false);
            await WriteOpfMetadataAsync(contents, newCoverName, opfDocument, cancellationToken).ConfigureAwait(false);
            IFile destinationOpfFile = outputDirectory.GetFile(contents.OpfFilePath);
            Stream destinationOpfStream = await destinationOpfFile.OpenWriteAsync(cancellationToken).ConfigureAwait(false);
            await using (destinationOpfStream.ConfigureAwait(false))
            {
                await EpubXml.SaveAsync(opfDocument, destinationOpfStream, cancellationToken).ConfigureAwait(false);
            }
        }
    }
}
