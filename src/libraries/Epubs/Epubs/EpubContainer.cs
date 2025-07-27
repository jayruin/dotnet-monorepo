using FileStorage;
using MediaTypes;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;

namespace Epubs;

public sealed class EpubContainer
{
    public EpubContainer(IDirectory rootDirectory)
    {
        RootDirectory = rootDirectory;
    }

    public IDirectory RootDirectory { get; }

    public async Task<int> GetVersionAsync(CancellationToken cancellationToken = default)
    {
        IFile opfFile = await GetOpfFileAsync(cancellationToken).ConfigureAwait(false);

        Stream stream = await opfFile.OpenReadAsync(cancellationToken).ConfigureAwait(false);
        await using ConfiguredAsyncDisposable configuredStream = stream.ConfigureAwait(false);
        XmlReaderSettings settings = new()
        {
            Async = true,
        };
        using XmlReader reader = XmlReader.Create(stream, settings);

        while (await reader.ReadAsync().ConfigureAwait(false) && reader.NodeType != XmlNodeType.Element)
        {
        }
        string? versionString = reader.GetAttribute("version");
        if (reader.NodeType != XmlNodeType.Element
            || reader.Name != "package"
            || reader.NamespaceURI != EpubXmlNamespaces.Opf
            || string.IsNullOrWhiteSpace(versionString))
        {
            throw new InvalidOperationException("Could not get epub version string from opf.");
        }

        if (!double.TryParse(versionString, out double versionDouble))
        {
            throw new InvalidOperationException("Could not parse epub version string.");
        }
        int version = (int)versionDouble;
        return version;
    }

    public async Task<EpubCover?> GetCoverAsync(CancellationToken cancellationToken = default)
    {
        int epubVersion = await GetVersionAsync(cancellationToken).ConfigureAwait(false);
        return epubVersion switch
        {
            3 => await GetEpub3CoverAsync(cancellationToken).ConfigureAwait(false)
                ?? await GetEpub2CoverAsync(cancellationToken).ConfigureAwait(false),
            2 => await GetEpub2CoverAsync(cancellationToken).ConfigureAwait(false),
            _ => throw new InvalidOperationException($"Epub version {epubVersion} is not supported."),
        };

        async Task<EpubCover?> GetEpub2CoverAsync(CancellationToken cancellationToken)
        {
            XDocument opfDocument = await GetOpfDocumentAsync(cancellationToken).ConfigureAwait(false);
            string? coverId = opfDocument
                .Element((XNamespace)EpubXmlNamespaces.Opf + "package")
                ?.Element((XNamespace)EpubXmlNamespaces.Opf + "metadata")
                ?.Elements((XNamespace)EpubXmlNamespaces.Opf + "meta")
                .FirstOrDefault(e => e.Attribute("name")?.Value == "cover")
                ?.Attribute("content")?.Value;
            if (string.IsNullOrWhiteSpace(coverId)) return null;
            XElement? coverElement = opfDocument
                .Element((XNamespace)EpubXmlNamespaces.Opf + "package")
                ?.Element((XNamespace)EpubXmlNamespaces.Opf + "manifest")
                ?.Elements((XNamespace)EpubXmlNamespaces.Opf + "item")
                .FirstOrDefault(e => e.Attribute("id")?.Value == coverId);
            if (coverElement is null) return null;
            string? mediaType = coverElement.Attribute("media-type")?.Value;
            if (string.IsNullOrWhiteSpace(mediaType)) return null;
            string? coverPath = coverElement.Attribute("href")?.Value;
            if (string.IsNullOrWhiteSpace(coverPath)) return null;
            IDirectory opfDirectory = await GetOpfDirectoryAsync(cancellationToken).ConfigureAwait(false);
            IFile coverFile = ResolveEpubPathToFile(opfDirectory, coverPath.Split('/'));
            return new EpubCover(coverFile, coverPath, mediaType);
        }

        async Task<EpubCover?> GetEpub3CoverAsync(CancellationToken cancellationToken)
        {
            XDocument opfDocument = await GetOpfDocumentAsync(cancellationToken).ConfigureAwait(false);
            XElement? coverElement = opfDocument
                .Element((XNamespace)EpubXmlNamespaces.Opf + "package")
                ?.Element((XNamespace)EpubXmlNamespaces.Opf + "manifest")
                ?.Elements((XNamespace)EpubXmlNamespaces.Opf + "item")
                .FirstOrDefault(e => e.Attribute("properties")?.Value.Split(' ').Contains("cover-image") ?? false);
            if (coverElement is null) return null;
            string? mediaType = coverElement.Attribute("media-type")?.Value;
            if (string.IsNullOrWhiteSpace(mediaType)) return null;
            string? coverPath = coverElement.Attribute("href")?.Value;
            if (string.IsNullOrWhiteSpace(coverPath)) return null;
            IDirectory opfDirectory = await GetOpfDirectoryAsync(cancellationToken).ConfigureAwait(false);
            IFile coverFile = ResolveEpubPathToFile(opfDirectory, coverPath.Split('/'));
            return new EpubCover(coverFile, coverPath, mediaType);
        }
    }

