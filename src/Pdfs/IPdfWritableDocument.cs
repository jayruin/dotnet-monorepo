using System;

namespace Pdfs;

public interface IPdfWritableDocument : IDisposable
{
    void SetTitle(string title);

    void SetAuthor(string author);

    void AddImagePage(byte[] data);

    void AddOutlineItem(PdfOutlineItem outlineItem);
}
