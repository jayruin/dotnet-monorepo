using ImgProj.Covers;
using iText.IO.Image;
using iText.Kernel.Geom;
using iText.Kernel.Pdf;
using iText.Kernel.Pdf.Canvas;
using iText.Kernel.Pdf.Navigation;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace ImgProj.Exporting;

public sealed class PdfExporter : IExporter
{
    private class PdfOutlineItem
    {
        public required string Text { get; init; }

        public required int PageNumber { get; init; }

        public IList<PdfOutlineItem> Children { get; } = new List<PdfOutlineItem>();
    }

    private readonly ICoverGenerator _coverGenerator;

    public ExportFormat ExportFormat { get; } = ExportFormat.Pdf;

    public PdfExporter(ICoverGenerator coverGenerator)
    {
        _coverGenerator = coverGenerator;
    }

    public async Task ExportAsync(IImgProject project, Stream stream, ImmutableArray<int> coordinates, string? version)
    {
        IImgProject subProject = project.GetSubProject(coordinates);
        version ??= subProject.MainVersion;
        IMetadataVersion metadata = subProject.MetadataVersions[version];
        List<IPage> pages = new();
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
        WriterProperties writerProperties = new WriterProperties().AddXmpMetadata();
        PdfWriter pdfWriter = new(stream, writerProperties);
        PdfDocument pdfDocument = new(pdfWriter);
        PdfDocumentInfo pdfDocumentInfo = pdfDocument.GetDocumentInfo();
        pdfDocumentInfo.SetTitle(title);
        pdfDocumentInfo.SetAuthor(author);
        foreach (IPage page in pages)
        {
            await using Stream pageStream = page.OpenRead();
            await using MemoryStream memoryStream = new();
            await pageStream.CopyToAsync(memoryStream);
            ImageData imageData = ImageDataFactory.Create(memoryStream.ToArray());
            PageSize pageSize = new(imageData.GetWidth(), imageData.GetHeight());
            PdfPage pdfPage = pdfDocument.AddNewPage(pageSize);
            PdfCanvas pdfCanvas = new(pdfPage);
            pdfCanvas.AddImageAt(imageData, 0, 0, false);
        }
        PdfOutline pdfOutline = pdfDocument.GetOutlines(true);
        AddPdfOutlineItem(pdfOutlineItem, pdfDocument, pdfOutline);
        pdfDocument.Close();
    }

    private static PdfOutlineItem Traverse(IImgProject project, string version, ICollection<IPage> pages)
    {
        string title = project.MetadataVersions[version].TitleParts.Count > 0
            ? project.MetadataVersions[version].TitleParts[^1]
            : $"No Title";
        PdfOutlineItem pdfOutlineItem = new()
        {
            Text = title,
            PageNumber = pages.Count + 1,
        };
        foreach (IPage page in project.EnumeratePages(version, false))
        {
            pages.Add(page);
        }
        foreach (IImgProject childProject in project.ChildProjects)
        {
            PdfOutlineItem childItem = Traverse(childProject, version, pages);
            pdfOutlineItem.Children.Add(childItem);
        }
        return pdfOutlineItem;
    }

    private static void AddPdfOutlineItem(PdfOutlineItem pdfOutlineItem, PdfDocument pdfDocument, PdfOutline pdfOutline)
    {
        PdfOutline newPdfOutline = pdfOutline.AddOutline(pdfOutlineItem.Text);
        PdfPage pdfPage = pdfDocument.GetPage(pdfOutlineItem.PageNumber);
        PdfDestination pdfDestination = PdfExplicitDestination.CreateFit(pdfPage);
        newPdfOutline.AddDestination(pdfDestination);
        foreach (PdfOutlineItem child in pdfOutlineItem.Children)
        {
            AddPdfOutlineItem(child, pdfDocument, newPdfOutline);
        }
    }
}
