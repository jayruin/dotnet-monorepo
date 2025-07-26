using MediaTypes;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace Epubs;

public sealed class EpubWriter : IDisposable, IAsyncDisposable
{
    private readonly ZipArchive _zipArchive;

    private readonly string _packageDocumentPath;

    private readonly string _reservedPrefix;

    private readonly string _contentDirectory;

    private readonly MetaInfHandler _metaInfHandler;

    private readonly PackageDocumentHandler _packageDocumentHandler;

    private readonly CoverXhtmlHandler _coverXhtmlHandler;

    private readonly NavigationDocumentHandler _navigationDocumentHandler;

    private readonly NcxHandler _ncxHandler;

    private readonly ISet<string> _resourcePaths = new HashSet<string>();

    private readonly IList<EpubResource> _resources = new List<EpubResource>();

    private string? _coverHref;

    private bool _coverInSequence;

    private IReadOnlyCollection<EpubNavItem>? _toc;

    private bool _tocInSequence;

    private bool IncludeNavigationDocument
    {
        get => Version == EpubVersion.Epub3 || (Version == EpubVersion.Epub2 && _tocInSequence);
    }

    private bool IncludeNcx
    {
        get => Version == EpubVersion.Epub2 || (Version == EpubVersion.Epub3 && IncludeLegacyFeatures);
    }

    private bool IncludeLandmarks
    {
        get => IncludeStructuralComponents && Version == EpubVersion.Epub3;
    }

    private bool IncludeGuide
    {
        get => IncludeStructuralComponents && (Version == EpubVersion.Epub2 || (Version == EpubVersion.Epub3 && IncludeLegacyFeatures));
    }

    public EpubVersion Version { get; }

    public string Identifier { get; set; } = $"urn:uuid:{Guid.NewGuid()}";

    public string Title { get; set; } = "Unknown Title";

    public IReadOnlyCollection<string> Languages { get; set; } = new List<string>() { "en", };

    public IReadOnlyCollection<EpubCreator>? Creators { get; set; }

    public DateTimeOffset? Date { get; set; }

    public bool PrePaginated { get; set; }

    public DateTimeOffset Modified { get; set; } = DateTimeOffset.Now;

    public EpubDirection Direction { get; set; }

    public EpubSeries? Series { get; set; }

    public bool IncludeStructuralComponents { get; set; }

    public bool IncludeLegacyFeatures { get; set; }

    private EpubWriter(ZipArchive zipArchive, EpubVersion epubVersion, IMediaTypeFileExtensionsMapping mediaTypeFileExtensionsMapping, string reservedPrefix, string contentDirectory)
    {
        _zipArchive = zipArchive;
        Version = epubVersion;
        _reservedPrefix = reservedPrefix;
        _contentDirectory = contentDirectory;
        _packageDocumentPath = GetResourcePath($"{_reservedPrefix}package.opf");
        _metaInfHandler = new MetaInfHandler(Version);
        _packageDocumentHandler = new PackageDocumentHandler(Version, mediaTypeFileExtensionsMapping);
        _coverXhtmlHandler = new CoverXhtmlHandler(Version);
        _navigationDocumentHandler = new NavigationDocumentHandler(Version);
        _ncxHandler = new NcxHandler(Version);
    }

    public static async Task<EpubWriter> CreateAsync(Stream stream, EpubVersion epubVersion, IMediaTypeFileExtensionsMapping mediaTypeFileExtensionsMapping, string reservedPrefix = ".", string contentDirectory = "OEBPS", CancellationToken cancellationToken = default)
    {
        if (epubVersion == EpubVersion.Unknown) throw new InvalidEpubVersionException();
        // TODO Async Zip
        ZipArchive zipArchive = new(stream, ZipArchiveMode.Create, true);
        EpubWriter epubWriter = new(zipArchive, epubVersion, mediaTypeFileExtensionsMapping, reservedPrefix, contentDirectory);
        await epubWriter.WriteMimetypeAsync(cancellationToken).ConfigureAwait(false);
        await epubWriter.WriteContainerXmlAsync(cancellationToken).ConfigureAwait(false);
        return epubWriter;
    }

    public static EpubWriter Create(Stream stream, EpubVersion epubVersion, IMediaTypeFileExtensionsMapping mediaTypeFileExtensionsMapping, string reservedPrefix = ".", string contentDirectory = "OEBPS")
    {
        if (epubVersion == EpubVersion.Unknown) throw new InvalidEpubVersionException();
        ZipArchive zipArchive = new(stream, ZipArchiveMode.Create, true);
        EpubWriter epubWriter = new(zipArchive, epubVersion, mediaTypeFileExtensionsMapping, reservedPrefix, contentDirectory);
        epubWriter.WriteMimetype();
        epubWriter.WriteContainerXml();
        return epubWriter;
    }

