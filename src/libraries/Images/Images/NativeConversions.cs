using SkiaSharp;

namespace Images;

internal static class NativeConversions
{
    public static SKColor ToNativeInternal(this Color color)
    {
        return new SKColor(color.Red, color.Green, color.Blue, color.Alpha);
    }

    public static SKEncodedImageFormat ToNativeInternal(this ImageFormat imageFormat)
    {
        return imageFormat switch
        {
            ImageFormat.Png => SKEncodedImageFormat.Png,
            ImageFormat.Webp => SKEncodedImageFormat.Webp,
            ImageFormat.Jpeg or _ => SKEncodedImageFormat.Jpeg,
        };
    }
}
