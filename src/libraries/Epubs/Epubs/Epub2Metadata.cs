using MediaTypes;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Xml.Linq;
using Utils;

namespace Epubs;

internal sealed class Epub2Metadata : IEpubMetadata, IEpubOpfMetadata
{
    DateTimeOffset? IEpubMetadata.LastModified => Dates is null || Dates.Count == 0
        ? null
        : Dates
            .Select(e => e.Value.ToDateTimeOffsetNullable())
            .Max();
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
            .GroupBy(e => e.Value)
            .Select(g => new EpubCreator()
            {
                Name = g.Key,
                Roles = [.. g.Select(e => e.Role).OfType<string>()],
            });
        set
        {
            Creators = [.. value
                .SelectMany(c => c.Roles.Select(r => new Epub2CreatorMetadataEntry()
                {
                    Value = c.Name,
                    Role = r,
                })),
            ];
        }
    }
    DateTimeOffset? IEpubMetadata.Date
    {
        get => (Dates ?? []).Select(e => e.Value.ToDateTimeOffsetNullable()).FirstOrDefault(d => d.HasValue);
        set
        {
            Dates = value is DateTimeOffset date
                ? [
                    new()
                    {
                        Value = date.ToString("o", CultureInfo.InvariantCulture),
                    },
                ]
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
        get
        {
            if (Metas is null) return null;
            string? seriesName = null;
            string? seriesIndex = null;
            Metas.ForEach(e =>
            {
                if (e.Name == "calibre:series")
                {
                    seriesName ??= e.Content;
                }
                if (e.Name == "calibre:series_index")
                {
                    seriesIndex ??= e.Content;
                }
            });
            if (string.IsNullOrWhiteSpace(seriesName) || string.IsNullOrWhiteSpace(seriesIndex)) return null;
            return new EpubSeries()
            {
                Name = seriesName,
                Index = seriesIndex,
            };
        }
        set
        {
            Metas?.RemoveAll(e => e.Name == "calibre:series" || e.Name == "calibre:series_index");
            if (value is null) return;
            Metas ??= [];
            Metas.Add(new()
            {
                Value = "",
                Name = "calibre:series",
                Content = value.Name,
            });
            Metas.Add(new()
            {
                Value = "",
                Name = "calibre:series_index",
                Content = value.Index,
            });
        }
    }

    public required Epub2IdentifierMetadataEntry Identifier { get; set; }
    public required Epub2HumanReadableMetadataEntry Title { get; set; }
    public required Epub2MetadataEntry Language { get; set; }
    public List<Epub2IdentifierMetadataEntry>? Identifiers { get; set; }
    public List<Epub2HumanReadableMetadataEntry>? Titles { get; set; }
    public List<Epub2MetadataEntry>? Languages { get; set; }
    public List<Epub2CreatorMetadataEntry>? Contributors { get; set; }
    public List<Epub2HumanReadableMetadataEntry>? Coverages { get; set; }
    public List<Epub2CreatorMetadataEntry>? Creators { get; set; }
    public List<Epub2DateMetadataEntry>? Dates { get; set; }
    public List<Epub2HumanReadableMetadataEntry>? Descriptions { get; set; }
    public List<Epub2MetadataEntry>? Formats { get; set; }
    public List<Epub2HumanReadableMetadataEntry>? Publishers { get; set; }
    public List<Epub2HumanReadableMetadataEntry>? Relations { get; set; }
    public List<Epub2HumanReadableMetadataEntry>? Rights { get; set; }
    public List<Epub2HumanReadableMetadataEntry>? Sources { get; set; }
    public List<Epub2HumanReadableMetadataEntry>? Subjects { get; set; }
    public List<Epub2MetadataEntry>? Types { get; set; }
    public List<Epub2MetaEntry>? Metas { get; set; }

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
    private static readonly XName IdentifierSchemeName = (XNamespace)EpubXmlNamespaces.Opf + "scheme";
    private static readonly XName CreatorRoleName = (XNamespace)EpubXmlNamespaces.Opf + "role";
    private static readonly XName CreatorFileAsName = (XNamespace)EpubXmlNamespaces.Opf + "file-as";
    private static readonly XName DateEventName = (XNamespace)EpubXmlNamespaces.Opf + "event";

    public static Epub2Metadata ReadFromOpf(XDocument document)
    {
        XElement package = document.Element(PackageName)
            ?? throw new InvalidOperationException("Missing package element.");
        string uniqueIdentifier = package.Attribute("unique-identifier")?.Value.Trim()
            ?? throw new InvalidOperationException("Missing unique-identifier.");
        XElement packageMetadata = package.Element(MetadataName)
            ?? throw new InvalidOperationException("Missing metadata element.");

        XElement dcIdentifier = packageMetadata
            .Elements(IdentifierName)
            .FirstOrDefault(e => e.Attribute("id")?.Value.Trim() == uniqueIdentifier)
                ?? throw new InvalidOperationException("Missing dc:identifier.");

        Epub2IdentifierMetadataEntry identifier = CreateIdentifierMetadataEntry(dcIdentifier);
        List<Epub2IdentifierMetadataEntry> identifiers = packageMetadata
            .Elements(IdentifierName)
            .Where(e => e != dcIdentifier)
            .Select(CreateIdentifierMetadataEntry)
            .ToList();

        XElement dcTitle = packageMetadata
            .Elements(TitleName)
            .FirstOrDefault()
                ?? throw new InvalidOperationException("Missing dc:title.");
        Epub2HumanReadableMetadataEntry title = CreateHumanReadableMetadataEntry(dcTitle);
        List<Epub2HumanReadableMetadataEntry> titles = packageMetadata
            .Elements(TitleName)
            .Where(e => e != dcTitle)
            .Select(CreateHumanReadableMetadataEntry)
            .ToList();

        XElement dcLanguage = packageMetadata
            .Elements(LanguageName)
            .FirstOrDefault()
                ?? throw new InvalidOperationException("Missing dc:language.");
        Epub2MetadataEntry language = CreateMetadataEntry(dcLanguage);
        List<Epub2MetadataEntry> languages = packageMetadata
            .Elements(LanguageName)
            .Where(e => e != dcLanguage)
            .Select(CreateMetadataEntry)
            .ToList();

        List<Epub2CreatorMetadataEntry> contributors = packageMetadata
            .Elements(ContributorName)
            .Select(CreateCreatorMetadataEntry)
            .ToList();

        List<Epub2HumanReadableMetadataEntry> coverages = packageMetadata
            .Elements(CoverageName)
            .Select(CreateHumanReadableMetadataEntry)
            .ToList();

        List<Epub2CreatorMetadataEntry> creators = packageMetadata
            .Elements(CreatorName)
            .Select(CreateCreatorMetadataEntry)
            .ToList();

        List<Epub2DateMetadataEntry> dates = packageMetadata
            .Elements(DateName)
            .Select(CreateDateMetadataEntry)
            .ToList();

        List<Epub2HumanReadableMetadataEntry> descriptions = packageMetadata
            .Elements(DescriptionName)
            .Select(CreateHumanReadableMetadataEntry)
            .ToList();

        List<Epub2MetadataEntry> formats = packageMetadata
            .Elements(FormatName)
            .Select(CreateMetadataEntry)
            .ToList();

        List<Epub2HumanReadableMetadataEntry> publishers = packageMetadata
            .Elements(PublisherName)
            .Select(CreateHumanReadableMetadataEntry)
            .ToList();

        List<Epub2HumanReadableMetadataEntry> relations = packageMetadata
            .Elements(RelationName)
            .Select(CreateHumanReadableMetadataEntry)
            .ToList();

        List<Epub2HumanReadableMetadataEntry> rights = packageMetadata
            .Elements(RightsName)
            .Select(CreateHumanReadableMetadataEntry)
            .ToList();

        List<Epub2HumanReadableMetadataEntry> sources = packageMetadata
            .Elements(SourceName)
            .Select(CreateHumanReadableMetadataEntry)
            .ToList();

        List<Epub2HumanReadableMetadataEntry> subjects = packageMetadata
            .Elements(SubjectName)
            .Select(CreateHumanReadableMetadataEntry)
            .ToList();

        List<Epub2MetadataEntry> types = packageMetadata
            .Elements(TypeName)
            .Select(CreateMetadataEntry)
            .ToList();

        List<Epub2MetaEntry> metas = packageMetadata
            .Elements(MetaName)
            .Where(e =>
                !string.IsNullOrWhiteSpace(e.Attribute("name")?.Value.Trim())
                    && !string.IsNullOrWhiteSpace(e.Attribute("content")?.Value.Trim()))
            .Select(CreateMetaEntry)
            .ToList();

        return new()
        {
            Identifier = identifier,
            Title = title,
            Language = language,
            Identifiers = identifiers.Count == 0 ? null : identifiers,
            Titles = titles.Count == 0 ? null : titles,
            Languages = languages.Count == 0 ? null : languages,
            Contributors = contributors.Count == 0 ? null : contributors,
            Coverages = coverages.Count == 0 ? null : coverages,
            Creators = creators.Count == 0 ? null : creators,
            Dates = dates.Count == 0 ? null : dates,
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
                    && !string.IsNullOrWhiteSpace(e.Attribute("name")?.Value)
                    && !string.IsNullOrWhiteSpace(e.Attribute("content")?.Value))
            .Remove();
        AddEntries(IdentifierName, [Identifier, .. Identifiers ?? []], packageMetadata);
        AddEntries(TitleName, [Title, .. Titles ?? []], packageMetadata);
        AddEntries(LanguageName, [Language, .. Languages ?? []], packageMetadata);
        AddEntries(ContributorName, Contributors ?? [], packageMetadata);
        AddEntries(CoverageName, Coverages ?? [], packageMetadata);
        AddEntries(CreatorName, Creators ?? [], packageMetadata);
        AddEntries(DateName, Dates ?? [], packageMetadata);
        AddEntries(DescriptionName, Descriptions ?? [], packageMetadata);
        AddEntries(FormatName, Formats ?? [], packageMetadata);
        AddEntries(PublisherName, Publishers ?? [], packageMetadata);
        AddEntries(RelationName, Relations ?? [], packageMetadata);
        AddEntries(RightsName, Rights ?? [], packageMetadata);
        AddEntries(SourceName, Sources ?? [], packageMetadata);
        AddEntries(SubjectName, Subjects ?? [], packageMetadata);
        AddEntries(TypeName, Types ?? [], packageMetadata);
        AddEntries(MetaName, Metas ?? [], packageMetadata);
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
                new XAttribute("media-type", mediaTypeFileExtensionsMapping.GetMediaTypeFromPath(newCover, MediaType.Image.Jpeg))));
            packageMetadata.Add(new XElement(MetaName,
                new XAttribute("name", "cover"),
                new XAttribute("content", coverId)));
        }
    }

    private static Epub2MetadataEntry CreateMetadataEntry(XElement element)
    {
        return new()
        {
            Value = element.Value.Trim(),
        };
    }

    private static Epub2HumanReadableMetadataEntry CreateHumanReadableMetadataEntry(XElement element)
    {
        return new()
        {
            Value = element.Value.Trim(),
            XmlLang = element.Attribute(XmlLangName)?.Value.Trim(),
        };
    }

    private static Epub2IdentifierMetadataEntry CreateIdentifierMetadataEntry(XElement element)
    {
        return new()
        {
            Value = element.Value.Trim(),
            Scheme = element.Attribute(IdentifierSchemeName)?.Value.Trim(),
        };
    }

    private static Epub2CreatorMetadataEntry CreateCreatorMetadataEntry(XElement element)
    {
        return new()
        {
            Value = element.Value.Trim(),
            XmlLang = element.Attribute(XmlLangName)?.Value.Trim(),
            Role = element.Attribute(CreatorRoleName)?.Value.Trim(),
            FileAs = element.Attribute(CreatorFileAsName)?.Value.Trim(),
        };
    }

    private static Epub2DateMetadataEntry CreateDateMetadataEntry(XElement element)
    {
        return new()
        {
            Value = element.Value.Trim(),
            Event = element.Attribute(DateEventName)?.Value.Trim(),
        };
    }

    private static Epub2MetaEntry CreateMetaEntry(XElement element)
    {
        return new()
        {
            Value = element.Value.Trim(),
            Name = element.Attribute("name")?.Value.Trim() ?? throw new InvalidOperationException("Missing name on meta."),
            Content = element.Attribute("content")?.Value.Trim() ?? throw new InvalidOperationException("Missing content on meta."),
        };
    }

    private static void AddEntries(XName name, IReadOnlyCollection<Epub2MetadataEntry> entries, XElement packageMetadata)
    {
        int index = 0;
        int counter = 1;
        foreach (Epub2MetadataEntry entry in entries)
        {
            bool isUniqueIdentifier = name == IdentifierName && index == 0;
            string? currentId = null;
            if (isUniqueIdentifier)
            {
                do
                {
                    currentId = $"id{counter}";
                    counter += 1;
                } while (packageMetadata.Descendants().Any(e => e.Attribute("id")?.Value.Trim() == currentId));
                XElement package = packageMetadata.Parent
                    ?? throw new InvalidOperationException("package metadata has no parent.");
                package.SetAttributeValue("unique-identifier", currentId);
            }
            Epub2HumanReadableMetadataEntry? humanReadableEntry = entry as Epub2HumanReadableMetadataEntry;
            Epub2IdentifierMetadataEntry? identifierEntry = entry as Epub2IdentifierMetadataEntry;
            Epub2CreatorMetadataEntry? creatorEntry = entry as Epub2CreatorMetadataEntry;
            Epub2DateMetadataEntry? dateEntry = entry as Epub2DateMetadataEntry;
            Epub2MetaEntry? metaEntry = entry as Epub2MetaEntry;
            packageMetadata.Add(new XElement(name,
                entry.Value,
                string.IsNullOrWhiteSpace(currentId) ? null : new XAttribute("id", currentId),
                string.IsNullOrWhiteSpace(humanReadableEntry?.XmlLang) ? null : new XAttribute(XmlLangName, humanReadableEntry.XmlLang),
                string.IsNullOrWhiteSpace(identifierEntry?.Scheme) ? null : new XAttribute(IdentifierSchemeName, identifierEntry.Scheme),
                string.IsNullOrWhiteSpace(creatorEntry?.Role) ? null : new XAttribute(CreatorRoleName, creatorEntry.Role),
                string.IsNullOrWhiteSpace(creatorEntry?.FileAs) ? null : new XAttribute(CreatorFileAsName, creatorEntry.FileAs),
                string.IsNullOrWhiteSpace(dateEntry?.Event) ? null : new XAttribute(DateEventName, dateEntry.Event),
                string.IsNullOrWhiteSpace(metaEntry?.Name) ? null : new XAttribute("name", metaEntry.Name),
                string.IsNullOrWhiteSpace(metaEntry?.Content) ? null : new XAttribute("content", metaEntry.Content)));
            index += 1;
        }
    }
}
