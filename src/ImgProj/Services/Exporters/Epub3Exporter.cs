using Images;
using ImgProj.Models;
using ImgProj.Services.Covers;
using Epub;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace ImgProj.Services.Exporters;

public sealed class Epub3Exporter : IExporter
{
    private readonly IImageLoader _imageLoader;

    private readonly ICoverGenerator _coverGenerator;

    public ExportFormat ExportFormat => ExportFormat.Epub3;

    public Epub3Exporter(IImageLoader imageLoader, ICoverGenerator coverGenerator)
    {
        _imageLoader = imageLoader;
        _coverGenerator = coverGenerator;
    }

    public async Task ExportAsync(ImgProject project, Stream stream, ImmutableArray<int> coordinates, string? version)
    {
        version ??= project.MainVersion;
        Entry entry = project.GetEntry(coordinates);
        await using EpubWriter epubWriter = await EpubWriter.CreateAsync(stream, EpubVersion.Epub3);
        List<Page> pages = new();
        EpubNavItem navItem = await TraverseAsync(project, entry, coordinates, version, pages, epubWriter);
        epubWriter.Title = project.ChooseRequiredValue(entry.Title, version);
        epubWriter.Languages = project.ChooseRequiredValue(project.Metadata.Languages, version);
        epubWriter.Creators = project.Metadata.Creators.Select(c =>
        {
            return new EpubCreator()
            {
                Name = project.ChooseRequiredValue(c.Name, version),
                Roles = c.Roles,
            };
        }).ToList();
        epubWriter.Date = project.GetLatestTimestamp(coordinates, version);
        epubWriter.PrePaginated = true;
        if (project.Metadata.Direction == Direction.LTR)
        {
            epubWriter.Direction = EpubDirection.LeftToRight;
        }
        if (project.Metadata.Direction == Direction.RTL)
        {
            epubWriter.Direction = EpubDirection.RightToLeft;
        }
        epubWriter.AddToc(new List<EpubNavItem>() { navItem, }, false);
        Page? cover = coordinates.Length == 0
            ? _coverGenerator.CreateCoverGrid(project, version)
            : pages.FirstOrDefault();
        if (cover is not null)
        {
            await using Stream destinationCoverStream = epubWriter.CreateRasterCover(cover.Extension, false);
            await using Stream sourceCoverStream = cover.OpenRead();
            await sourceCoverStream.CopyToAsync(destinationCoverStream);
        }
    }

    private async Task<EpubNavItem> TraverseAsync(ImgProject project, Entry entry, ImmutableArray<int> coordinates, string version, ICollection<Page> pages, EpubWriter epubWriter)
    {
        EpubNavItem navItem = new()
        {
            Text = project.ChooseRequiredValue(entry.Title, version),
        };
        List<EpubNavItem> children = new();
        IReadOnlyList<Page> entryPages = project.GetPages(coordinates, version).ToList();
        for (int i = 0; i < entryPages.Count; i++)
        {
            Page page = entryPages[i];
            pages.Add(page);
            await SavePageAsync(epubWriter, coordinates, page, i + 1);
            await SavePageXhtmlAsync(epubWriter, project, coordinates, page, i + 1);
        }
        for (int i = 0; i < entry.Entries.Length; i++)
        {
            EpubNavItem childNavItem = await TraverseAsync(project, entry.Entries[i], coordinates.Add(i + 1), version, pages, epubWriter);
            children.Add(childNavItem);
        }
        if (entryPages.Count > 0)
        {
            navItem.Reference = string.Join('/', coordinates.Select(c => c.ToString()).Append("1.xhtml"));
        }
        else if (children.Count > 0)
        {
            navItem.Reference = children[0].Reference;
        }
        else
        {
            throw new FileNotFoundException();
        }
        navItem.Children = children;
        return navItem;
    }

    private static async Task SavePageAsync(EpubWriter epubWriter, ImmutableArray<int> coordinates, Page page, int pageNumber)
    {
        string imageHref = string.Join('/', coordinates.Select(c => c.ToString()).Append($"{pageNumber}{page.Extension}"));
        EpubResource imageResource = new()
        {
            Href = imageHref,
        };
        await using Stream pageStream = page.OpenRead();
        await epubWriter.AddResourceAsync(pageStream, imageResource);
    }

    private async Task SavePageXhtmlAsync(EpubWriter epubWriter, ImgProject project, ImmutableArray<int> coordinates, Page page, int pageNumber)
    {
        string xhtmlHref = string.Join('/', coordinates.Select(c => c.ToString()).Append($"{pageNumber}.xhtml"));
        EpubResource xhtmlResource = new()
        {
            Href = xhtmlHref,
        };
        List<string> spineProperties = new();
        foreach (Spread spread in project.Spreads)
        {
            if (spread.Left.SequenceEqual(coordinates))
            {
                spineProperties.Add("page-spread-left");
            }
            if (spread.Right.SequenceEqual(coordinates))
            {
                spineProperties.Add("page-spread-right");
            }
        }
        if (spineProperties.Count > 0)
        {
            xhtmlResource.SpineProperties = spineProperties;
        }
        await using Stream xhtmlStream = epubWriter.CreateResource(xhtmlResource);
        await using Stream pageStream = page.OpenRead();
        XDocument pageXhtml = CreatePageXhtml(pageStream, $"{pageNumber}{page.Extension}");
        await EpubXml.SaveAsync(pageXhtml, xhtmlStream);
    }

    private XDocument CreatePageXhtml(Stream pageStream, string src)
    {
        using IImage image = _imageLoader.LoadImage(pageStream);
        return new XDocument(
            new XDeclaration("1.0", "utf-8", null),
            new XDocumentType("html", null, null, null),
            new XElement((XNamespace)EpubXmlNamespaces.Xhtml + "html",
                new XAttribute("xmlns", EpubXmlNamespaces.Xhtml),
                new XAttribute(XNamespace.Xmlns + "epub", EpubXmlNamespaces.Ops),
                new XElement((XNamespace)EpubXmlNamespaces.Xhtml + "head",
                    new XElement((XNamespace)EpubXmlNamespaces.Xhtml + "title",
                        "Paginated Image"
                    ),
                    new XElement((XNamespace)EpubXmlNamespaces.Xhtml + "meta",
                        new XAttribute("charset", "utf-8")
                    ),
                    new XElement((XNamespace)EpubXmlNamespaces.Xhtml + "meta",
                        new XAttribute("name", "viewport"),
                        new XAttribute("content", $"width={image.Width}, height={image.Height}")
                    )
                ),
                new XElement((XNamespace)EpubXmlNamespaces.Xhtml + "body",
                    new XElement((XNamespace)EpubXmlNamespaces.Xhtml + "img",
                        new XAttribute("src", src)
                    )
                )
            )
        );
    }
}
