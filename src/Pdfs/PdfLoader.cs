using iText.Kernel.Pdf;
using System.IO;
using System.Text;

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

    internal static PdfDocument InternalOpenRead(Stream stream, string? password)
    {
        ReaderProperties readerProperties = new();
        if (!string.IsNullOrEmpty(password))
        {
            Encoding encoding = new UTF8Encoding();
            readerProperties = readerProperties.SetPassword(encoding.GetBytes(password));
        }
        PdfReader pdfReader = new(stream, readerProperties);
        PdfDocument pdfDocument = new(pdfReader);
        return pdfDocument;
    }
}
