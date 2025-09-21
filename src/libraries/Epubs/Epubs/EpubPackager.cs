using FileStorage;
using FileStorage.Zip;
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

    public EpubPackager(EpubContainer container, IMediaTypeFileExtensionsMapping mediaTypeFileExtensionsMapping)
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

        ZipFileStorageOptions options = new()
        {
            Mode = ZipArchiveMode.Create,
            FixedTimestamp = timestamp,
            Compression = compressionLevel,
            CompressionOverrides = [("mimetype", CompressionLevel.NoCompression)],
        };
        // TODO Async Zip
        using ZipFileStorage fileStorage = new(outputStream, options);
        IDirectory directory = fileStorage.GetDirectory();
        await PackageAsync(directory, cancellationToken).ConfigureAwait(false);
    }

    public async Task PackageAsync(IDirectory outputDirectory, CancellationToken cancellationToken = default)
    {
        EpubContents contents = await _container.TraverseAsync(cancellationToken).ConfigureAwait(false);

        await outputDirectory.EnsureIsEmptyAsync(cancellationToken).ConfigureAwait(false);

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

    private Task CopyFileAsync(ImmutableArray<string> path, IDirectory outputDirectory, CancellationToken cancellationToken)
    {
        IFile sourceFile = _container.RootDirectory.GetFile(path);
        IFile destinationFile = outputDirectory.GetFile(path);
        return sourceFile.CopyToAsync(destinationFile, cancellationToken);
    }

    private Task WriteMimetypeFileAsync(EpubContents contents, IDirectory outputDirectory, CancellationToken cancellationToken)
    {
        return CopyFileAsync(contents.MimetypeFilePath, outputDirectory, cancellationToken);
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
