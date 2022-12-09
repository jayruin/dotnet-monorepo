using SkiaSharp;

namespace ImgProj.Services.ImageResizers;

public interface IImageResizer
{
    public SKImage ResizeImageKeepAspectRatio(SKImage image, int width, int height);
}