    public async Task AddResourceAsync(Stream stream, EpubResource resource, CancellationToken cancellationToken = default)
    {
        Stream resourceStream = await CreateResourceAsync(resource, cancellationToken).ConfigureAwait(false);
        await using ConfiguredAsyncDisposable configuredResourceStream = resourceStream.ConfigureAwait(false);
        await stream.CopyToAsync(resourceStream, cancellationToken).ConfigureAwait(false);
    }

    public void AddResource(Stream stream, EpubResource resource)
    {
        using Stream resourceStream = CreateResource(resource);
        stream.CopyTo(resourceStream);
    }

    public Task<Stream> CreateResourceAsync(EpubResource resource, CancellationToken cancellationToken = default)
    {
        if (Path.GetFileNameWithoutExtension(resource.Href).StartsWith(_reservedPrefix))
        {
            throw new InvalidOperationException($"File name must not start with {_reservedPrefix}");
        }
        string resourcePath = GetResourcePath(resource.Href);
        if (!_resourcePaths.Add(resourcePath))
        {
            throw new InvalidOperationException("Resource already exists!");
        }
        _resources.Add(resource);
        // TODO Async Zip
        return Task.FromResult(_zipArchive.CreateEntry(resourcePath).Open());
    }

    public Stream CreateResource(EpubResource resource)
    {
        if (Path.GetFileNameWithoutExtension(resource.Href).StartsWith(_reservedPrefix))
        {
            throw new InvalidOperationException($"File name must not start with {_reservedPrefix}");
        }
        string resourcePath = GetResourcePath(resource.Href);
        if (!_resourcePaths.Add(resourcePath))
        {
            throw new InvalidOperationException("Resource already exists!");
        }
        _resources.Add(resource);
        return _zipArchive.CreateEntry(resourcePath).Open();
    }

    public Task<Stream> CreateRasterCoverAsync(string extension, bool inSequence, CancellationToken cancellationToken = default)
    {
        if (_coverHref is not null) throw new InvalidOperationException("Cover already added!");
        _coverHref = $"{_reservedPrefix}cover{extension}";
        _coverInSequence = inSequence;
        // TODO Async Zip
        return Task.FromResult(_zipArchive.CreateEntry(GetResourcePath(_coverHref)).Open());
    }

    public Stream CreateRasterCover(string extension, bool inSequence)
    {
        if (_coverHref is not null) throw new InvalidOperationException("Cover already added!");
        _coverHref = $"{_reservedPrefix}cover{extension}";
        _coverInSequence = inSequence;
        return _zipArchive.CreateEntry(GetResourcePath(_coverHref)).Open();
    }

    public void AddToc(IReadOnlyCollection<EpubNavItem> navItems, bool inSequence)
    {
        if (_toc is not null) throw new InvalidOperationException("Toc already added!");
        _toc = navItems;
        _tocInSequence = inSequence;
    }

    public void Dispose()
    {
        SaveChanges();
        WriteSpecialDocuments();
        _zipArchive.Dispose();
    }

    public async ValueTask DisposeAsync()
    {
        SaveChanges();
        await WriteSpecialDocumentsAsync(default).ConfigureAwait(false);
        // TODO Async Zip
        _zipArchive.Dispose();
    }

    private string GetResourcePath(string href)
    {
        return string.Join('/', _contentDirectory.Trim('/'), href.Trim('/')).Trim('/');
    }

    private async Task WriteMimetypeAsync(CancellationToken cancellationToken)
    {
        ZipArchiveEntry mimetype = _zipArchive.CreateEntry("mimetype", CompressionLevel.NoCompression);
        // TODO Async Zip
        Stream mimetypeStream = mimetype.Open();
        await using ConfiguredAsyncDisposable configuredMimetypeStream = mimetypeStream.ConfigureAwait(false);
        StreamWriter mimetypeStreamWriter = new(mimetypeStream, Encoding.ASCII);
        await using ConfiguredAsyncDisposable configuredMimetypeStreamWriter = mimetypeStreamWriter.ConfigureAwait(false);
        await mimetypeStreamWriter.WriteAsync("application/epub+zip".AsMemory(), cancellationToken).ConfigureAwait(false);
    }

    private void WriteMimetype()
    {
        ZipArchiveEntry mimetype = _zipArchive.CreateEntry("mimetype", CompressionLevel.NoCompression);
        using Stream mimetypeStream = mimetype.Open();
        using StreamWriter mimetypeStreamWriter = new(mimetypeStream, Encoding.ASCII);
        mimetypeStreamWriter.Write("application/epub+zip");
    }

