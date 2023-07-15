using FileStorage;
using Images;
using iText.Kernel.Pdf.Canvas.Parser;
using iText.Kernel.Pdf.Canvas.Parser.Data;
using iText.Kernel.Pdf.Canvas.Parser.Listener;
using iText.Kernel.Pdf.Xobject;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;

namespace PdfEdit;

public sealed class ImageDeleter : IEventListener
{
    private readonly IImmutableList<(float, float)> _sizes;
    private readonly IDirectory? _outputDirectory;
    private readonly string? _id;
    private readonly IImageLoader _imageLoader;
    private int _count;

    public ImageDeleter(IEnumerable<(float, float)> sizes, IDirectory? outputDirectory = null, string? id = null)
    {
        _sizes = sizes.ToImmutableArray();
        _outputDirectory = outputDirectory;
        _id = id;
        _imageLoader = new ImageLoader();
    }

    public void EventOccurred(IEventData data, EventType type)
    {
        if (type != EventType.RENDER_IMAGE) return;
        ImageRenderInfo renderInfo = (ImageRenderInfo)data;
        PdfImageXObject pdfImage = renderInfo.GetImage();
        float width = pdfImage.GetWidth();
        float height = pdfImage.GetHeight();
        if (_sizes.Contains((width, height)))
        {
            _count += 1;
            if (_outputDirectory is not null && _id is not null)
            {
                using Stream memoryStream = new MemoryStream(pdfImage.GetImageBytes(), false);
                IImage image = _imageLoader.LoadImage(memoryStream);
                IFile outputFile = _outputDirectory.GetFile($"{_id}-{_count}.jpg");
                using Stream outputStream = outputFile.OpenWrite();
                image.SaveTo(outputStream, ImageFormat.Jpeg);
            }
            pdfImage.GetPdfObject().Clear();
        }
    }

    public ICollection<EventType> GetSupportedEvents()
    {
        return new[] { EventType.RENDER_IMAGE };
    }
}
