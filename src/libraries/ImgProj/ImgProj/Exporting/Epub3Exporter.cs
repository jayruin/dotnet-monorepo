using Epubs;
using FileStorage;
using Images;
using ImgProj.Covers;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace ImgProj.Exporting;

public sealed class Epub3Exporter : IExporter
{
    private readonly ICoverGenerator _coverGenerator;

    private readonly IImageLoader _imageLoader;

    public ExportFormat ExportFormat { get; } = ExportFormat.Epub3;

    public Epub3Exporter(ICoverGenerator coverGenerator, IImageLoader imageLoader)
    {
        _coverGenerator = coverGenerator;
        _imageLoader = imageLoader;
    }

    public async Task ExportAsync(IImgProject project, Stream stream, ImmutableArray<int> coordinates, string? version)
    {
        IImgProject subProject = project.GetSubProject(coordinates);
        version ??= subProject.MainVersion;
        IMetadataVersion metadata = subProject.MetadataVersions[version];
        await using EpubWriter epubWriter = await EpubWriter.CreateAsync(stream, EpubVersion.Epub3);
        List<IPage> pages = new();
        IPage? cover = coordinates.Length == 0
            ? _coverGenerator.CreateCoverGrid(subProject, version)
            : subProject.EnumeratePages(version, true).FirstOrDefault();
        if (cover is not null)
        {
            await using Stream destinationCoverStream = epubWriter.CreateRasterCover(cover.Extension, true);
            await using Stream sourceCoverStream = cover.OpenRead();
            await sourceCoverStream.CopyToAsync(destinationCoverStream);
        }
        EpubNavItem navItem = await TraverseAsync(subProject, coordinates, version, pages, epubWriter);
        epubWriter.Title = metadata.TitleParts.Count > 0
            ? string.Join(" - ", metadata.TitleParts)
            : $"No Title";
        epubWriter.Languages = metadata.Languages;
        epubWriter.Creators = metadata.Creators.Select(c =>
        {
            return new EpubCreator
            {
                Name = c.Key,
                Roles = c.Value,
            };
        }).ToList();
        epubWriter.Date = metadata.Timestamp;
        epubWriter.PrePaginated = true;
        if (metadata.ReadingDirection == ReadingDirection.LTR)
        {
            epubWriter.Direction = EpubDirection.LeftToRight;
        }
        if (metadata.ReadingDirection == ReadingDirection.RTL)
        {
            epubWriter.Direction = EpubDirection.RightToLeft;
        }
        epubWriter.AddToc(new List<EpubNavItem>() { navItem, }, false);
    }

    private async Task<EpubNavItem> TraverseAsync(IImgProject project, ImmutableArray<int> coordinates, string version, ICollection<IPage> pages, EpubWriter epubWriter)
    {
        string title = project.MetadataVersions[version].TitleParts.Count > 0
            ? project.MetadataVersions[version].TitleParts[^1]
            : $"No Title";
        EpubNavItem navItem = new()
        {
            Text = title,
        };
        List<EpubNavItem> children = new();
        int pageNumber = 1;
        foreach (IPage page in project.EnumeratePages(version, false))
        {
            pages.Add(page);
            await SavePageAsync(epubWriter, coordinates, page, pageNumber);
            await SavePageXhtmlAsync(epubWriter, project, coordinates, page, pageNumber);
            pageNumber += 1;
        }
        for (int i = 0; i < project.ChildProjects.Count; i++)
        {
            EpubNavItem childNavItem = await TraverseAsync(project.ChildProjects[i], coordinates.Add(i + 1), version, pages, epubWriter);
            children.Add(childNavItem);
        }
        if (pageNumber > 1)
        {
            navItem.Reference = string.Join('/', coordinates.Select(c => c.ToString()).Append("1.xhtml"));
        }
        else if (children.Count > 0)
        {
            navItem.Reference = children[0].Reference;
        }
        else
        {
            throw new FileStorageException();
        }
        navItem.Children = children;
        return navItem;
    }

    private static async Task SavePageAsync(EpubWriter epubWriter, ImmutableArray<int> coordinates, IPage page, int pageNumber)
    {
        string imageHref = string.Join('/', coordinates.Select(c => c.ToString()).Append($"{pageNumber}{page.Extension}"));
        EpubResource imageResource = new()
        {
            Href = imageHref,
        };
        await using Stream pageStream = page.OpenRead();
        await epubWriter.AddResourceAsync(pageStream, imageResource);
    }

    private async Task SavePageXhtmlAsync(EpubWriter epubWriter, IImgProject project, ImmutableArray<int> coordinates, IPage page, int pageNumber)
    {
        string xhtmlHref = string.Join('/', coordinates.Select(c => c.ToString()).Append($"{pageNumber}.xhtml"));
        EpubResource xhtmlResource = new()
        {
            Href = xhtmlHref,
        };
        List<string> spineProperties = new();
        foreach (IPageSpread pageSpread in project.MetadataVersions[page.Version].PageSpreads)
        {
            if (pageSpread.Left.Length == 1 && pageSpread.Left[0] == pageNumber)
            {
                spineProperties.Add("page-spread-left");
            }
            if (pageSpread.Right.Length == 1 && pageSpread.Right[0] == pageNumber)
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