    private async Task WriteContainerXmlAsync(CancellationToken cancellationToken)
    {
        XDocument document = _metaInfHandler.GetContainerXmlDocument(_packageDocumentPath);
        // TODO Async Zip
        Stream stream = _zipArchive.CreateEntry("META-INF/container.xml").Open();
        await using ConfiguredAsyncDisposable configuredStream = stream.ConfigureAwait(false);
        await EpubXml.SaveAsync(document, stream, cancellationToken).ConfigureAwait(false);
    }

    private void WriteContainerXml()
    {
        XDocument document = _metaInfHandler.GetContainerXmlDocument(_packageDocumentPath);
        using Stream stream = _zipArchive.CreateEntry("META-INF/container.xml").Open();
        EpubXml.Save(document, stream);
    }

    private async Task WriteSpecialDocumentsAsync(CancellationToken cancellationToken)
    {
        if (_coverHref is not null && _coverInSequence)
        {
            // TODO Async Zip
            Stream stream = _zipArchive.CreateEntry(GetResourcePath($"{_reservedPrefix}cover.xhtml")).Open();
            await using ConfiguredAsyncDisposable configuredStream = stream.ConfigureAwait(false);
            await EpubXml.SaveAsync(_coverXhtmlHandler.GetRasterDocument(_coverHref), stream, cancellationToken).ConfigureAwait(false);
        }
        if (_toc is not null)
        {
            if (IncludeNavigationDocument)
            {
                // TODO Async Zip
                Stream stream = _zipArchive.CreateEntry(GetResourcePath($"{_reservedPrefix}nav.xhtml")).Open();
                await using ConfiguredAsyncDisposable configuredStream = stream.ConfigureAwait(false);
                await EpubXml.SaveAsync(_navigationDocumentHandler.GetDocument(), stream, cancellationToken).ConfigureAwait(false);
            }
            if (IncludeNcx)
            {
                // TODO Async Zip
                Stream stream = _zipArchive.CreateEntry(GetResourcePath($"{_reservedPrefix}toc.ncx")).Open();
                await using ConfiguredAsyncDisposable configuredStream = stream.ConfigureAwait(false);
                await EpubXml.SaveAsync(_ncxHandler.GetDocument(), stream, cancellationToken).ConfigureAwait(false);
            }
        }
        Stream packageStream = _zipArchive.CreateEntry(GetResourcePath($"{_reservedPrefix}package.opf")).Open();
        await using ConfiguredAsyncDisposable configuredPackageStream = packageStream.ConfigureAwait(false);
        await EpubXml.SaveAsync(_packageDocumentHandler.GetDocument(), packageStream, cancellationToken).ConfigureAwait(false);
    }

    private void WriteSpecialDocuments()
    {
        if (_coverHref is not null && _coverInSequence)
        {
            using Stream stream = _zipArchive.CreateEntry(GetResourcePath($"{_reservedPrefix}cover.xhtml")).Open();
            EpubXml.Save(_coverXhtmlHandler.GetRasterDocument(_coverHref), stream);
        }
        if (_toc is not null)
        {
            if (IncludeNavigationDocument)
            {
                using Stream stream = _zipArchive.CreateEntry(GetResourcePath($"{_reservedPrefix}nav.xhtml")).Open();
                EpubXml.Save(_navigationDocumentHandler.GetDocument(), stream);
            }
            if (IncludeNcx)
            {
                using Stream stream = _zipArchive.CreateEntry(GetResourcePath($"{_reservedPrefix}toc.ncx")).Open();
                EpubXml.Save(_ncxHandler.GetDocument(), stream);
            }
        }
        using Stream packageStream = _zipArchive.CreateEntry(GetResourcePath($"{_reservedPrefix}package.opf")).Open();
        EpubXml.Save(_packageDocumentHandler.GetDocument(), packageStream);
    }

    private void SaveChanges()
    {
        SaveMetadata();
        SaveCover();
        SaveToc();
        SaveResources();
        SaveStructuralComponents();

        if (Direction == EpubDirection.LeftToRight) _packageDocumentHandler.AddLeftToRight();
        else if (Direction == EpubDirection.RightToLeft) _packageDocumentHandler.AddRightToLeft();
    }

    private void SaveMetadata()
    {
        _packageDocumentHandler.AddIdentifier(Identifier);
        _packageDocumentHandler.AddTitle(Title);
        if (Languages.Count > 0)
        {
            foreach (string language in Languages)
            {
                _packageDocumentHandler.AddLanguage(language);
            }
        }
        else
        {
            _packageDocumentHandler.AddLanguage("en");
        }
        if (Creators?.Count > 0)
        {
            foreach (EpubCreator creator in Creators)
            {
                _packageDocumentHandler.AddCreator(creator.Name, creator.Roles);
            }
        }
        if (Date is not null) _packageDocumentHandler.AddDate((DateTimeOffset)Date);
        if (PrePaginated) _packageDocumentHandler.AddPrePaginated();
        _packageDocumentHandler.AddModified(Modified);
        if (Series is not null) _packageDocumentHandler.AddSeries(Series);
    }

