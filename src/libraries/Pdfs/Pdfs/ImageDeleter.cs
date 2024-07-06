using iText.Kernel.Pdf.Canvas.Parser;
using iText.Kernel.Pdf.Canvas.Parser.Data;
using iText.Kernel.Pdf.Canvas.Parser.Listener;
using iText.Kernel.Pdf.Xobject;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace Pdfs;

internal sealed class ImageDeleter : IEventListener
{
    private readonly ImmutableArray<PdfImageFilter> _filters;
    private readonly float _delta = 0.1f;
    private readonly List<byte[]> _deletedImages = [];

    public IReadOnlyList<byte[]> DeletedImages => _deletedImages;

    public ImageDeleter(ImmutableArray<PdfImageFilter> filters)
    {
        _filters = filters;
    }

    public void EventOccurred(IEventData data, EventType type)
    {
        if (type != EventType.RENDER_IMAGE) return;
        ImageRenderInfo renderInfo = (ImageRenderInfo)data;
        PdfImageXObject pdfImage = renderInfo.GetImage();
        float width = pdfImage.GetWidth();
        float height = pdfImage.GetHeight();
        bool shouldFilter = _filters.Any(f => Math.Abs(f.Width - width) < _delta && Math.Abs(f.Height - height) < _delta);
        if (!shouldFilter) return;
        _deletedImages.Add(pdfImage.GetImageBytes());
        pdfImage.GetPdfObject().Clear();
    }

    public ICollection<EventType> GetSupportedEvents()
    {
        return [EventType.RENDER_IMAGE];
    }
}
