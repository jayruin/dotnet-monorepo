using FileStorage;
using FileStorage.Zip;
using MediaTypes;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.IO.Compression;
using System.Linq;
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
    internal IReadOnlyDictionary<string, string?>? FileNameOverrides { get; set; }

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

    public EpubPackager WithFileNameOverrides(IReadOnlyDictionary<string, string?> fileNameOverrides)
    {
        FileNameOverrides = fileNameOverrides;
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
        ImmutableArray<XhtmlProperties> xhtmlProperties = await WriteXhtmlFilesAsync(contents, outputDirectory, cancellationToken).ConfigureAwait(false);
        await WriteOpfAndNcxFilesAsync(contents, outputDirectory, newCoverName, xhtmlProperties, cancellationToken).ConfigureAwait(false);
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

    private async Task CopyFileAsync(ImmutableArray<string> path, IDirectory outputDirectory, CancellationToken cancellationToken)
    {
        string? joinedDestinationPath = string.Join('/', path);
        if (FileNameOverrides is not null && FileNameOverrides.TryGetValue(joinedDestinationPath, out string? joinedDestinationPathOverride))
        {
            joinedDestinationPath = joinedDestinationPathOverride;
        }
        if (string.IsNullOrWhiteSpace(joinedDestinationPath)) return;
        string[] destinationPath = joinedDestinationPath.Split('/');
        IFile sourceFile = _container.RootDirectory.GetFile(path);
        IFile destinationFile = outputDirectory.GetFile(destinationPath);
        await sourceFile.CopyToAsync(destinationFile, cancellationToken).ConfigureAwait(false);
    }

    private Task WriteMimetypeFileAsync(EpubContents contents, IDirectory outputDirectory, CancellationToken cancellationToken)
    {
        return CopyFileAsync(contents.MimetypeFilePath, outputDirectory, cancellationToken);
    }

    private async Task CopyRegularItemsAsync(EpubContents contents, IDirectory outputDirectory, CancellationToken cancellationToken)
    {
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
        ImmutableArray<string> opfDirectoryPath = contents.OpfFilePath[..^1];
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

    private Dictionary<string, string?>? GetRelativeFileNameOverrides(ImmutableArray<string> start)
    {
        return FileNameOverrides?.ToDictionary(
            kvp => string.Join('/', EpubPaths.GetRelativePath([.. kvp.Key.Split('/')], start)),
            kvp => string.IsNullOrWhiteSpace(kvp.Value)
                ? null
                : string.Join('/', EpubPaths.GetRelativePath([.. kvp.Value.Split('/')], start)));
    }

    private void AdjustXhtmlReferences(ImmutableArray<string> xhtmlPath, XDocument xhtmlDocument)
    {
        ImmutableArray<string> xhtmlDirectoryPath = xhtmlPath[..^1];
        Dictionary<string, string?>? relativeFileNameOverrides = GetRelativeFileNameOverrides(xhtmlDirectoryPath);
        if (relativeFileNameOverrides is null) return;
        XElement? html = xhtmlDocument.Element((XNamespace)EpubXmlNamespaces.Xhtml + "html");
        if (html is null) return;
        AdjustXhtmlElementReferences("script", "src", relativeFileNameOverrides, html);
        AdjustXhtmlElementReferences("link", "href", relativeFileNameOverrides, html);
        AdjustXhtmlElementReferences("a", "href", relativeFileNameOverrides, html);
        AdjustXhtmlElementReferences("img", "src", relativeFileNameOverrides, html);
    }

    private static void AdjustElementReference(XElement element, XName attributeName,
        Dictionary<string, string?> relativeFileNameOverrides)
    {
        string? reference = element.Attribute(attributeName)?.Value;
        if (string.IsNullOrWhiteSpace(reference)) return;
        string[] referenceParts = reference.Split('#');
        string path = referenceParts[0];
        if (relativeFileNameOverrides.TryGetValue(path, out string? overridePath))
        {
            if (string.IsNullOrWhiteSpace(overridePath))
            {
                element.Remove();
            }
            else
            {
                string overrideReference = string.Join('#', [overridePath, .. referenceParts[1..]]);
                element.SetAttributeValue(attributeName, overrideReference);
            }
        }
    }

    private static void AdjustXhtmlElementReferences(string elementName, string attributeName,
        Dictionary<string, string?> relativeFileNameOverrides,
        XElement htmlElement)
    {
        foreach (XElement element in htmlElement.Descendants((XNamespace)EpubXmlNamespaces.Xhtml + elementName).ToList())
        {
            AdjustElementReference(element, attributeName, relativeFileNameOverrides);
        }
    }

    private async Task<ImmutableArray<XhtmlProperties>> WriteXhtmlFilesAsync(EpubContents contents, IDirectory outputDirectory, CancellationToken cancellationToken)
    {
        ImmutableArray<XhtmlProperties>.Builder properties = ImmutableArray.CreateBuilder<XhtmlProperties>();
        foreach (ImmutableArray<string> path in contents.XhtmlPaths)
        {
            if (HandleXhtml is null && FileNameOverrides is null)
            {
                await CopyFileAsync(path, outputDirectory, cancellationToken).ConfigureAwait(false);
            }
            else
            {
                ImmutableArray<string> destinationPath = path;
                if (FileNameOverrides is not null && FileNameOverrides.TryGetValue(string.Join('/', path), out string? joinedDestinationPathOverride))
                {
                    if (string.IsNullOrWhiteSpace(joinedDestinationPathOverride)) continue;
                    destinationPath = [.. joinedDestinationPathOverride.Split('/')];
                }
                XDocument xhtmlDocument = await GetDocumentAsync(path, cancellationToken).ConfigureAwait(false);
                if (HandleXhtml is not null)
                {
                    HandleXhtml(xhtmlDocument);
                }
                if (FileNameOverrides is not null)
                {
                    AdjustXhtmlReferences(path, xhtmlDocument);
                }
                properties.Add(new()
                {
                    Path = path,
                    IsScripted = xhtmlDocument
                        .Element((XNamespace)EpubXmlNamespaces.Xhtml + "html")
                        ?.Descendants((XNamespace)EpubXmlNamespaces.Xhtml + "script")
                        ?.Any()
                        ?? false,
                });
                IFile destinationFile = outputDirectory.GetFile(destinationPath);
                Stream destinationStream = await destinationFile.OpenWriteAsync(cancellationToken).ConfigureAwait(false);
                await using (destinationStream.ConfigureAwait(false))
                {
                    await EpubXml.SaveAsync(xhtmlDocument, destinationStream, cancellationToken).ConfigureAwait(false);
                }
            }
        }
        return properties.ToImmutable();
    }

    private Task<XDocument> GetOpfDocumentAsync(EpubContents contents, CancellationToken cancellationToken)
    {
        return GetDocumentAsync(contents.OpfFilePath, cancellationToken);
    }

    private async Task<XDocument?> GetNcxDocumentAsync(EpubContents contents, CancellationToken cancellationToken)
    {
        return contents.NcxFilePath.Length > 0
            ? await GetDocumentAsync(contents.NcxFilePath, cancellationToken).ConfigureAwait(false)
            : null;
    }

    private async Task<IEpubMetadata> WriteOpfMetadataAsync(EpubContents contents, string? newCoverName, XDocument opfDocument, CancellationToken cancellationToken)
    {
        IEpubOpfMetadata metadata = EpubMetadata.ReadFromOpf(contents.Version, opfDocument);
        if (HandleMetadataAsync is not null)
        {
            await HandleMetadataAsync(metadata, cancellationToken).ConfigureAwait(false);
        }
        metadata.WriteToOpf(opfDocument, newCoverName, _mediaTypeFileExtensionsMapping);
        return metadata;
    }

    private void AdjustOpfReferences(EpubContents contents, XDocument opfDocument, ImmutableArray<XhtmlProperties> xhtmlProperties)
    {
        ImmutableArray<string> opfDirectoryPath = contents.OpfFilePath[..^1];
        Dictionary<string, string?>? relativeFileNameOverrides = GetRelativeFileNameOverrides(opfDirectoryPath);
        if (relativeFileNameOverrides is null) return;
        XElement package = opfDocument.Element((XNamespace)EpubXmlNamespaces.Opf + "package")
            ?? throw new InvalidOperationException("Could not get package element.");
        XElement manifest = package.Element((XNamespace)EpubXmlNamespaces.Opf + "manifest")
            ?? throw new InvalidOperationException("Could not get manifest element.");
        foreach (XElement item in manifest.Elements((XNamespace)EpubXmlNamespaces.Opf + "item").ToList())
        {
            AdjustElementReference(item, "href", relativeFileNameOverrides);
            string? hrefPath = item.Attribute("href")?.Value?.Split('#')?[0];
            if (string.IsNullOrWhiteSpace(hrefPath)) continue;
            XhtmlProperties? matchingProperties = xhtmlProperties
                .FirstOrDefault(p => string.Join('/', EpubPaths.GetRelativePath(p.Path, opfDirectoryPath)) == hrefPath);
            if (matchingProperties is null) continue;
            if (contents.Version != 3) continue;
            HashSet<string> properties = item.Attribute("properties")?.Value?.Split(' ')?.ToHashSet() ?? [];
            if (matchingProperties.IsScripted)
            {
                properties.Add("scripted");
            }
            else
            {
                properties.Remove("scripted");
            }
            if (properties.Count > 0)
            {
                item.SetAttributeValue("properties", string.Join(' ', properties.Order()));
            }
            else
            {
                item.Attribute("properties")?.Remove();
            }
        }
        XElement? guide = package.Element((XNamespace)EpubXmlNamespaces.Opf + "guide");
        foreach (XElement reference in guide?.Elements((XNamespace)EpubXmlNamespaces.Opf + "reference")?.ToList() ?? [])
        {
            AdjustElementReference(reference, "href", relativeFileNameOverrides);
        }
    }

    private void WriteMetadataToNcx(IEpubMetadata metadata, XDocument ncxDocument)
    {
        XElement? uidElement = ncxDocument
            .Element((XNamespace)EpubXmlNamespaces.Ncx + "ncx")
            ?.Element((XNamespace)EpubXmlNamespaces.Ncx + "head")
            ?.Elements((XNamespace)EpubXmlNamespaces.Ncx + "meta")
            .FirstOrDefault(e => e.Attribute("name")?.Value == "dtb:uid");
        uidElement?.SetAttributeValue("content", metadata.Identifier);
    }

    private void AdjustNcxReferences(EpubContents contents, XDocument ncxDocument)
    {
        ImmutableArray<string> ncxDirectoryPath = contents.NcxFilePath[..^1];
        Dictionary<string, string?>? relativeFileNameOverrides = GetRelativeFileNameOverrides(ncxDirectoryPath);
        if (relativeFileNameOverrides is null) return;
        foreach (XElement contentElement in ncxDocument
            .Element((XNamespace)EpubXmlNamespaces.Ncx + "ncx")
            ?.Element((XNamespace)EpubXmlNamespaces.Ncx + "navMap")
            ?.Descendants((XNamespace)EpubXmlNamespaces.Ncx + "content")
            ?? [])
        {
            AdjustElementReference(contentElement, "src", relativeFileNameOverrides);
        }
    }

    private async Task WriteOpfAndNcxFilesAsync(EpubContents contents, IDirectory outputDirectory, string? newCoverName, ImmutableArray<XhtmlProperties> xhtmlProperties, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(newCoverName) && HandleMetadataAsync is null && FileNameOverrides is null)
        {
            await CopyFileAsync(contents.OpfFilePath, outputDirectory, cancellationToken).ConfigureAwait(false);
            if (contents.NcxFilePath.Length > 0)
            {
                await CopyFileAsync(contents.NcxFilePath, outputDirectory, cancellationToken).ConfigureAwait(false);
            }
        }
        else
        {
            XDocument opfDocument = await GetOpfDocumentAsync(contents, cancellationToken).ConfigureAwait(false);
            IEpubMetadata metadata = await WriteOpfMetadataAsync(contents, newCoverName, opfDocument, cancellationToken).ConfigureAwait(false);
            AdjustOpfReferences(contents, opfDocument, xhtmlProperties);
            IFile destinationOpfFile = outputDirectory.GetFile(contents.OpfFilePath);
            Stream destinationOpfStream = await destinationOpfFile.OpenWriteAsync(cancellationToken).ConfigureAwait(false);
            await using (destinationOpfStream.ConfigureAwait(false))
            {
                await EpubXml.SaveAsync(opfDocument, destinationOpfStream, cancellationToken).ConfigureAwait(false);
            }

            XDocument? ncxDocument = await GetNcxDocumentAsync(contents, cancellationToken).ConfigureAwait(false);
            if (ncxDocument is not null)
            {
                WriteMetadataToNcx(metadata, ncxDocument);
                AdjustNcxReferences(contents, ncxDocument);
                IFile destinationNcxFile = outputDirectory.GetFile(contents.NcxFilePath);
                Stream destinationNcxStream = await destinationNcxFile.OpenWriteAsync(cancellationToken).ConfigureAwait(false);
                await using (destinationNcxStream.ConfigureAwait(false))
                {
                    await EpubXml.SaveAsync(ncxDocument, destinationNcxStream, cancellationToken).ConfigureAwait(false);
                }
            }
        }
    }

    private sealed class XhtmlProperties
    {
        public required ImmutableArray<string> Path { get; init; }
        public required bool IsScripted { get; init; }
    }
}