    public async Task<IEpubMetadata> GetMetadataAsync(CancellationToken cancellationToken = default)
    {
        int version = await GetVersionAsync(cancellationToken).ConfigureAwait(false);
        XDocument opfDocument = await GetOpfDocumentAsync(cancellationToken).ConfigureAwait(false);
        return EpubMetadata.ReadFromOpf(version, opfDocument);
    }

    public async Task<bool> IsPrePaginatedAsync(CancellationToken cancellationToken = default)
    {
        IFile opfFile = await GetOpfFileAsync(cancellationToken).ConfigureAwait(false);

        Stream stream = await opfFile.OpenReadAsync(cancellationToken).ConfigureAwait(false);
        await using ConfiguredAsyncDisposable configuredStream = stream.ConfigureAwait(false);
        XmlReaderSettings settings = new()
        {
            Async = true,
        };
        using XmlReader reader = XmlReader.Create(stream, settings);

        while (await reader.ReadAsync().ConfigureAwait(false))
        {
            if (reader.NodeType != XmlNodeType.Element
                || reader.NamespaceURI != EpubXmlNamespaces.Opf
                || reader.Name != "meta"
                || reader.GetAttribute("property") != "rendition:layout") continue;
            while (await reader.ReadAsync().ConfigureAwait(false) && reader.NodeType == XmlNodeType.Text)
            {
                if (await reader.GetValueAsync().ConfigureAwait(false) == "pre-paginated") return true;
            }
        }
        return false;
    }

    public EpubPackager CreatePackager(IMediaTypeFileExtensionsMapping mediaTypeFileExtensionsMapping)
        => new(this, mediaTypeFileExtensionsMapping);

    public EpubToCbzConverter CreateCbzConverter()
        => new(this);

