using ImgProj.Covers;
using Pdfs;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Utils;

namespace ImgProj.Exporting;

public sealed class PdfExporter : IExporter
{
    private readonly ICoverGenerator _coverGenerator;

    private readonly IPdfLoader _pdfLoader;

    public ExportFormat ExportFormat { get; } = ExportFormat.Pdf;

    public PdfExporter(ICoverGenerator coverGenerator, IPdfLoader pdfLoader)
    {
        _coverGenerator = coverGenerator;
        _pdfLoader = pdfLoader;
    }

    public async Task ExportAsync(IImgProject project, Stream stream, ImmutableArray<int> coordinates, string? version)
    {
        IImgProject subProject = project.GetSubProject(coordinates);
        version ??= subProject.MainVersion;
        IMetadataVersion metadata = subProject.MetadataVersions[version];
        List<IPage> pages = [];
        IPage? cover = _coverGenerator.CreateCoverGrid(subProject, version);
        if (cover is not null)
        {
            pages.Add(cover);
        }
        PdfOutlineItem pdfOutlineItem = Traverse(subProject, version, pages);
        string title = metadata.TitleParts.Count > 0
            ? string.Join(" - ", metadata.TitleParts)
            : $"No Title";
        string author = metadata.Creators.Count > 0
            ? string.Join(", ", metadata.Creators.Select(c => c.Key))
            : "No Author";
        using IPdfWritableDocument pdf = _pdfLoader.OpenWrite(stream);
        pdf.SetTitle(title);
        pdf.SetAuthor(author);
        foreach (IPage page in pages)
        {
            await using Stream pageStream = page.OpenRead();
            pdf.AddImagePage(await pageStream.ToByteArrayAsync());
        }
        pdf.AddOutlineItem(pdfOutlineItem);
    }

    private static PdfOutlineItem Traverse(IImgProject project, string version, ICollection<IPage> pages)
    {
        string title = project.MetadataVersions[version].TitleParts.Count > 0
            ? project.MetadataVersions[version].TitleParts[^1]
            : $"No Title";
        int pageNumber = pages.Count + 1;
        foreach (IPage page in project.EnumeratePages(version, false))
        {
            pages.Add(page);
        }
        ImmutableArray<PdfOutlineItem> children = project.ChildProjects
            .Select(childProject => Traverse(childProject, version, pages))
            .ToImmutableArray();
        PdfOutlineItem pdfOutlineItem = new()
        {
            Text = title,
            Page = pageNumber,
            Children = children,
        };
        return pdfOutlineItem;
    }
}
