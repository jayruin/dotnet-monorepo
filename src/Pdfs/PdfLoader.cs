using iText.Kernel.Pdf;
using System.IO;

namespace Pdfs;

public sealed class PdfLoader : IPdfLoader
{
    public IPdfWritableDocument OpenWrite(Stream stream)
    {
        WriterProperties writerProperties = new WriterProperties().AddXmpMetadata();
        PdfWriter pdfWriter = new(stream, writerProperties);
        PdfDocument pdfDocument = new(pdfWriter);
        return new PdfWritableDocument(pdfDocument);
    }
}