    internal async Task<EpubContents> TraverseAsync(CancellationToken cancellationToken)
    {
        int version = await GetVersionAsync(cancellationToken).ConfigureAwait(false);
        ImmutableArray<ImmutableArray<string>>.Builder directoryPaths = ImmutableArray.CreateBuilder<ImmutableArray<string>>();
        ImmutableArray<ImmutableArray<string>>.Builder filePaths = ImmutableArray.CreateBuilder<ImmutableArray<string>>();
        await TraverseAsync(RootDirectory, [], directoryPaths, filePaths, cancellationToken).ConfigureAwait(false);

        ImmutableArray<string> mimetypeFilePath = filePaths.First(p => p.SequenceEqual(["mimetype"]));
        filePaths.Remove(mimetypeFilePath);

        string[] expectedOpfPath = (await GetOpfFilePathAsync(cancellationToken).ConfigureAwait(false)).Split('/');
        ImmutableArray<string> opfFilePath = filePaths.First(p => p.SequenceEqual(expectedOpfPath));
        filePaths.Remove(opfFilePath);

        ImmutableArray<string> opfDirectoryPath = opfFilePath.RemoveAt(opfFilePath.Length - 1);

        EpubCover? cover = await GetCoverAsync(cancellationToken).ConfigureAwait(false);
        ImmutableArray<string> expectedCoverFilePath = cover is not null
            ? ResolveEpubPath(opfDirectoryPath, cover.RelativePath)
            : [];
        ImmutableArray<string> coverFilePath = expectedCoverFilePath.Length > 0
            ? filePaths.First(p => p.SequenceEqual(expectedCoverFilePath))
            : [];
        filePaths.Remove(coverFilePath);

        XDocument opfDocument = await GetOpfDocumentAsync(cancellationToken).ConfigureAwait(false);
        ImmutableArray<ImmutableArray<string>> expectedXhtmlPaths = GetXhtmlPaths(opfDocument, opfDirectoryPath);
        ImmutableArray<ImmutableArray<string>>.Builder xhtmlPaths = ImmutableArray.CreateBuilder<ImmutableArray<string>>();
        foreach (ImmutableArray<string> expectedXhtmlPath in expectedXhtmlPaths)
        {
            ImmutableArray<string> xhtmlPath = filePaths.First(p => p.SequenceEqual(expectedXhtmlPath));
            filePaths.Remove(xhtmlPath);
            xhtmlPaths.Add(xhtmlPath);
        }

        return new()
        {
            Version = version,
            MimetypeFilePath = mimetypeFilePath,
            OpfFilePath = opfFilePath,
            CoverFilePath = coverFilePath,
            XhtmlPaths = xhtmlPaths.ToImmutable(),
            DirectoryPaths = directoryPaths.ToImmutable(),
            FilePaths = filePaths.ToImmutable(),
        };

        static async Task TraverseAsync(IDirectory parentDirectory, ImmutableArray<string> parentPath,
            IList<ImmutableArray<string>> directoryPaths, IList<ImmutableArray<string>> filePaths,
            CancellationToken cancellationToken)
        {
            await foreach (IFile file in parentDirectory.EnumerateFilesAsync(cancellationToken).ConfigureAwait(false))
            {
                filePaths.Add(parentPath.Add(file.Name));
            }
            await foreach (IDirectory directory in parentDirectory.EnumerateDirectoriesAsync(cancellationToken).ConfigureAwait(false))
            {
                IDirectory currentDirectory = parentDirectory.GetDirectory(directory.Name);
                ImmutableArray<string> currentPath = parentPath.Add(directory.Name);
                directoryPaths.Add(currentPath);
                await TraverseAsync(currentDirectory, currentPath, directoryPaths, filePaths, cancellationToken).ConfigureAwait(false);
            }
        }

        static ImmutableArray<ImmutableArray<string>> GetXhtmlPaths(XDocument opfDocument, ImmutableArray<string> opfDirectoryPath)
        {
            XElement package = opfDocument.Element((XNamespace)EpubXmlNamespaces.Opf + "package")
            ?? throw new InvalidOperationException("No package element found.");
            XElement manifest = package.Element((XNamespace)EpubXmlNamespaces.Opf + "manifest")
                ?? throw new InvalidOperationException("No manifest element found.");
            XElement spine = package.Element((XNamespace)EpubXmlNamespaces.Opf + "spine")
                ?? throw new InvalidOperationException("No spine element found.");

            ImmutableArray<ImmutableArray<string>>.Builder xhtmlPaths = ImmutableArray.CreateBuilder<ImmutableArray<string>>();
            foreach (XElement itemrefElement in spine.Elements((XNamespace)EpubXmlNamespaces.Opf + "itemref"))
            {
                string? idref = itemrefElement.Attribute("idref")?.Value;
                if (string.IsNullOrWhiteSpace(idref)) continue;
                XElement? manifestItemElement = manifest
                    .Elements((XNamespace)EpubXmlNamespaces.Opf + "item")
                    .SingleOrDefault(e => e.Attribute("id")?.Value == idref);
                if (manifestItemElement is null) continue;
                string? manifestItemMediaType = manifestItemElement.Attribute("media-type")?.Value;
                if (manifestItemMediaType != MediaType.Application.Xhtml_Xml) continue;
                string? manifestHref = manifestItemElement.Attribute("href")?.Value;
                if (string.IsNullOrWhiteSpace(manifestHref)) continue;
                ImmutableArray<string> xhtmlPath = ResolveEpubPath(opfDirectoryPath, manifestHref);
                xhtmlPaths.Add(xhtmlPath);
            }
            return xhtmlPaths.ToImmutable();
        }
    }

    internal async IAsyncEnumerable<IFile> GetPrePaginatedImageFilesAsync([EnumeratorCancellation] CancellationToken cancellationToken)
    {
        if (!await IsPrePaginatedAsync(cancellationToken).ConfigureAwait(false))
        {
            throw new InvalidOperationException("Epub is not pre-paginated.");
        }

        IDirectory opfDirectory = await GetOpfDirectoryAsync(cancellationToken).ConfigureAwait(false);
        XDocument opfDocument = await GetOpfDocumentAsync(cancellationToken).ConfigureAwait(false);
        XElement package = opfDocument.Element((XNamespace)EpubXmlNamespaces.Opf + "package")
            ?? throw new InvalidOperationException("No package element found.");
        XElement manifest = package.Element((XNamespace)EpubXmlNamespaces.Opf + "manifest")
            ?? throw new InvalidOperationException("No manifest element found.");
        XElement spine = package.Element((XNamespace)EpubXmlNamespaces.Opf + "spine")
            ?? throw new InvalidOperationException("No spine element found.");

        foreach (XElement itemrefElement in spine.Elements((XNamespace)EpubXmlNamespaces.Opf + "itemref"))
        {
            XAttribute? linearAttribute = itemrefElement.Attribute("linear");
            if (linearAttribute is not null && linearAttribute.Value != "yes") continue;
            string? idref = itemrefElement.Attribute("idref")?.Value;
            if (string.IsNullOrWhiteSpace(idref)) continue;
            string? pageXhtmlManifestHref = manifest
                .Elements((XNamespace)EpubXmlNamespaces.Opf + "item")
                .Where(e => e.Attribute("id")?.Value == idref)
                .Select(e => e.Attribute("href")?.Value)
                .OfType<string>()
                .FirstOrDefault();
            if (string.IsNullOrWhiteSpace(pageXhtmlManifestHref)) continue;
            IFile pageXhtmlFile = ResolveEpubPathToFile(opfDirectory, pageXhtmlManifestHref.Split('/'));
            IDirectory pageXhtmlDirectory = pageXhtmlFile.GetParentDirectory()
                ?? throw new InvalidOperationException($"Could not get parent directory of page file {pageXhtmlFile.FullPath}.");
            XDocument pageXhtmlDocument = await LoadDocumentAsync(pageXhtmlFile, cancellationToken).ConfigureAwait(false);
            string? pageImagePath = pageXhtmlDocument
                .Element((XNamespace)EpubXmlNamespaces.Xhtml + "html")
                ?.Element((XNamespace)EpubXmlNamespaces.Xhtml + "body")
                ?.Elements((XNamespace)EpubXmlNamespaces.Svg + "svg")
                .Select(e => e.Element((XNamespace)EpubXmlNamespaces.Svg + "image")?.Attribute((XNamespace)EpubXmlNamespaces.Xlink + "href")?.Value)
                ?.FirstOrDefault(s => !string.IsNullOrWhiteSpace(s));
            if (string.IsNullOrWhiteSpace(pageImagePath)) continue;
            IFile pageImageFile = ResolveEpubPathToFile(pageXhtmlDirectory, pageImagePath.Split('/'));
            yield return pageImageFile;
        }
    }

