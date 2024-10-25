using SkiaSharp;
using System;
using System.IO;
using System.Threading.Tasks;

namespace Images;

internal sealed class NativeImage : IImage
{
    internal SKImage InternalImage { get; }

    internal NativeImage(SKImage skImage)
    {
        InternalImage = skImage;
    }

    public int Width => InternalImage.Width;

    public int Height => InternalImage.Height;

    IImage IImage.ResizeKeepAspectRatio(int width, int height, Color? backgroundColor) => ResizeKeepAspectRatio(width, height, backgroundColor);

    public NativeImage ResizeKeepAspectRatio(int width, int height, Color? backgroundColor = null)
    {
        double imageAspectRatio = new Size(Width, Height).AspectRatio;
        double targetAspectRatio = new Size(width, height).AspectRatio;
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
        using SKBitmap bitmap = SKBitmap.FromImage(InternalImage);
        using SKBitmap resizedBitmap = bitmap.Resize(new SKImageInfo(resizedWidth, resizedHeight), SKFilterQuality.High);
        using SKSurface targetSurface = SKSurface.Create(new SKImageInfo(width, height));
        if (backgroundColor != null)
        {
            targetSurface.Canvas.Clear(backgroundColor.ToNativeInternal());
        }
        targetSurface.Canvas.DrawBitmap(resizedBitmap, new SKPoint(offsetX, offsetY));
        return new NativeImage(targetSurface.Snapshot());
    }

    public void SaveTo(Stream stream, ImageFormat imageFormat)
    {
        SKEncodedImageFormat internalImageFormat = imageFormat.ToNativeInternal();
        using SKData data = InternalImage.Encode(internalImageFormat, 100);
        data.SaveTo(stream);
    }

    public Task SaveToAsync(Stream stream, ImageFormat imageFormat)
    {
        SaveTo(stream, imageFormat);
        return Task.CompletedTask;
    }

    public void Dispose()
    {
        InternalImage.Dispose();
    }
}
