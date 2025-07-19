using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Images;

internal sealed class ManagedImage : IImage
{
    internal Image InternalImage { get; }

    internal ManagedImage(Image image)
    {
        InternalImage = image;
    }

    public int Width => InternalImage.Width;

    public int Height => InternalImage.Height;

    IImage IImage.ResizeKeepAspectRatio(int width, int height, Color? backgroundColor) => ResizeKeepAspectRatio(width, height, backgroundColor);

    public ManagedImage ResizeKeepAspectRatio(int width, int height, Color? backgroundColor = null)
    {
        ResizeOptions resizeOptions = new()
        {
            Size = new(width, height),
            Mode = ResizeMode.Pad,
        };
        if (backgroundColor is not null)
        {
            resizeOptions.PadColor = backgroundColor.ToManagedInternal();
        }
        Image clone = InternalImage.Clone(i => i.Resize(resizeOptions));
        return new ManagedImage(clone);
    }

    public void SaveTo(Stream stream, ImageFormat imageFormat)
    {
        InternalImage.Save(stream, imageFormat.ToManagedInternal());
    }

    public Task SaveToAsync(Stream stream, ImageFormat imageFormat, CancellationToken cancellationToken = default)
    {
        return InternalImage.SaveAsync(stream, imageFormat.ToManagedInternal(), cancellationToken);
    }

    public void Dispose()
    {
        InternalImage.Dispose();
    }
}
