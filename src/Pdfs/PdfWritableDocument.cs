using iText.IO.Image;
using iText.Kernel.Geom;
using iText.Kernel.Pdf;
using iText.Kernel.Pdf.Canvas;
using iText.Kernel.Pdf.Navigation;

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
        PdfPage pdfPage = _pdfDocument.GetPage(pdfOutlineItem.PageNumber);
        PdfDestination pdfDestination = PdfExplicitDestination.CreateFit(pdfPage);
        newPdfOutline.AddDestination(pdfDestination);
        foreach (PdfOutlineItem child in pdfOutlineItem.Children)
        {
            AddOutlineItem(child, newPdfOutline);
        }
        newPdfOutline.SetOpen(false);
    }
}
