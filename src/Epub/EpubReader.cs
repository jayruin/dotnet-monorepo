using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Xml.Linq;
using Utils;

namespace Epub;

public sealed class EpubReader : IDisposable
{
    private readonly ZipArchive _zipArchive;

    private readonly XNamespace _containerNamespace = EpubXmlNamespaces.Container;

    private readonly XNamespace _opfNamespace = EpubXmlNamespaces.Opf;

    private readonly XNamespace _dcNamespace = EpubXmlNamespaces.Dc;

    private readonly Lazy<XDocument> _lazyContainerDocument;

    private readonly Lazy<XDocument> _lazyOpfDocument;

    private readonly Lazy<EpubVersion> _lazyEpubVersion;

    private XDocument ContainerDocument => _lazyContainerDocument.Value;

    private XDocument OpfDocument => _lazyOpfDocument.Value;

    public EpubVersion Version => _lazyEpubVersion.Value;

    public EpubReader(Stream stream)
    {
        _zipArchive = new ZipArchive(stream, ZipArchiveMode.Read, true);
        _lazyContainerDocument = new Lazy<XDocument>(GetContainerDocument);
        _lazyOpfDocument = new Lazy<XDocument>(GetOpfDocument);
        _lazyEpubVersion = new Lazy<EpubVersion>(GetEpubVersion);
    }

    public void Dispose() => _zipArchive.Dispose();

    private XDocument GetContainerDocument()
    {
        ZipArchiveEntry containerXml = _zipArchive.GetEntry("META-INF/container.xml") ?? throw new ContainerXmlNotFoundException();
        using Stream containerXmlStream = containerXml.Open();
        return XDocument.Load(containerXmlStream);
    }

    private XDocument GetOpfDocument()
    {
        string opfPath = ContainerDocument
            .Element(_containerNamespace + "container")
            ?.Element(_containerNamespace + "rootfiles")
            ?.Element(_containerNamespace + "rootfile")
            ?.Attribute("full-path")
            ?.Value ?? throw new PackageDocumentNotFoundException();
        ZipArchiveEntry? opfFile = _zipArchive.GetEntry(opfPath) ?? throw new PackageDocumentNotFoundException();
        using Stream opfStream = opfFile.Open();
        return XDocument.Load(opfStream);
    }

    private EpubVersion GetEpubVersion()
    {
        string? version = OpfDocument
            .Element(_opfNamespace + "package")
            ?.Attribute("version")
            ?.Value;
        return version switch
        {
            "3.0" => EpubVersion.Epub3,
            "2.0" => EpubVersion.Epub2,
            _ => EpubVersion.Unknown,
        };
    }

    public IEnumerable<string> EnumerateResources() => _zipArchive.Entries.Select(e => e.FullName);

    public Stream? OpenResource(string resource) => _zipArchive.GetEntry(resource)?.Open();

    public DateTimeOffset GuessLastModified()
    {
        return GetModified().ToDateTimeOffsetNullable(null)
            ?? _zipArchive.Entries
                .Select(e => e.LastWriteTime)
                .Append(GetDate().ToDateTimeOffset(DateTimeOffset.MinValue))
                .Max();
    }

    public string? GetModified()
    {
        if (Version != EpubVersion.Epub3) return null;
        return OpfDocument
            .Element(_opfNamespace + "package")
            ?.Element(_opfNamespace + "metadata")
            ?.Elements(_opfNamespace + "meta")
            ?.FirstOrDefault(e => e.Attribute("property")?.Value == "dcterms:modified")
            ?.Value;
    }

    public string? GetDate()
    {
        return OpfDocument
            ?.Element(_opfNamespace + "package")
            ?.Element(_opfNamespace + "metadata")
            ?.Element(_dcNamespace + "date")
            ?.Value;
    }
}