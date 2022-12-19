using ImgProj.Models;
using ImgProj.Services.Covers;
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

namespace ImgProj.Services.Exporters;

public sealed class PdfExporter : IExporter
{
    private class PdfOutlineItem
    {
        public required string Text { get; init; }

        public required int PageNumber { get; init; }

        public IList<PdfOutlineItem> Children { get; } = new List<PdfOutlineItem>();
    }

    private readonly ICoverGenerator _coverGenerator;

    public ExportFormat ExportFormat => ExportFormat.Pdf;

    public PdfExporter(ICoverGenerator coverGenerator)
    {
        _coverGenerator = coverGenerator;
    }

    public async Task ExportAsync(ImgProject project, Stream stream, ImmutableArray<int> coordinates, string? version)
    {
        version ??= project.MainVersion;
        Entry entry = project.GetEntry(coordinates);
        List<Page> pages = new();
        if (coordinates.Length == 0)
        {
            Page? cover = _coverGenerator.CreateCoverGrid(project, version);
            if (cover is not null)
            {
                pages.Add(cover);
            }
        }
        PdfOutlineItem pdfOutlineItem = Traverse(project, entry, coordinates, version, pages);
        string title = project.GetTitle(coordinates, version);
        string author = string.Join(", ", project.Metadata.Creators.Select(c => project.ChooseRequiredValue(c.Name, version)));
        WriterProperties writerProperties = new WriterProperties().AddXmpMetadata();
        PdfWriter pdfWriter = new(stream, writerProperties);
        PdfDocument pdfDocument = new(pdfWriter);
        PdfDocumentInfo pdfDocumentInfo = pdfDocument.GetDocumentInfo();
        pdfDocumentInfo.SetTitle(title);
        pdfDocumentInfo.SetAuthor(author);
        foreach (Page page in pages)
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

    private PdfOutlineItem Traverse(ImgProject project, Entry entry, ImmutableArray<int> coordinates, string version, ICollection<Page> pages)
    {
        PdfOutlineItem pdfOutlineItem = new()
        {
            Text = project.ChooseRequiredValue(entry.Title, version),
            PageNumber = pages.Count + 1,
        };
        foreach (Page page in project.GetPages(coordinates, version))
        {
            pages.Add(page);
        }
        for (int i = 0; i < entry.Entries.Length; i++)
        {
            PdfOutlineItem childItem = Traverse(project, entry.Entries[i], coordinates.Add(i + 1), version, pages);
            pdfOutlineItem.Children.Add(childItem);
        }
        return pdfOutlineItem;
    }

    private void AddPdfOutlineItem(PdfOutlineItem pdfOutlineItem, PdfDocument pdfDocument, PdfOutline pdfOutline)
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
