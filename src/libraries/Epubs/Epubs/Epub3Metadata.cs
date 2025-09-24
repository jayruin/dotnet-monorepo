using MediaTypes;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Globalization;
using System.Linq;
using System.Xml.Linq;
using Utils;

namespace Epubs;

internal sealed class Epub3Metadata : IEpubMetadata, IEpubOpfMetadata
{
    DateTimeOffset? IEpubMetadata.LastModified => LastModified;
    string IEpubMetadata.Identifier
    {
        get => Identifier.Value;
        set
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(value);
            Identifier = new()
            {
                Value = value,
            };
            Identifiers = null;
        }
    }
    string IEpubMetadata.Title
    {
        get => Title.Value;
        set
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(value);
            Title = new()
            {
                Value = value,
            };
            Titles = null;
        }
    }
    IEnumerable<EpubCreator> IEpubMetadata.Creators
    {
        get => (Creators ?? [])
            .Select(e => new EpubCreator()
            {
                Name = e.Value,
                Roles = [.. (e.Metas ?? [])
                    .Where(m => m.Property == "role" && m.Scheme == "marc:relators")
                    .Select(m => m.Value),
                ],
            });
        set
        {
            Creators = [.. value
                .Select(c => new Epub3DirLangMetadataEntry()
                {
                    Value = c.Name,
                    Metas = [.. c.Roles
                        .Select(r => new Epub3MetaEntry()
                        {
                            Value = r,
                            Property = "role",
                            Scheme = "marc:relators",
                        }),
                    ],
                }),
            ];
        }
    }
    DateTimeOffset? IEpubMetadata.Date
    {
        get => Date?.Value?.ToDateTimeOffsetNullable();
        set
        {
            Date = value is DateTimeOffset date
                ? new()
                {
                    Value = date.ToString("o", CultureInfo.InvariantCulture),
                }
                : null;
        }
    }
    string? IEpubMetadata.Description
    {
        get => Descriptions is null ? null : string.Join('\n', Descriptions.Select(e => e.Value));
        set
        {
            if (value is null)
            {
                Descriptions = null;
                return;
            }
            Descriptions = [
                new()
                {
                    Value = value,
                },
            ];
        }
    }
    EpubSeries? IEpubMetadata.Series
    {
        get => Metas?.Select(e =>
        {
            if (e.Property == "belongs-to-collection")
            {
                string seriesName = e.Value;
                string? seriesIndex = e.Metas?.FirstOrDefault(e2 => e2.Property == "group-position")?.Value;
                if (string.IsNullOrWhiteSpace(seriesName) || string.IsNullOrWhiteSpace(seriesIndex)) return null;
                return new EpubSeries()
                {
                    Name = seriesName,
                    Index = seriesIndex,
                };
            }
            return null;
        })?.FirstOrDefault(s => s is not null);
        set
        {
            Metas?.RemoveAll(e => e.Property == "belongs-to-collection");
            if (value is null) return;
            Metas ??= [];
            Metas.Add(new()
            {
                Value = value.Name,
                Property = "belongs-to-collection",
                Metas = [
                    new()
                    {
                        Value = value.Index,
                        Property = "group-position",
                    },
                    new()
                    {
                        Value = "series",
                        Property = "collection-type",
                    },
                ],
            });
        }
    }

    public required DateTimeOffset LastModified { get; set; }
    public required Epub3MetadataEntry Identifier { get; set; }
    public required Epub3DirLangMetadataEntry Title { get; set; }
    public required Epub3MetadataEntry Language { get; set; }
    public List<Epub3MetadataEntry>? Identifiers { get; set; }
    public List<Epub3DirLangMetadataEntry>? Titles { get; set; }
    public List<Epub3MetadataEntry>? Languages { get; set; }
    public List<Epub3DirLangMetadataEntry>? Contributors { get; set; }
    public List<Epub3DirLangMetadataEntry>? Coverages { get; set; }
    public List<Epub3DirLangMetadataEntry>? Creators { get; set; }
    public Epub3MetadataEntry? Date { get; set; }
    public List<Epub3DirLangMetadataEntry>? Descriptions { get; set; }
    public List<Epub3MetadataEntry>? Formats { get; set; }
    public List<Epub3DirLangMetadataEntry>? Publishers { get; set; }
    public List<Epub3DirLangMetadataEntry>? Relations { get; set; }
    public List<Epub3DirLangMetadataEntry>? Rights { get; set; }
    public List<Epub3MetadataEntry>? Sources { get; set; }
    public List<Epub3DirLangMetadataEntry>? Subjects { get; set; }
    public List<Epub3MetadataEntry>? Types { get; set; }
    public List<Epub3MetaEntry>? Metas { get; set; }

    private static readonly XName PackageName = (XNamespace)EpubXmlNamespaces.Opf + "package";
    private static readonly XName MetadataName = (XNamespace)EpubXmlNamespaces.Opf + "metadata";
    private static readonly XName ManifestName = (XNamespace)EpubXmlNamespaces.Opf + "manifest";
    private static readonly XName ManifestItemName = (XNamespace)EpubXmlNamespaces.Opf + "item";
    private static readonly XName IdentifierName = (XNamespace)EpubXmlNamespaces.Dc + "identifier";
    private static readonly XName TitleName = (XNamespace)EpubXmlNamespaces.Dc + "title";
    private static readonly XName LanguageName = (XNamespace)EpubXmlNamespaces.Dc + "language";
    private static readonly XName ContributorName = (XNamespace)EpubXmlNamespaces.Dc + "contributor";
    private static readonly XName CoverageName = (XNamespace)EpubXmlNamespaces.Dc + "coverage";
    private static readonly XName CreatorName = (XNamespace)EpubXmlNamespaces.Dc + "creator";
    private static readonly XName DateName = (XNamespace)EpubXmlNamespaces.Dc + "date";
    private static readonly XName DescriptionName = (XNamespace)EpubXmlNamespaces.Dc + "description";
    private static readonly XName FormatName = (XNamespace)EpubXmlNamespaces.Dc + "format";
    private static readonly XName PublisherName = (XNamespace)EpubXmlNamespaces.Dc + "publisher";
    private static readonly XName RelationName = (XNamespace)EpubXmlNamespaces.Dc + "relation";
    private static readonly XName RightsName = (XNamespace)EpubXmlNamespaces.Dc + "rights";
    private static readonly XName SourceName = (XNamespace)EpubXmlNamespaces.Dc + "source";
    private static readonly XName SubjectName = (XNamespace)EpubXmlNamespaces.Dc + "subject";
    private static readonly XName TypeName = (XNamespace)EpubXmlNamespaces.Dc + "type";
    private static readonly XName MetaName = (XNamespace)EpubXmlNamespaces.Opf + "meta";
    private static readonly XName XmlLangName = XNamespace.Xml + "lang";

    public static Epub3Metadata ReadFromOpf(XDocument document)
    {
        XElement package = document.Element(PackageName)
            ?? throw new InvalidOperationException("Missing package element.");
        string uniqueIdentifier = package.Attribute("unique-identifier")?.Value.Trim()
            ?? throw new InvalidOperationException("Missing unique-identifier.");
        XElement packageMetadata = package.Element(MetadataName)
            ?? throw new InvalidOperationException("Missing metadata element.");

        XElement dctermsModified = packageMetadata.Elements(MetaName)
            .FirstOrDefault(e => e.Attribute("property")?.Value.Trim() == "dcterms:modified")
                ?? throw new InvalidOperationException("Missing dcterms:modified.");
        DateTimeOffset lastModified = DateTimeOffset.ParseExact(dctermsModified.Value.Trim(), DateTimeFormatting.Iso8601, CultureInfo.InvariantCulture);

        XElement dcIdentifier = packageMetadata
            .Elements(IdentifierName)
            .FirstOrDefault(e => e.Attribute("id")?.Value.Trim() == uniqueIdentifier)
                ?? throw new InvalidOperationException("Missing dc:identifier.");
        Epub3MetadataEntry identifier = CreateMetadataEntry(dcIdentifier, packageMetadata);
        List<Epub3MetadataEntry> identifiers = packageMetadata
            .Elements(IdentifierName)
            .Where(e => e != dcIdentifier)
            .Select(e => CreateMetadataEntry(e, packageMetadata))
            .ToList();

        XElement dcTitle = packageMetadata
            .Elements(TitleName)
            .FirstOrDefault()
                ?? throw new InvalidOperationException("Missing dc:title.");
        Epub3DirLangMetadataEntry title = CreateDirLangMetadataEntry(dcTitle, packageMetadata);
        List<Epub3DirLangMetadataEntry> titles = packageMetadata
            .Elements(TitleName)
            .Where(e => e != dcTitle)
            .Select(e => CreateDirLangMetadataEntry(e, packageMetadata))
            .ToList();

        XElement dcLanguage = packageMetadata
            .Elements(LanguageName)
            .FirstOrDefault()
                ?? throw new InvalidOperationException("Missing dc:language.");
        Epub3MetadataEntry language = CreateMetadataEntry(dcLanguage, packageMetadata);
        List<Epub3MetadataEntry> languages = packageMetadata
            .Elements(LanguageName)
            .Where(e => e != dcLanguage)
            .Select(e => CreateMetadataEntry(e, packageMetadata))
            .ToList();

        List<Epub3DirLangMetadataEntry> contributors = packageMetadata
            .Elements(ContributorName)
            .Select(e => CreateDirLangMetadataEntry(e, packageMetadata))
            .ToList();

        List<Epub3DirLangMetadataEntry> coverages = packageMetadata
            .Elements(CoverageName)
            .Select(e => CreateDirLangMetadataEntry(e, packageMetadata))
            .ToList();

        List<Epub3DirLangMetadataEntry> creators = packageMetadata
            .Elements(CreatorName)
            .Select(e => CreateDirLangMetadataEntry(e, packageMetadata))
            .ToList();

        XElement? dcDate = packageMetadata
            .Element(DateName);
        Epub3MetadataEntry? date = dcDate is null ? null : CreateMetadataEntry(dcDate, packageMetadata);

        List<Epub3DirLangMetadataEntry> descriptions = packageMetadata
            .Elements(DescriptionName)
            .Select(e => CreateDirLangMetadataEntry(e, packageMetadata))
            .ToList();

        List<Epub3MetadataEntry> formats = packageMetadata
            .Elements(FormatName)
            .Select(e => CreateMetadataEntry(e, packageMetadata))
            .ToList();

        List<Epub3DirLangMetadataEntry> publishers = packageMetadata
            .Elements(PublisherName)
            .Select(e => CreateDirLangMetadataEntry(e, packageMetadata))
            .ToList();

        List<Epub3DirLangMetadataEntry> relations = packageMetadata
            .Elements(RelationName)
            .Select(e => CreateDirLangMetadataEntry(e, packageMetadata))
            .ToList();

        List<Epub3DirLangMetadataEntry> rights = packageMetadata
            .Elements(RightsName)
            .Select(e => CreateDirLangMetadataEntry(e, packageMetadata))
            .ToList();

        List<Epub3MetadataEntry> sources = packageMetadata
            .Elements(SourceName)
            .Select(e => CreateMetadataEntry(e, packageMetadata))
            .ToList();

        List<Epub3DirLangMetadataEntry> subjects = packageMetadata
            .Elements(SubjectName)
            .Select(e => CreateDirLangMetadataEntry(e, packageMetadata))
            .ToList();

        List<Epub3MetadataEntry> types = packageMetadata
            .Elements(TypeName)
            .Select(e => CreateMetadataEntry(e, packageMetadata))
            .ToList();

        List<Epub3MetaEntry> metas = packageMetadata
            .Elements(MetaName)
            .Where(e =>
                !string.IsNullOrWhiteSpace(e.Attribute("property")?.Value.Trim())
                    && string.IsNullOrWhiteSpace(e.Attribute("refines")?.Value.Trim())
                    && e != dctermsModified)
            .Select(e => CreateMetaEntry(e, packageMetadata))
            .ToList();

        return new()
        {
            LastModified = lastModified,
            Identifier = identifier,
            Title = title,
            Language = language,
            Identifiers = identifiers.Count == 0 ? null : identifiers,
            Titles = titles.Count == 0 ? null : titles,
            Languages = languages.Count == 0 ? null : languages,
            Contributors = contributors.Count == 0 ? null : contributors,
            Coverages = coverages.Count == 0 ? null : coverages,
            Creators = creators.Count == 0 ? null : creators,
            Date = date,
            Descriptions = descriptions.Count == 0 ? null : descriptions,
            Formats = formats.Count == 0 ? null : formats,
            Publishers = publishers.Count == 0 ? null : publishers,
            Relations = relations.Count == 0 ? null : relations,
            Rights = rights.Count == 0 ? null : rights,
            Sources = sources.Count == 0 ? null : sources,
            Subjects = subjects.Count == 0 ? null : subjects,
            Types = types.Count == 0 ? null : types,
            Metas = metas.Count == 0 ? null : metas,
        };
    }

    public void WriteToOpf(XDocument document, string? newCover, IMediaTypeFileExtensionsMapping mediaTypeFileExtensionsMapping)
    {
        XElement package = document.Element(PackageName)
            ?? throw new InvalidOperationException("Missing package element.");
        XElement packageMetadata = package.Element(MetadataName)
            ?? throw new InvalidOperationException("Missing metadata element.");
        packageMetadata
            .Elements()
            .Where(e => e.Name.NamespaceName == EpubXmlNamespaces.Dc
                || e.Name == MetaName
                    && !string.IsNullOrWhiteSpace(e.Attribute("property")?.Value))
            .Remove();
        packageMetadata.Add(new XElement(MetaName,
            new XAttribute("property", "dcterms:modified"),
            LastModified.UtcDateTime.ToString(DateTimeFormatting.Iso8601)));
        AddEntries(IdentifierName, [Identifier, .. Identifiers ?? []], null, packageMetadata);
        AddEntries(TitleName, [Title, .. Titles ?? []], null, packageMetadata);
        AddEntries(LanguageName, [Language, .. Languages ?? []], null, packageMetadata);
        AddEntries(ContributorName, Contributors ?? [], null, packageMetadata);
        AddEntries(CoverageName, Coverages ?? [], null, packageMetadata);
        AddEntries(CreatorName, Creators ?? [], null, packageMetadata);
        AddEntries(DateName, Date is null ? [] : [Date], null, packageMetadata);
        AddEntries(DescriptionName, Descriptions ?? [], null, packageMetadata);
        AddEntries(FormatName, Formats ?? [], null, packageMetadata);
        AddEntries(PublisherName, Publishers ?? [], null, packageMetadata);
        AddEntries(RelationName, Relations ?? [], null, packageMetadata);
        AddEntries(RightsName, Rights ?? [], null, packageMetadata);
        AddEntries(SourceName, Sources ?? [], null, packageMetadata);
        AddEntries(SubjectName, Subjects ?? [], null, packageMetadata);
        AddEntries(TypeName, Types ?? [], null, packageMetadata);
        AddEntries(MetaName, Metas ?? [], null, packageMetadata);
        if (!string.IsNullOrWhiteSpace(newCover))
        {
            XElement packageManifest = package.Element(ManifestName)
                ?? throw new InvalidOperationException("Missing manifest element.");
            string coverId = "cover";
            int coverCounter = 1;
            while (packageManifest.Elements(ManifestItemName).FirstOrDefault(e => e.Attribute("id")?.Value == coverId) is not null)
            {
                coverId = $"cover{coverCounter}";
            }
            packageManifest.Add(new XElement(ManifestItemName,
                new XAttribute("id", coverId),
                new XAttribute("href", newCover),
                new XAttribute("media-type", mediaTypeFileExtensionsMapping.GetMediaTypeFromPath(newCover, MediaType.Image.Jpeg)),
                new XAttribute("properties", "cover-image")));
        }
    }

    private static Epub3MetadataEntry CreateMetadataEntry(XElement element, XElement packageMetadata)
    {
        return new()
        {
            Value = element.Value.Trim(),
            Metas = CreateMetaEntries(element.Attribute("id")?.Value.Trim(), packageMetadata),
        };
    }

    private static Epub3DirLangMetadataEntry CreateDirLangMetadataEntry(XElement element, XElement packageMetadata)
    {
        return new()
        {
            Value = element.Value.Trim(),
            Dir = element.Attribute("dir")?.Value.Trim(),
            XmlLang = element.Attribute(XmlLangName)?.Value.Trim(),
            Metas = CreateMetaEntries(element.Attribute("id")?.Value.Trim(), packageMetadata),
        };
    }

    private static Epub3MetaEntry CreateMetaEntry(XElement element, XElement packageMetadata)
    {
        return new()
        {
            Value = element.Value.Trim(),
            Property = element.Attribute("property")?.Value.Trim() ?? throw new InvalidOperationException("Missing property on meta."),
            Dir = element.Attribute("dir")?.Value.Trim(),
            XmlLang = element.Attribute(XmlLangName)?.Value.Trim(),
            Scheme = element.Attribute("scheme")?.Value.Trim(),
            Metas = CreateMetaEntries(element.Attribute("id")?.Value.Trim(), packageMetadata),
        };
    }

    private static List<Epub3MetaEntry>? CreateMetaEntries(string? id, XElement packageMetadata)
    {
        if (string.IsNullOrWhiteSpace(id)) return null;
        List<Epub3MetaEntry> elements = packageMetadata
            .Elements(MetaName)
            .Where(e => e.Attribute("refines")?.Value.Trim() == $"#{id}")
            .Select(e => CreateMetaEntry(e, packageMetadata))
            .ToList();
        if (elements.Count == 0) return null;
        return elements;
    }

    private static void AddEntries(XName name, IReadOnlyCollection<Epub3MetadataEntry> entries, string? parentId, XElement packageMetadata)
    {
        int index = 0;
        int counter = 1;
        foreach (Epub3MetadataEntry entry in entries)
        {
            bool isUniqueIdentifier = name == IdentifierName && index == 0;
            string? currentId = null;
            if ((entry.Metas ?? []).Count > 0 || isUniqueIdentifier)
            {
                do
                {
                    List<string> idParts = [];
                    if (!string.IsNullOrWhiteSpace(parentId))
                    {
                        idParts.Add(parentId);
                    }
                    idParts.Add($"{name.LocalName}{counter}");
                    currentId = string.Join('-', idParts);
                    counter += 1;
                } while (packageMetadata.Descendants().Any(e => e.Attribute("id")?.Value.Trim() == currentId));
            }
            if (isUniqueIdentifier)
            {
                XElement package = packageMetadata.Parent
                    ?? throw new InvalidOperationException("package metadata has no parent.");
                package.SetAttributeValue("unique-identifier", currentId);
            }
            Epub3DirLangMetadataEntry? dirLangEntry = entry as Epub3DirLangMetadataEntry;
            Epub3MetaEntry? metaEntry = entry as Epub3MetaEntry;
            packageMetadata.Add(new XElement(name,
                entry.Value,
                string.IsNullOrWhiteSpace(currentId) ? null : new XAttribute("id", currentId),
                string.IsNullOrWhiteSpace(dirLangEntry?.Dir) ? null : new XAttribute("dir", dirLangEntry.Dir),
                string.IsNullOrWhiteSpace(dirLangEntry?.XmlLang) ? null : new XAttribute(XmlLangName, dirLangEntry.XmlLang),
                string.IsNullOrWhiteSpace(metaEntry?.Property) ? null : new XAttribute("property", metaEntry.Property),
                string.IsNullOrWhiteSpace(metaEntry?.Scheme) ? null : new XAttribute("scheme", metaEntry.Scheme),
                string.IsNullOrWhiteSpace(parentId) || metaEntry is null ? null : new XAttribute("refines", $"#{parentId}")));
            AddEntries(MetaName, entry.Metas ?? [], currentId, packageMetadata);
            index += 1;
        }
    }
}
