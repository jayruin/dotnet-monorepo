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

    public async Task<int> PeekVersionAsync(CancellationToken cancellationToken = default)
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
            || reader.LocalName != "package"
            || reader.NamespaceURI != EpubXmlNamespaces.Opf
            || string.IsNullOrWhiteSpace(versionString))
        {
            throw new InvalidOperationException("Could not get epub version string from opf.");
        }

        int version = ParseEpubVersion(versionString);
        return version;
    }

    public async Task<EpubPackageInfo> GetPackageInfoAsync(CancellationToken cancellationToken = default)
    {
        EpubPath opfFilePath = await GetOpfFilePathAsync(cancellationToken).ConfigureAwait(false);
        XDocument opfDocument = await LoadDocumentAsync(opfFilePath, cancellationToken).ConfigureAwait(false);
        string? versionString = opfDocument
            .Element((XNamespace)EpubXmlNamespaces.Opf + "package")
            ?.Attribute("version")
            ?.Value;
        int version = ParseEpubVersion(versionString);
        EpubCover? cover = GetEpubCover(opfDocument, opfFilePath, version);
        IEpubMetadata metadata = EpubMetadata.ReadFromOpf(version, opfDocument);
        EpubManifest manifest = GetManifest(opfDocument, opfFilePath);
        EpubSpine spine = GetSpine(opfDocument);
        return new()
        {
            Version = version,
            OpfFilePath = opfFilePath,
            Cover = cover,
            Metadata = metadata,
            Manifest = manifest,
            Spine = spine,
        };
    }

    public async Task<bool> PeekIsPrePaginatedAsync(CancellationToken cancellationToken = default)
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

    internal async Task<EpubContents> TraverseAsync(CancellationToken cancellationToken)
    {
        EpubPackageInfo packageInfo = await GetPackageInfoAsync(cancellationToken).ConfigureAwait(false);

        int version = packageInfo.Version;
        ImmutableArray<EpubPath>.Builder filePaths = ImmutableArray.CreateBuilder<EpubPath>();
        await TraverseAsync(RootDirectory, new(), filePaths, cancellationToken).ConfigureAwait(false);

        EpubPath expectedMimetypeFilePath = new("mimetype");
        EpubPath mimetypeFilePath = filePaths.First(p => p.Equals(expectedMimetypeFilePath));
        filePaths.Remove(mimetypeFilePath);

        EpubPath expectedOpfPath = packageInfo.OpfFilePath;
        EpubPath opfFilePath = filePaths.First(p => p.Equals(expectedOpfPath));
        filePaths.Remove(opfFilePath);

        EpubCover? cover = packageInfo.Cover;
        EpubPath expectedCoverFilePath = cover?.Path ?? new();
        EpubPath coverFilePath = !expectedCoverFilePath.IsEmpty
            ? filePaths.First(p => p.Equals(expectedCoverFilePath))
            : new();
        filePaths.Remove(coverFilePath);

        EpubPath expectedNcxFilePath = GetNcxFilePath(packageInfo) ?? new();
        EpubPath ncxFilePath = !expectedNcxFilePath.IsEmpty
            ? filePaths.First(p => p.Equals(expectedNcxFilePath))
            : new();
        filePaths.Remove(ncxFilePath);

        ImmutableArray<EpubPath> expectedXhtmlPaths = GetXhtmlPaths(packageInfo);
        ImmutableArray<EpubPath>.Builder xhtmlPaths = ImmutableArray.CreateBuilder<EpubPath>();
        foreach (EpubPath expectedXhtmlPath in expectedXhtmlPaths)
        {
            EpubPath xhtmlPath = filePaths.First(p => p.Equals(expectedXhtmlPath));
            filePaths.Remove(xhtmlPath);
            xhtmlPaths.Add(xhtmlPath);
        }

        return new()
        {
            Version = version,
            MimetypeFilePath = mimetypeFilePath,
            OpfFilePath = opfFilePath,
            CoverFilePath = coverFilePath,
            NcxFilePath = ncxFilePath,
            XhtmlPaths = xhtmlPaths.ToImmutable(),
            FilePaths = filePaths.ToImmutable(),
        };

        static async Task TraverseAsync(IDirectory parentDirectory, EpubPath parentPath,
            IList<EpubPath> filePaths,
            CancellationToken cancellationToken)
        {
            await foreach (IFile file in parentDirectory.EnumerateFilesAsync(cancellationToken).ConfigureAwait(false))
            {
                filePaths.Add(parentPath.Resolve(file.Name));
            }
            await foreach (IDirectory directory in parentDirectory.EnumerateDirectoriesAsync(cancellationToken).ConfigureAwait(false))
            {
                IDirectory currentDirectory = parentDirectory.GetDirectory(directory.Name);
                EpubPath currentPath = parentPath.Resolve(directory.Name);
                await TraverseAsync(currentDirectory, currentPath, filePaths, cancellationToken).ConfigureAwait(false);
            }
        }

        static ImmutableArray<EpubPath> GetXhtmlPaths(EpubPackageInfo packageInfo)
        {
            ImmutableArray<EpubPath>.Builder xhtmlPaths = ImmutableArray.CreateBuilder<EpubPath>();
            foreach (EpubManifestItem manifestItem in packageInfo.Manifest.Items)
            {
                string? manifestItemMediaType = manifestItem.MediaType;
                if (manifestItemMediaType != MediaType.Application.Xhtml_Xml) continue;
                EpubPath xhtmlPath = new(manifestItem.AbsoluteHref);
                xhtmlPaths.Add(xhtmlPath);
            }
            return xhtmlPaths.ToImmutable();
        }
    }

    internal Task<XDocument> LoadDocumentAsync(EpubPath path, CancellationToken cancellationToken)
        => LoadDocumentAsync(RootDirectory.GetFile(path.Parts), cancellationToken);

    private async Task<EpubPath> GetOpfFilePathAsync(CancellationToken cancellationToken)
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
        return new(opfFilePath);
    }

    private async Task<IFile> GetOpfFileAsync(CancellationToken cancellationToken)
    {
        EpubPath opfPath = await GetOpfFilePathAsync(cancellationToken).ConfigureAwait(false);
        return RootDirectory.GetFile(opfPath.Parts);
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

    private static EpubPath? GetNcxFilePath(EpubPackageInfo packageInfo)
    {
        string? ncxId = packageInfo.Spine.Toc;
        if (string.IsNullOrWhiteSpace(ncxId)) return null;
        string? href = packageInfo.Manifest.Items
            .FirstOrDefault(mi => mi.Id == ncxId)
            ?.AbsoluteHref;
        if (string.IsNullOrWhiteSpace(href)) return null;
        return new EpubPath(href);
    }

    private static int ParseEpubVersion(string? versionString)
    {
        if (!double.TryParse(versionString, out double versionDouble))
        {
            throw new InvalidOperationException("Could not parse epub version string.");
        }
        int version = (int)versionDouble;
        return version;
    }

    private EpubCover? GetEpubCover(XDocument opfDocument, EpubPath opfFilePath, int epubVersion)
    {
        return epubVersion switch
        {
            3 => GetEpub3Cover(opfDocument, opfFilePath)
                ?? GetEpub2Cover(opfDocument, opfFilePath),
            2 => GetEpub2Cover(opfDocument, opfFilePath),
            _ => throw new InvalidOperationException($"Epub version {epubVersion} is not supported."),
        };
    }

    private EpubCover? GetEpub2Cover(XDocument opfDocument, EpubPath opfFilePath)
    {
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
        EpubPath coverFilePath = opfFilePath.Parent.Resolve(coverPath);
        IFile coverFile = RootDirectory.GetFile(coverFilePath.Parts);
        return new EpubCover(coverFile, coverFilePath, mediaType);
    }

    private EpubCover? GetEpub3Cover(XDocument opfDocument, EpubPath opfFilePath)
    {
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
        EpubPath coverFilePath = opfFilePath.Parent.Resolve(coverPath);
        IFile coverFile = RootDirectory.GetFile(coverFilePath.Parts);
        return new EpubCover(coverFile, coverFilePath, mediaType);
    }

    private static EpubManifest GetManifest(XDocument opfDocument, EpubPath opfFilePath)
    {
        ImmutableArray<EpubManifestItem>.Builder builder = ImmutableArray.CreateBuilder<EpubManifestItem>();
        EpubPath opfDirectoryPath = opfFilePath.Parent;
        foreach (XElement element in opfDocument
            .Element((XNamespace)EpubXmlNamespaces.Opf + "package")
            ?.Element((XNamespace)EpubXmlNamespaces.Opf + "manifest")
            ?.Elements((XNamespace)EpubXmlNamespaces.Opf + "item")
            ?? [])
        {
            string? id = element.Attribute("id")?.Value;
            string? href = element.Attribute("href")?.Value;
            string? mediaType = element.Attribute("media-type")?.Value;
            string[] properties = element.Attribute("properties")?.Value?.Split(' ') ?? [];
            if (string.IsNullOrWhiteSpace(id) || string.IsNullOrWhiteSpace(href) || string.IsNullOrWhiteSpace(mediaType))
            {
                continue;
            }
            string absoluteHref = opfDirectoryPath.Resolve(href).ToString();
            builder.Add(new()
            {
                Id = id,
                AbsoluteHref = absoluteHref,
                MediaType = mediaType,
                Properties = [.. properties],
            });
        }
        return new()
        {
            Items = builder.ToImmutable(),
        };
    }

    private static EpubSpine GetSpine(XDocument opfDocument)
    {
        ImmutableArray<EpubSpineItem>.Builder builder = ImmutableArray.CreateBuilder<EpubSpineItem>();
        foreach (XElement element in opfDocument
            .Element((XNamespace)EpubXmlNamespaces.Opf + "package")
            ?.Element((XNamespace)EpubXmlNamespaces.Opf + "spine")
            ?.Elements((XNamespace)EpubXmlNamespaces.Opf + "itemref")
            ?? [])
        {
            string? idref = element.Attribute("idref")?.Value;
            if (string.IsNullOrWhiteSpace(idref)) throw new InvalidOperationException("Missing or invalid idref.");
            string? linear = element.Attribute("linear")?.Value;
            string[] properties = element.Attribute("properties")?.Value?.Split(' ') ?? [];
            builder.Add(new()
            {
                Idref = idref,
                Linear = linear,
                Properties = [.. properties],
            });
        }
        string? toc = opfDocument
            .Element((XNamespace)EpubXmlNamespaces.Opf + "package")
            ?.Element((XNamespace)EpubXmlNamespaces.Opf + "spine")
            ?.Attribute("toc")
            ?.Value;
        return new()
        {
            Toc = toc,
            Items = builder.ToImmutable(),
        };
    }
}
