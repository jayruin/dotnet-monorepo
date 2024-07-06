using iText.IO.Image;
using iText.Kernel.Geom;
using iText.Kernel.Pdf;
using iText.Kernel.Pdf.Canvas;
using iText.Kernel.Pdf.Canvas.Parser;
using iText.Kernel.Pdf.Navigation;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;

namespace Pdfs;

internal sealed class PdfWritableDocument : IPdfWritableDocument
{
    private readonly PdfDocument _pdfDocument;

    public PdfWritableDocument(PdfDocument pdfDocument)
    {
        _pdfDocument = pdfDocument;
    }

    public void Dispose()
    {
        _pdfDocument.Close();
    }

    public int NumberOfPages => _pdfDocument.GetNumberOfPages();

    public void SetAuthor(string author)
    {
        PdfDocumentInfo pdfDocumentInfo = _pdfDocument.GetDocumentInfo();
        pdfDocumentInfo.SetAuthor(author);
    }

    public void SetTitle(string title)
    {
        PdfDocumentInfo pdfDocumentInfo = _pdfDocument.GetDocumentInfo();
        pdfDocumentInfo.SetTitle(title);
    }

    public void AddImagePage(byte[] data)
    {
        ImageData imageData = ImageDataFactory.Create(data);
        PageSize pageSize = new(imageData.GetWidth(), imageData.GetHeight());
        PdfPage pdfPage = _pdfDocument.AddNewPage(pageSize);
        PdfCanvas pdfCanvas = new(pdfPage);
        pdfCanvas.AddImageAt(imageData, 0, 0, false);
    }

    public void AddOutlineItem(PdfOutlineItem outlineItem)
    {
        PdfOutline pdfOutline = _pdfDocument.GetOutlines(true);
        AddOutlineItem(outlineItem, pdfOutline);
        pdfOutline.SetOpen(false);
    }

    private void AddOutlineItem(PdfOutlineItem pdfOutlineItem, PdfOutline pdfOutline)
    {
        PdfOutline newPdfOutline = pdfOutline.AddOutline(pdfOutlineItem.Text);
        PdfPage pdfPage = _pdfDocument.GetPage(pdfOutlineItem.Page);
        PdfDestination pdfDestination = PdfExplicitDestination.CreateFit(pdfPage);
        newPdfOutline.AddDestination(pdfDestination);
        foreach (PdfOutlineItem child in pdfOutlineItem.Children)
        {
            AddOutlineItem(child, newPdfOutline);
        }
        newPdfOutline.SetOpen(false);
    }

    public PdfCopyPagesResult CopyPages(Stream stream, string? password, ImmutableArray<PdfImageFilter> imageFilters)
    {
        using PdfDocument otherPdfDocument = PdfLoader.InternalOpenRead(stream, password);
        ClearOutline(otherPdfDocument);
        IReadOnlyList<byte[]> deletedImages = DeleteImages(otherPdfDocument, imageFilters);
        int pagesCopied = otherPdfDocument.CopyPagesTo(1, otherPdfDocument.GetNumberOfPages(), _pdfDocument).Count;
        return new()
        {
            Pages = pagesCopied,
            DeletedImages = deletedImages,
        };
    }

    private static void ClearOutline(PdfDocument pdfDocument)
    {
        pdfDocument.GetOutlines(true)?.RemoveOutline();
        for (int i = 1; i <= pdfDocument.GetNumberOfPages(); i++)
        {
            IList<PdfOutline>? pageOutlines = pdfDocument.GetPage(i).GetOutlines(true);
            if (pageOutlines is null) continue;
            foreach (PdfOutline pageOutline in pageOutlines)
            {
                pageOutline.RemoveOutline();
            }
        }
    }

    private static IReadOnlyList<byte[]> DeleteImages(PdfDocument pdfDocument, ImmutableArray<PdfImageFilter> imageFilters)
    {
        if (imageFilters.Length == 0) return [];
        ImageDeleter imageDeleter = new(imageFilters);
        PdfCanvasProcessor processor = new(imageDeleter);
        for (int i = 1; i <= pdfDocument.GetNumberOfPages(); i++)
        {
            processor.ProcessPageContent(pdfDocument.GetPage(i));
        }
        return imageDeleter.DeletedImages;
    }
}
