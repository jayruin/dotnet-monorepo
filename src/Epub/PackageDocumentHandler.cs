using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Xml.Linq;

namespace Epub;

internal sealed class PackageDocumentHandler
{
    private const string _dateTimeFormat = "yyyy-MM-ddTHH:mm:ssK";

    private readonly XDocument _document;

    private readonly XElement _packageElement;

    private readonly Lazy<XElement> _metadataElementLazy;

    private readonly Lazy<XElement> _manifestElementLazy;

    private readonly Lazy<XElement> _spineElementLazy;

    private readonly Lazy<XElement> _guideElementLazy;

    private int _creatorCount = 1;

    private int _resourceCount = 1;

    private string NextItemId
    {
        get
        {
            string itemId = $"item-id-{_resourceCount}";
            _resourceCount += 1;
            return itemId;
        }
    }

    public XElement MetadataElement => _metadataElementLazy.Value;

    public XElement ManifestElement => _manifestElementLazy.Value;

    public XElement SpineElement => _spineElementLazy.Value;

    public XElement GuideElement => _guideElementLazy.Value;

    public EpubVersion Version { get; }

    public PackageDocumentHandler(EpubVersion epubVersion)
    {
        Version = epubVersion;
        string version = Version switch
        {
            EpubVersion.Epub3 => "3.0",
            EpubVersion.Epub2 => "2.0",
            _ => throw new InvalidEpubVersionException(),
        };
        _packageElement = new XElement((XNamespace)EpubXmlNamespaces.Opf + "package",
            new XAttribute("xmlns", EpubXmlNamespaces.Opf),
            new XAttribute("unique-identifier", "publication-id"),
            new XAttribute("version", version)
        );
        _document =  new(
            new XDeclaration("1.0", "utf-8", null),
            _packageElement
        );
        _metadataElementLazy = new Lazy<XElement>(() =>
        {
            XElement metadataElement = new((XNamespace)EpubXmlNamespaces.Opf + "metadata",
                new XAttribute(XNamespace.Xmlns + "dc", EpubXmlNamespaces.Dc)
            );
            if (Version == EpubVersion.Epub2)
            {
                metadataElement.Add(
                    new XAttribute(XNamespace.Xmlns + "opf", EpubXmlNamespaces.Opf),
                    new XAttribute("xmlns", EpubXmlNamespaces.Opf)
                );
            }
            _packageElement.Add(metadataElement);
            return metadataElement;
        });
        _manifestElementLazy = new Lazy<XElement>(() =>
        {
            XElement manifestElement = new((XNamespace)EpubXmlNamespaces.Opf + "manifest");
            _packageElement.Add(manifestElement);
            return manifestElement;
        });
        _spineElementLazy = new Lazy<XElement>(() =>
        {
            XElement spineElement = new((XNamespace)EpubXmlNamespaces.Opf + "spine");
            _packageElement.Add(spineElement);
            return spineElement;
        });
        _guideElementLazy = new Lazy<XElement>(() =>
        {
            XElement guideElement = new((XNamespace)EpubXmlNamespaces.Opf + "guide");
            _packageElement.Add(guideElement);
            return guideElement;
        });
    }

    public XDocument GetDocument() => _document;

    public void AddIdentifier(string identifier)
    {
        MetadataElement.Add(
            new XElement((XNamespace)EpubXmlNamespaces.Dc + "identifier",
                new XAttribute("id", "publication-id"),
                identifier
            )
        );
    }

    public void AddTitle(string title)
    {
        if (Version == EpubVersion.Epub3)
        {
            MetadataElement.Add(
                new XElement((XNamespace)EpubXmlNamespaces.Dc + "title",
                    new XAttribute("id", "title-id"),
                    title
                ),
                new XElement((XNamespace)EpubXmlNamespaces.Opf + "meta",
                    new XAttribute("refines", "#title-id"),
                    new XAttribute("property", "title-type"),
                    "main"
                )
            );
        }
        else if (Version == EpubVersion.Epub2)
        {
            MetadataElement.Add(
                new XElement((XNamespace)EpubXmlNamespaces.Dc + "title",
                    title
                )
            );
        }
    }

    public void AddLanguage(string language)
    {
        MetadataElement.Add(
            new XElement((XNamespace)EpubXmlNamespaces.Dc + "language",
                language
            )
        );
    }

