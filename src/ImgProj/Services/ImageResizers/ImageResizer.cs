using ImgProj.Models;
using SkiaSharp;
using System;

namespace ImgProj.Services.ImageResizers;

public sealed class ImageResizer : IImageResizer
{
    public SKImage ResizeImageKeepAspectRatio(SKImage image, int width, int height)
    {
        double imageAspectRatio = new AspectRatio(image.Width, image.Height);
        double targetAspectRatio = new AspectRatio(width, height);
        int resizedWidth;
        int resizedHeight;
        if (targetAspectRatio > imageAspectRatio)
        {
            resizedWidth = Convert.ToInt32(Math.Floor(height * imageAspectRatio));
            resizedHeight = height;
        }
        else
        {
            resizedWidth = width;
            resizedHeight = Convert.ToInt32(Math.Floor(width / imageAspectRatio));
        }
        int offsetX = Convert.ToInt32(Math.Floor((double)(width - resizedWidth) / 2));
        int offsetY = Convert.ToInt32(Math.Floor((double)(height - resizedHeight) / 2));
        using SKBitmap bitmap = SKBitmap.FromImage(image);
        using SKBitmap resizedBitmap = bitmap.Resize(new SKImageInfo(resizedWidth, resizedHeight), SKFilterQuality.High);
        using SKSurface targetSurface = SKSurface.Create(new SKImageInfo(width, height));
        targetSurface.Canvas.DrawBitmap(resizedBitmap, new SKPoint(offsetX, offsetY));
        return targetSurface.Snapshot();
    }
}