    private async Task<string> GetOpfFilePathAsync(CancellationToken cancellationToken)
    {
        IFile containerXml = RootDirectory.GetFile("META-INF", "container.xml");
        string? opfFilePath;
        Stream containerXmlStream = await containerXml.OpenReadAsync(cancellationToken).ConfigureAwait(false);
        await using (containerXmlStream.ConfigureAwait(false))
        {
            XDocument document = await XDocument.LoadAsync(containerXmlStream, default, cancellationToken).ConfigureAwait(false);
            opfFilePath = document
                .Element((XNamespace)EpubXmlNamespaces.Container + "container")
                ?.Element((XNamespace)EpubXmlNamespaces.Container + "rootfiles")
                ?.Element((XNamespace)EpubXmlNamespaces.Container + "rootfile")
                ?.Attribute("full-path")
                ?.Value;
        }
        if (string.IsNullOrWhiteSpace(opfFilePath))
        {
            throw new InvalidOperationException("Could not find opf file.");
        }
        return opfFilePath;
    }

    private static async Task<XDocument> LoadDocumentAsync(IFile file, CancellationToken cancellationToken)
    {
        XDocument document;
        Stream stream = await file.OpenReadAsync(cancellationToken).ConfigureAwait(false);
        await using (stream.ConfigureAwait(false))
        {
            document = await XDocument.LoadAsync(stream, default, cancellationToken).ConfigureAwait(false);
        }
        return document;
    }

    private async Task<IFile> GetOpfFileAsync(CancellationToken cancellationToken)
    {
        string opfPath = await GetOpfFilePathAsync(cancellationToken).ConfigureAwait(false);
        return RootDirectory.GetFile(opfPath.Split('/'));
    }

    private async Task<IDirectory> GetOpfDirectoryAsync(CancellationToken cancellationToken)
    {
        IFile opfFile = await GetOpfFileAsync(cancellationToken).ConfigureAwait(false);
        return opfFile.GetParentDirectory()
            ?? throw new InvalidOperationException("Could not get parent directory of opf file.");
    }

    private async Task<XDocument> GetOpfDocumentAsync(CancellationToken cancellationToken)
    {
        IFile opfFile = await GetOpfFileAsync(cancellationToken).ConfigureAwait(false);
        XDocument opfDocument = await LoadDocumentAsync(opfFile, cancellationToken).ConfigureAwait(false);
        return opfDocument;
    }

    private static ImmutableArray<string> ResolveEpubPath(ImmutableArray<string> currentDirectoryPath, string epubPath)
    {
        ImmutableArray<string> currentPath = currentDirectoryPath;
        string[] epubPathParts = epubPath.Split('/');
        foreach (string epubPathPart in epubPathParts)
        {
            currentPath = epubPath == ".."
                ? currentPath.RemoveAt(currentPath.Length - 1)
                : currentPath.Add(epubPathPart);
        }
        return currentPath;
    }

    private static IFile ResolveEpubPathToFile(IDirectory directory, params IReadOnlyList<string> epubPathParts)
    {
        IDirectory currentDirectory = directory;
        for (int i = 0; i < epubPathParts.Count - 1; i++)
        {
            string epubPathPart = epubPathParts[i];
            currentDirectory = epubPathPart == ".."
                ? currentDirectory.GetParentDirectory()
                    ?? throw new InvalidOperationException($"Could not get parent directory of {currentDirectory.FullPath}.")
                : currentDirectory.GetDirectory(epubPathPart);
        }
        return currentDirectory.GetFile(epubPathParts[^1]);
    }
}