    public void AddCreator(string name, IEnumerable<string> roles)
    {
        if (Version == EpubVersion.Epub3)
        {
            MetadataElement.Add(
                new XElement((XNamespace)EpubXmlNamespaces.Dc + "creator",
                    new XAttribute("id", $"creator-id-{_creatorCount}"),
                    name
                )
            );
            foreach (string role in roles)
            {
                MetadataElement.Add(
                    new XElement((XNamespace)EpubXmlNamespaces.Opf + "meta",
                        new XAttribute("refines", $"#creator-id-{_creatorCount}"),
                        new XAttribute("property", "role"),
                        new XAttribute("scheme", "marc:relators"),
                        role
                    )
                );
            }
            _creatorCount += 1;
        }
        else if (Version == EpubVersion.Epub2)
        {
            bool hasRoles = false;
            foreach (string role in roles)
            {
                hasRoles = true;
                MetadataElement.Add(
                    new XElement((XNamespace)EpubXmlNamespaces.Dc + "creator",
                        new XAttribute((XNamespace)EpubXmlNamespaces.Opf + "role", role),
                        name
                    )
                );
            }
            if (!hasRoles)
            {
                MetadataElement.Add(
                    new XElement((XNamespace)EpubXmlNamespaces.Dc + "creator",
                        name
                    )
                );
            }
        }
    }

    public void AddDate(DateTimeOffset date)
    {
        MetadataElement.Add(
            new XElement((XNamespace)EpubXmlNamespaces.Dc + "date",
                date.UtcDateTime.ToString(_dateTimeFormat, CultureInfo.InvariantCulture)
            )
        );
    }

    public void AddPrePaginated()
    {
        if (Version == EpubVersion.Epub3)
        {
            MetadataElement.Add(
                new XElement((XNamespace)EpubXmlNamespaces.Opf + "meta",
                    new XAttribute("property", "rendition:layout"),
                    "pre-paginated"
                )
            );
        }
    }

    public void AddModified(DateTimeOffset modified)
    {
        if (Version == EpubVersion.Epub3)
        {
            MetadataElement.Add(
                new XElement((XNamespace)EpubXmlNamespaces.Opf + "meta",
                    new XAttribute("property", "dcterms:modified"),
                    modified.UtcDateTime.ToString(_dateTimeFormat, CultureInfo.InvariantCulture)
                )
            );
        }
    }

    public void AddItemToManifest(string href, string? manifestProperties, string? itemId)
    {
        itemId ??= NextItemId;
        XElement item = new((XNamespace)EpubXmlNamespaces.Opf + "item",
            new XAttribute("href", href),
            new XAttribute("id", itemId),
            new XAttribute("media-type", EpubMediaTypeProvider.GuessMediaType(href))
        );
        if (Version == EpubVersion.Epub3)
        {
            item.SetAttributeValue("properties", manifestProperties);
        }
        ManifestElement.Add(item);
    }

    public void AddItemToSpine(string? spineProperties, string itemId)
    {
        XElement itemref = new((XNamespace)EpubXmlNamespaces.Opf + "itemref",
            new XAttribute("idref", itemId)
        );
        if (Version == EpubVersion.Epub3)
        {
            itemref.SetAttributeValue("properties", spineProperties);
        }
        SpineElement.Add(itemref);
    }

    public void AddItemToManifestAndSpine(string href, string? manifestProperties, string? spineProperties, string? itemId)
    {
        itemId ??= NextItemId;
        AddItemToManifest(href, manifestProperties, itemId);
        AddItemToSpine(spineProperties, itemId);
    }

    public void AddNcx(string itemId)
    {
        SpineElement.Add(new XAttribute("toc", itemId));
    }

    public void AddRightToLeft()
    {
        if (Version == EpubVersion.Epub3)
        {
            SpineElement.Add(new XAttribute("page-progression-direction", "rtl"));
        }
    }

    public void AddLeftToRight()
    {
        if (Version == EpubVersion.Epub3)
        {
            SpineElement.Add(new XAttribute("page-progression-direction", "ltr"));
        }
    }

    public void AddReferenceToGuide(string type, string title, string href)
    {
        GuideElement.Add(
            new XElement((XNamespace)EpubXmlNamespaces.Opf + "reference",
                new XAttribute("type", type),
                new XAttribute("title", title),
                new XAttribute("href", href)
            )
        );
    }
}
