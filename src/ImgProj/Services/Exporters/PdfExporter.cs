using ImgProj.Models;
using ImgProj.Services.Covers;
using iText.Kernel.Pdf;
using iText.Kernel.Pdf.Navigation;
using SkiaSharp;
using System;
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
        public string Text { get; init; } = String.Empty;

        public int PageNumber { get; init; }

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
        await using MemoryStream memoryStream = new();
        string title = project.GetTitle(coordinates, version);
        string author = string.Join(", ", project.Metadata.Creators.Select(c => project.ChooseRequiredValue(c.Name, version)));
        DateTime creation = project.GetLatestTimestamp(coordinates, version)?.UtcDateTime ?? DateTime.Now;
        await WritePagesToPdfAsync(memoryStream, pages, title, author, creation);
        memoryStream.Seek(0, SeekOrigin.Begin);
        AddOutline(memoryStream, stream, pdfOutlineItem);
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

    private static async Task WritePagesToPdfAsync(Stream stream, IReadOnlyCollection<Page> pages, string title, string author, DateTime creation)
    {
        SKDocumentPdfMetadata pdfMetadata = new()
        {
            Title = title,
            Author = author,
            Creation = creation,
        };
        using SKDocument document = SKDocument.CreatePdf(stream, pdfMetadata);
        foreach (Page page in pages)
        {
            await using Stream pageStream = page.OpenRead();
            using SKImage image = SKImage.FromEncodedData(pageStream);
            using SKCanvas canvas = document.BeginPage(image.Width, image.Height);
            canvas.DrawImage(image, 0, 0);
        }
    }

    private void AddOutline(Stream sourceStream, Stream destinationStream, PdfOutlineItem pdfOutlineItem)
    {
        PdfReader pdfReader = new(sourceStream);
        PdfWriter pdfWriter = new(destinationStream);
        PdfDocument pdfDocument = new(pdfReader, pdfWriter);
        PdfOutline pdfOutline = pdfDocument.GetOutlines(true);
        AddPdfOutlineItem(pdfOutlineItem, pdfDocument, pdfOutline);
        pdfDocument.Close();
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
