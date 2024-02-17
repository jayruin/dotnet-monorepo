using System.Collections.Generic;

namespace Pdfs;

public sealed class PdfCopyPagesResult
{
    public required int Pages { get; init; }

    public required IReadOnlyList<byte[]> DeletedImages { get; init; }
}
