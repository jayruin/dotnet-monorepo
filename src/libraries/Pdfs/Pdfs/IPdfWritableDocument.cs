using System;
using System.Collections.Immutable;
using System.IO;

namespace Pdfs;

public interface IPdfWritableDocument : IDisposable
{
    int NumberOfPages { get; }

    void SetTitle(string title);

    void SetAuthor(string author);

    void AddImagePage(byte[] data);

    void AddOutlineItem(PdfOutlineItem outlineItem);

    PdfCopyPagesResult CopyPages(Stream stream, string? password, ImmutableArray<PdfImageFilter> imageFilters);
}
