using FileStorage;
using FileStorage.Zip;
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
    private readonly IDirectory _rootDirectory;
    private readonly EpubWriterOptions _options;
    private readonly ZipFileStorage? _zipFileStorage;
    private readonly string _coverXhtmlFileName;
    private readonly string _navXhtmlFileName;
    private readonly string _tocNcxFileName;
    private readonly string _packageDocumentPath;
    private readonly MetaInfHandler _metaInfHandler;
    private readonly PackageDocumentHandler _packageDocumentHandler;
    private readonly CoverXhtmlHandler _coverXhtmlHandler;
    private readonly NavigationDocumentHandler _navigationDocumentHandler;
    private readonly NcxHandler _ncxHandler;
    private readonly HashSet<string> _resourcePaths = [];
    private readonly List<EpubResource> _resources = [];
    private string? _coverHref;
    private bool _coverInSequence;
    private IReadOnlyCollection<EpubNavItem>? _toc;
    private bool _tocInSequence;

    private bool IncludeNavigationDocument
    {
        get => _options.Version == EpubVersion.Epub3 || (_options.Version == EpubVersion.Epub2 && _tocInSequence);
    }

    private bool IncludeNcx
    {
        get => _options.Version == EpubVersion.Epub2 || (_options.Version == EpubVersion.Epub3 && IncludeLegacyFeatures);
    }

    private bool IncludeLandmarks
    {
        get => IncludeStructuralComponents && _options.Version == EpubVersion.Epub3;
    }

    private bool IncludeGuide
    {
        get => IncludeStructuralComponents && (_options.Version == EpubVersion.Epub2 || (_options.Version == EpubVersion.Epub3 && IncludeLegacyFeatures));
    }

    public string Identifier { get; set; } = $"urn:uuid:{Guid.NewGuid()}";

    public string Title { get; set; } = "Unknown Title";

    public IReadOnlyCollection<string> Languages { get; set; } = new List<string>() { "en", };

    public IReadOnlyCollection<EpubCreator>? Creators { get; set; }

    public DateTimeOffset? Date { get; set; }

    public bool PrePaginated { get; set; }

    public EpubDirection Direction { get; set; }

    public EpubSeries? Series { get; set; }

    public bool IncludeStructuralComponents { get; set; }

    public bool IncludeLegacyFeatures { get; set; }

    private EpubWriter(IDirectory rootDirectory, EpubWriterOptions options, IMediaTypeFileExtensionsMapping mediaTypeFileExtensionsMapping, ZipFileStorage? zipFileStorage)
    {
        _rootDirectory = rootDirectory;
        _options = options;
        _zipFileStorage = zipFileStorage;
        _coverXhtmlFileName = $"{_options.ReservedPrefix}cover.xhtml";
        _navXhtmlFileName = $"{_options.ReservedPrefix}nav.xhtml";
        _tocNcxFileName = $"{_options.ReservedPrefix}toc.ncx";
        _packageDocumentPath = GetResourcePath($"{_options.ReservedPrefix}package.opf");
        _metaInfHandler = new MetaInfHandler(_options.Version);
        _packageDocumentHandler = new PackageDocumentHandler(_options.Version, mediaTypeFileExtensionsMapping);
        _coverXhtmlHandler = new CoverXhtmlHandler(_options.Version);
        _navigationDocumentHandler = new NavigationDocumentHandler(_options.Version);
        _ncxHandler = new NcxHandler(_options.Version);
    }

    public static async Task<EpubWriter> CreateAsync(Stream stream, EpubWriterOptions options, IMediaTypeFileExtensionsMapping mediaTypeFileExtensionsMapping, CancellationToken cancellationToken = default)
    {
        if (options.Version == EpubVersion.Unknown) throw new InvalidEpubVersionException();
        ZipFileStorageOptions zipFileStorageOptions = new()
        {
            Mode = ZipArchiveMode.Create,
            FixedTimestamp = options.Modified,
            Compression = options.Compression,
            CompressionOverrides = [("mimetype", CompressionLevel.NoCompression)],
        };
        ZipFileStorage zipFileStorage = new(stream, zipFileStorageOptions);
        EpubWriter epubWriter = new(zipFileStorage.GetDirectory(), options, mediaTypeFileExtensionsMapping, zipFileStorage);
        await epubWriter.WriteMimetypeAsync(cancellationToken).ConfigureAwait(false);
        await epubWriter.WriteContainerXmlAsync(cancellationToken).ConfigureAwait(false);
        return epubWriter;
    }

    public static async Task<EpubWriter> CreateAsync(IDirectory directory, EpubWriterOptions options, IMediaTypeFileExtensionsMapping mediaTypeFileExtensionsMapping, CancellationToken cancellationToken = default)
    {
        if (options.Version == EpubVersion.Unknown) throw new InvalidEpubVersionException();
        EpubWriter epubWriter = new(directory, options, mediaTypeFileExtensionsMapping, null);
        await epubWriter.WriteMimetypeAsync(cancellationToken).ConfigureAwait(false);
        await epubWriter.WriteContainerXmlAsync(cancellationToken).ConfigureAwait(false);
        return epubWriter;
    }

    public static EpubWriter Create(Stream stream, EpubWriterOptions options, IMediaTypeFileExtensionsMapping mediaTypeFileExtensionsMapping)
    {
        if (options.Version == EpubVersion.Unknown) throw new InvalidEpubVersionException();
        ZipFileStorageOptions zipFileStorageOptions = new()
        {
            Mode = ZipArchiveMode.Create,
            FixedTimestamp = options.Modified,
            Compression = options.Compression,
            CompressionOverrides = [("mimetype", CompressionLevel.NoCompression)],
        };
        ZipFileStorage zipFileStorage = new(stream, zipFileStorageOptions);
        EpubWriter epubWriter = new(zipFileStorage.GetDirectory(), options, mediaTypeFileExtensionsMapping, zipFileStorage);
        epubWriter.WriteMimetype();
        epubWriter.WriteContainerXml();
        return epubWriter;
    }

    public static EpubWriter Create(IDirectory directory, EpubWriterOptions options, IMediaTypeFileExtensionsMapping mediaTypeFileExtensionsMapping)
    {
        if (options.Version == EpubVersion.Unknown) throw new InvalidEpubVersionException();
        EpubWriter epubWriter = new(directory, options, mediaTypeFileExtensionsMapping, null);
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

    public async Task<Stream> CreateResourceAsync(EpubResource resource, CancellationToken cancellationToken = default)
    {
        if (Path.GetFileNameWithoutExtension(resource.Href).StartsWith(_options.ReservedPrefix))
        {
            throw new InvalidOperationException($"File name must not start with {_options.ReservedPrefix}");
        }
        string resourcePath = GetResourcePath(resource.Href);
        if (!_resourcePaths.Add(resourcePath))
        {
            throw new InvalidOperationException("Resource already exists!");
        }
        _resources.Add(resource);
        IFile file = _rootDirectory.GetFile(resourcePath.Split('/'));
        return await file.OpenWriteAsync(cancellationToken).ConfigureAwait(false);
    }

    public Stream CreateResource(EpubResource resource)
    {
        if (Path.GetFileNameWithoutExtension(resource.Href).StartsWith(_options.ReservedPrefix))
        {
            throw new InvalidOperationException($"File name must not start with {_options.ReservedPrefix}");
        }
        string resourcePath = GetResourcePath(resource.Href);
        if (!_resourcePaths.Add(resourcePath))
        {
            throw new InvalidOperationException("Resource already exists!");
        }
        _resources.Add(resource);
        IFile file = _rootDirectory.GetFile(resourcePath.Split('/'));
        return file.OpenWrite();
    }

    public async Task<Stream> CreateRasterCoverAsync(string extension, bool inSequence, CancellationToken cancellationToken = default)
    {
        if (_coverHref is not null) throw new InvalidOperationException("Cover already added!");
        _coverHref = $"{_options.ReservedPrefix}cover{extension}";
        _coverInSequence = inSequence;
        IFile file = _rootDirectory.GetFile(GetResourcePath(_coverHref).Split('/'));
        return await file.OpenWriteAsync(cancellationToken).ConfigureAwait(false);
    }

    public Stream CreateRasterCover(string extension, bool inSequence)
    {
        if (_coverHref is not null) throw new InvalidOperationException("Cover already added!");
        _coverHref = $"{_options.ReservedPrefix}cover{extension}";
        _coverInSequence = inSequence;
        IFile file = _rootDirectory.GetFile(GetResourcePath(_coverHref).Split('/'));
        return file.OpenWrite();
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
        _zipFileStorage?.Dispose();
    }

    public async ValueTask DisposeAsync()
    {
        SaveChanges();
        await WriteSpecialDocumentsAsync(default).ConfigureAwait(false);
        // TODO Async Zip
        _zipFileStorage?.Dispose();
    }

    private string GetResourcePath(string href)
    {
        return string.Join('/', _options.ContentDirectory.Trim('/'), href.Trim('/')).Trim('/');
    }

    private Task WriteMimetypeAsync(CancellationToken cancellationToken)
    {
        return _rootDirectory.GetFile("mimetype")
            .WriteTextAsync("application/epub+zip", Encoding.ASCII, cancellationToken);
    }

    private void WriteMimetype()
    {
        _rootDirectory.GetFile("mimetype")
            .WriteText("application/epub+zip", Encoding.ASCII);
    }

    private async Task WriteContainerXmlAsync(CancellationToken cancellationToken)
    {
        XDocument document = _metaInfHandler.GetContainerXmlDocument(_packageDocumentPath);
        IFile file = _rootDirectory.GetFile("META-INF", "container.xml");
        await EpubXml.SaveAsync(document, file, cancellationToken).ConfigureAwait(false);
    }

    private void WriteContainerXml()
    {
        XDocument document = _metaInfHandler.GetContainerXmlDocument(_packageDocumentPath);
        IFile file = _rootDirectory.GetFile("META-INF", "container.xml");
        EpubXml.Save(document, file);
    }

    private async Task WriteSpecialDocumentsAsync(CancellationToken cancellationToken)
    {
        if (_coverHref is not null && _coverInSequence)
        {
            await EpubXml.SaveAsync(_coverXhtmlHandler.GetRasterDocument(_coverHref), GetCoverXhtmlFile(), cancellationToken).ConfigureAwait(false);
        }
        if (_toc is not null)
        {
            if (IncludeNavigationDocument)
            {
                await EpubXml.SaveAsync(_navigationDocumentHandler.GetDocument(), GetNavXhtmlFile(), cancellationToken).ConfigureAwait(false);
            }
            if (IncludeNcx)
            {
                await EpubXml.SaveAsync(_ncxHandler.GetDocument(), GetTocNcxFile(), cancellationToken).ConfigureAwait(false);
            }
        }
        await EpubXml.SaveAsync(_packageDocumentHandler.GetDocument(), GetPackageFile(), cancellationToken).ConfigureAwait(false);
    }

    private void WriteSpecialDocuments()
    {
        if (_coverHref is not null && _coverInSequence)
        {
            EpubXml.Save(_coverXhtmlHandler.GetRasterDocument(_coverHref), GetCoverXhtmlFile());
        }
        if (_toc is not null)
        {
            if (IncludeNavigationDocument)
            {
                EpubXml.Save(_navigationDocumentHandler.GetDocument(), GetNavXhtmlFile());
            }
            if (IncludeNcx)
            {
                EpubXml.Save(_ncxHandler.GetDocument(), GetTocNcxFile());
            }
        }
        EpubXml.Save(_packageDocumentHandler.GetDocument(), GetPackageFile());
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
        _packageDocumentHandler.AddModified(_options.Modified);
        if (Series is not null) _packageDocumentHandler.AddSeries(Series);
    }

    private void SaveCover()
    {
        if (_coverHref is null) return;
        _packageDocumentHandler.AddItemToManifest(_coverHref, "cover-image", "cover-id");
        if (_options.Version == EpubVersion.Epub2)
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
            _packageDocumentHandler.AddItemToManifestAndSpine(_coverXhtmlFileName, null, null, "cover-xhtml-id");
        }
    }

    private void SaveToc()
    {
        if (IncludeNavigationDocument)
        {
            if (_tocInSequence)
            {
                _packageDocumentHandler.AddItemToManifestAndSpine(_navXhtmlFileName, "nav", null, null);
            }
            else
            {
                _packageDocumentHandler.AddItemToManifest(_navXhtmlFileName, "nav", null);
            }
        }
        if (IncludeNcx)
        {
            _ncxHandler.AddIdentifier(Identifier);
            _ncxHandler.AddTitle(Title);
            _packageDocumentHandler.AddItemToManifest(_tocNcxFileName, null, "ncx-id");
            _packageDocumentHandler.AddNcx("ncx-id");
        }
        if (_coverInSequence)
        {
            EpubNavItem coverNavItem = new()
            {
                Text = "Cover",
                Reference = _coverXhtmlFileName,
            };
            SaveNavItem(coverNavItem);
        }
        if (_tocInSequence)
        {
            EpubNavItem tocNavItem = new()
            {
                Text = "Table Of Contents",
                Reference = _navXhtmlFileName,
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
                _navigationDocumentHandler.AddItemToLandmarks("cover", "Cover", _coverXhtmlFileName);
            }
            if (_tocInSequence)
            {
                _navigationDocumentHandler.AddItemToLandmarks("toc", "Table Of Contents", _navXhtmlFileName);
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
                _packageDocumentHandler.AddReferenceToGuide("cover", "Cover", _coverXhtmlFileName);
            }
            if (_tocInSequence)
            {
                _packageDocumentHandler.AddReferenceToGuide("toc", "Table Of Contents", _navXhtmlFileName);
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

    private IFile GetCoverXhtmlFile()
        => _rootDirectory.GetFile(GetResourcePath(_coverXhtmlFileName).Split('/'));

    private IFile GetNavXhtmlFile()
        => _rootDirectory.GetFile(GetResourcePath(_navXhtmlFileName).Split('/'));

    private IFile GetTocNcxFile()
        => _rootDirectory.GetFile(GetResourcePath(_tocNcxFileName).Split('/'));

    private IFile GetPackageFile()
        => _rootDirectory.GetFile(_packageDocumentPath.Split('/'));
}
