using FileStorage;
using FileStorage.Zip;
using ImgProj.Covers;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.IO.Compression;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Utils;

namespace ImgProj.Exporting;

public sealed class CbzExporter : IExporter, IDirectoryExporter
{
    private static readonly CompressionLevel Compression = CompressionLevel.NoCompression;

    private readonly ICoverGenerator _coverGenerator;

    public ExportFormat ExportFormat { get; } = ExportFormat.Cbz;

    public CbzExporter(ICoverGenerator coverGenerator)
    {
        _coverGenerator = coverGenerator;
    }

    public async Task ExportAsync(IImgProject project, Stream stream, ImmutableArray<int> coordinates, string? version, CancellationToken cancellationToken = default)
    {
        IImgProject subProject = project.GetSubProject(coordinates);
        IMetadataVersion metadata = subProject.MetadataVersions[version ?? subProject.MainVersion];
        DateTimeOffset timestamp = metadata.Timestamp ?? System.DateTimeOffset.MinValue;
        timestamp = timestamp.Clamp(ZipConstants.MinLastWriteTime, ZipConstants.MaxLastWriteTime);
        ZipFileStorageOptions options = new()
        {
            Mode = ZipArchiveMode.Create,
            FixedTimestamp = timestamp,
            Compression = Compression,
        };
        ZipFileStorage fileStorage = await ZipFileStorage.CreateAsync(stream, options, cancellationToken).ConfigureAwait(false);
        await using ConfiguredAsyncDisposable configuredZipFileStorage = fileStorage.ConfigureAwait(false);
        IDirectory directory = fileStorage.GetDirectory();
        await ExportAsync(project, directory, coordinates, version, cancellationToken).ConfigureAwait(false);
    }

    public async Task ExportAsync(IImgProject project, IDirectory directory, ImmutableArray<int> coordinates, string? version, CancellationToken cancellationToken = default)
    {
        IImgProject subProject = project.GetSubProject(coordinates);
        version ??= subProject.MainVersion;
        List<IPage> pages = [];
        IPage? cover = await _coverGenerator.CreateCoverGridPageAsync(subProject, version, cancellationToken).ConfigureAwait(false);
        if (cover is not null)
        {
            pages.Add(cover);
        }
        await foreach (IPage page in subProject.EnumeratePagesAsync(version, true, cancellationToken).ConfigureAwait(false))
        {
            pages.Add(page);
        }
        int pageCount = pages.Count;
        int pageNumber = 1;
        foreach (IPage page in pages)
        {
            IFile file = directory.GetFile($"{pageNumber.ToPaddedString(pageCount)}{page.Extension}");
            Stream sourceStream = await page.OpenReadAsync(cancellationToken).ConfigureAwait(false);
            await using ConfiguredAsyncDisposable configuredSourceStream = sourceStream.ConfigureAwait(false);
            Stream destinationStream = await file.OpenWriteAsync(cancellationToken).ConfigureAwait(false);
            await using ConfiguredAsyncDisposable configuredDestinationStream = destinationStream.ConfigureAwait(false);
            await sourceStream.CopyToAsync(destinationStream, cancellationToken).ConfigureAwait(false);
            pageNumber += 1;
        }
    }
}
