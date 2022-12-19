using SkiaSharp;
using System;
using System.IO;

namespace Images;

internal sealed class Image : IImage
{
    internal SKImage InternalImage { get; }

    internal Image(SKImage skImage)
    {
        InternalImage = skImage;
    }

    public int Width => InternalImage.Width;

    public int Height => InternalImage.Height;

    public IImage ResizeKeepAspectRatio(int width, int height, Color? backgroundColor = null)
    {
        return new Image(ResizeKeepAspectRatio(InternalImage, width, height, backgroundColor));
    }

    public void SaveTo(Stream stream, ImageFormat imageFormat)
    {
        SKEncodedImageFormat internalImageFormat = GetInternalImageFormat(imageFormat);
        using SKData data = InternalImage.Encode(internalImageFormat, 100);
        data.SaveTo(stream);
    }

    public void Dispose()
    {
        InternalImage.Dispose();
    }

    internal static SKImage ResizeKeepAspectRatio(SKImage image, int width, int height, Color? backgroundColor = null)
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
        if (backgroundColor != null)
        {
            targetSurface.Canvas.Clear(backgroundColor.ToInternalColor());
        }
        targetSurface.Canvas.DrawBitmap(resizedBitmap, new SKPoint(offsetX, offsetY));
        return targetSurface.Snapshot();
    }

    private static SKEncodedImageFormat GetInternalImageFormat(ImageFormat imageFormat)
    {
        return imageFormat switch
        {
            ImageFormat.Jpeg => SKEncodedImageFormat.Jpeg,
            ImageFormat.Png => SKEncodedImageFormat.Png,
            _ => SKEncodedImageFormat.Jpeg,
        };
    }
}
