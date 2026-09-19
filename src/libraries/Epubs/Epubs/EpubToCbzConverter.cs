using FileStorage;
using FileStorage.Zip;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;
using Utils;

namespace Epubs;

public sealed class EpubToCbzConverter
{
    private readonly EpubContainer _container;

    public EpubToCbzConverter(EpubContainer container)
    {
        _container = container;
    }

    public async Task WriteAsync(Stream outputStream, CompressionLevel compressionLevel = CompressionLevel.NoCompression, CancellationToken cancellationToken = default)
    {
        EpubPackageInfo packageInfo = await _container.GetPackageInfoAsync(cancellationToken).ConfigureAwait(false);
        DateTimeOffset timestamp = GetTimestamp(packageInfo);

        ZipFileStorageOptions options = new()
        {
            Mode = ZipArchiveMode.Create,
            FixedTimestamp = timestamp,
            Compression = compressionLevel,
        };
        ZipFileStorage fileStorage = await ZipFileStorage.CreateAsync(outputStream, options, cancellationToken).ConfigureAwait(false);
        await using ConfiguredAsyncDisposable configuredZipFileStorage = fileStorage.ConfigureAwait(false);
        IDirectory directory = fileStorage.GetDirectory();
        await WriteAsync(packageInfo, directory, cancellationToken).ConfigureAwait(false);
    }

    public async Task WriteAsync(IDirectory outputDirectory, CancellationToken cancellationToken = default)
    {
        EpubPackageInfo packageInfo = await _container.GetPackageInfoAsync(cancellationToken).ConfigureAwait(false);
        await WriteAsync(packageInfo, outputDirectory, cancellationToken).ConfigureAwait(false);
    }

    private async Task WriteAsync(EpubPackageInfo packageInfo, IDirectory outputDirectory, CancellationToken cancellationToken = default)
    {
        List<IFile> imageFiles = await GetPrePaginatedImageFilesAsync(packageInfo, cancellationToken)
            .ToListAsync(cancellationToken).ConfigureAwait(false);

        await outputDirectory.EnsureIsEmptyAsync(cancellationToken).ConfigureAwait(false);

        for (int i = 0; i < imageFiles.Count; i++)
        {
            IFile imageFile = imageFiles[i];
            IFile outputFile = outputDirectory.GetFile($"{i.ToPaddedString(imageFiles.Count)}{imageFile.Extension}");
            await imageFile.CopyToAsync(outputFile, cancellationToken).ConfigureAwait(false);
        }
    }

    private static DateTimeOffset GetTimestamp(EpubPackageInfo packageInfo)
    {
        IEpubMetadata metadata = packageInfo.Metadata;
        DateTimeOffset timestamp = metadata.LastModified ?? DateTimeOffset.MinValue;
        return timestamp.Clamp(ZipConstants.MinLastWriteTime, ZipConstants.MaxLastWriteTime);
    }

    internal async IAsyncEnumerable<IFile> GetPrePaginatedImageFilesAsync(EpubPackageInfo packageInfo, [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        foreach (EpubSpineItem spineItem in packageInfo.Spine.Items)
        {
            if (!string.IsNullOrWhiteSpace(spineItem.Linear) && spineItem.Linear != "yes") continue;
            string? pageXhtmlManifestHref = packageInfo.Manifest.Items
                .Where(mi => mi.Id == spineItem.Idref)
                .Select(mi => mi.AbsoluteHref)
                .OfType<string>()
                .FirstOrDefault();
            if (string.IsNullOrWhiteSpace(pageXhtmlManifestHref)) continue;
            EpubPath pageXhtmlPath = new(pageXhtmlManifestHref);
            IDirectory pageXhtmlDirectory = _container.RootDirectory.GetDirectory(pageXhtmlPath.Parent.Parts);
            XDocument pageXhtmlDocument = await _container.LoadDocumentAsync(pageXhtmlPath, cancellationToken).ConfigureAwait(false);
            string? pageImageHref = pageXhtmlDocument
                .Element((XNamespace)EpubXmlNamespaces.Xhtml + "html")
                ?.Element((XNamespace)EpubXmlNamespaces.Xhtml + "body")
                ?.Elements((XNamespace)EpubXmlNamespaces.Svg + "svg")
                .Select(e => e.Element((XNamespace)EpubXmlNamespaces.Svg + "image")?.Attribute((XNamespace)EpubXmlNamespaces.Xlink + "href")?.Value)
                ?.FirstOrDefault(s => !string.IsNullOrWhiteSpace(s));
            if (string.IsNullOrWhiteSpace(pageImageHref)) continue;
            EpubPath pageImagePath = pageXhtmlPath.Parent.Resolve(pageImageHref);
            IFile pageImageFile = _container.RootDirectory.GetFile(pageImagePath.Parts);
            yield return pageImageFile;
        }
    }
}
