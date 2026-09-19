using FileStorage;
using FileStorage.Zip;
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

public sealed class EpubImageExtractor
{
    private readonly EpubContainer _container;

    public EpubImageExtractor(EpubContainer container)
    {
        _container = container;
    }

    public async Task WriteAsync(Stream outputStream, CancellationToken cancellationToken = default)
    {
        ZipFileStorageOptions zipOptions = new()
        {
            Mode = ZipArchiveMode.Create,
            FixedTimestamp = ZipConstants.MinLastWriteTime,
            Compression = CompressionLevel.NoCompression,
        };
        ZipFileStorage zipFileStorage = await ZipFileStorage.CreateAsync(outputStream, zipOptions, cancellationToken).ConfigureAwait(false);
        await using ConfiguredAsyncDisposable configuredImagesZipFileStorage = zipFileStorage.ConfigureAwait(false);
        IDirectory imagesDirectory = zipFileStorage.GetDirectory();
        await WriteAsync(imagesDirectory, cancellationToken).ConfigureAwait(false);
    }

    public async Task WriteAsync(IDirectory outputDirectory, CancellationToken cancellationToken = default)
    {
        EpubPackageInfo packageInfo = await _container.GetPackageInfoAsync(cancellationToken).ConfigureAwait(false);
        List<IFile> imageFiles = await EnumerateImageFilesAsync(packageInfo, cancellationToken)
            .ToListAsync(cancellationToken).ConfigureAwait(false);

        await outputDirectory.EnsureIsEmptyAsync(cancellationToken).ConfigureAwait(false);

        for (int i = 0; i < imageFiles.Count; i++)
        {
            IFile imageFile = imageFiles[i];
            IFile outputFile = outputDirectory.GetFile($"{(i + 1).ToPaddedString(imageFiles.Count)}{imageFile.Extension}");
            await imageFile.CopyToAsync(outputFile, cancellationToken).ConfigureAwait(false);
        }
    }

    public async Task WriteAsync(IFile outputFile, CancellationToken cancellationToken = default)
    {
        Stream stream = await outputFile.OpenWriteAsync(cancellationToken).ConfigureAwait(false);
        await using ConfiguredAsyncDisposable configuredStream = stream.ConfigureAwait(false);
        await WriteAsync(stream, cancellationToken).ConfigureAwait(false);
    }

    private async IAsyncEnumerable<IFile> EnumerateImageFilesAsync(EpubPackageInfo packageInfo, [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        foreach (EpubPath xhtmlPath in packageInfo.GetOrderedXhtmlPaths(true))
        {
            EpubPath xhtmlDirectoryPath = xhtmlPath.Parent;
            XDocument xhtmlDocument = await _container.LoadDocumentAsync(xhtmlPath, cancellationToken).ConfigureAwait(false);
            foreach (XElement img in xhtmlDocument
                .Element((XNamespace)EpubXmlNamespaces.Xhtml + "html")
                ?.Element((XNamespace)EpubXmlNamespaces.Xhtml + "body")
                ?.Descendants((XNamespace)EpubXmlNamespaces.Xhtml + "img")
                ?? [])
            {
                string? src = img.Attribute("src")?.Value;
                if (string.IsNullOrWhiteSpace(src)) continue;
                EpubPath imagePath = xhtmlDirectoryPath.Resolve(src);
                IFile imageFile = _container.RootDirectory.GetFile(imagePath.Parts);
                yield return imageFile;
            }
        }
    }
}