    private void SaveCover()
    {
        if (_coverHref is null) return;
        _packageDocumentHandler.AddItemToManifest(_coverHref, "cover-image", "cover-id");
        if (Version == EpubVersion.Epub2)
        {
            // Not in epub2 specs, but has become the de facto way to add epub2 cover
            _packageDocumentHandler.MetadataElement.Add(
                new XElement((XNamespace)EpubXmlNamespaces.Opf + "meta",
                    new XAttribute("name", "cover"),
                    new XAttribute("content", "cover-id")
                )
            );
        }
        if (_coverInSequence)
        {
            _packageDocumentHandler.AddItemToManifestAndSpine($"{_reservedPrefix}cover.xhtml", null, null, "cover-xhtml-id");
        }
    }

    private void SaveToc()
    {
        if (IncludeNavigationDocument)
        {
            if (_tocInSequence)
            {
                _packageDocumentHandler.AddItemToManifestAndSpine($"{_reservedPrefix}nav.xhtml", "nav", null, null);
            }
            else
            {
                _packageDocumentHandler.AddItemToManifest($"{_reservedPrefix}nav.xhtml", "nav", null);
            }
        }
        if (IncludeNcx)
        {
            _ncxHandler.AddIdentifier(Identifier);
            _ncxHandler.AddTitle(Title);
            _packageDocumentHandler.AddItemToManifest($"{_reservedPrefix}toc.ncx", null, "ncx-id");
            _packageDocumentHandler.AddNcx("ncx-id");
        }
        if (_coverInSequence)
        {
            EpubNavItem coverNavItem = new()
            {
                Text = "Cover",
                Reference = $"{_reservedPrefix}cover.xhtml",
            };
            SaveNavItem(coverNavItem);
        }
        if (_tocInSequence)
        {
            EpubNavItem tocNavItem = new()
            {
                Text = "Table Of Contents",
                Reference = $"{_reservedPrefix}nav.xhtml",
            };
            SaveNavItem(tocNavItem);
        }
        if (_toc?.Count > 0)
        {
            foreach (EpubNavItem navItem in _toc)
            {
                SaveNavItem(navItem);
            }
        }
    }

    private void SaveResources()
    {
        foreach (EpubResource resource in _resources)
        {
            string? manifestProperties = string.Join(' ', resource.ManifestProperties);
            if (string.IsNullOrWhiteSpace(manifestProperties)) manifestProperties = null;
            if (!resource.Href.EndsWith(".xhtml"))
            {
                _packageDocumentHandler.AddItemToManifest(resource.Href, manifestProperties, null);
            }
            else
            {
                string? spineProperties = string.Join(' ', resource.SpineProperties);
                if (string.IsNullOrWhiteSpace(spineProperties)) spineProperties = null;
                _packageDocumentHandler.AddItemToManifestAndSpine(resource.Href, manifestProperties, spineProperties, null);
            }
        }
    }

    private void SaveStructuralComponents()
    {
        EpubResource? startOfContent = _resources.FirstOrDefault(r => r.Href.EndsWith(".xhtml"));
        if (IncludeLandmarks)
        {
            if (_coverInSequence)
            {
                _navigationDocumentHandler.AddItemToLandmarks("cover", "Cover", $"{_reservedPrefix}cover.xhtml");
            }
            if (_tocInSequence)
            {
                _navigationDocumentHandler.AddItemToLandmarks("toc", "Table Of Contents", $"{_reservedPrefix}nav.xhtml");
            }
            if (startOfContent is not null)
            {
                _navigationDocumentHandler.AddItemToLandmarks("bodymatter", "Start Of Content", startOfContent.Href);
            }
        }
        if (IncludeGuide)
        {
            if (_coverInSequence)
            {
                _packageDocumentHandler.AddReferenceToGuide("cover", "Cover", $"{_reservedPrefix}cover.xhtml");
            }
            if (_tocInSequence)
            {
                _packageDocumentHandler.AddReferenceToGuide("toc", "Table Of Contents", $"{_reservedPrefix}nav.xhtml");
            }
            if (startOfContent is not null)
            {
                _navigationDocumentHandler.AddItemToLandmarks("text", "Start Of Content", startOfContent.Href);
            }
        }
    }

    private void SaveNavItem(EpubNavItem navItem)
    {
        if (IncludeNavigationDocument)
        {
            _navigationDocumentHandler.AddNavItem(navItem);
        }
        if (IncludeNcx)
        {
            _ncxHandler.AddNavItem(navItem);
        }
    }
}
