using SkiaSharp;

namespace Images;

internal static class NativeConversions
{
    extension(Color color)
    {
        public SKColor ToNativeInternal()
        {
            return new SKColor(color.Red, color.Green, color.Blue, color.Alpha);
        }
    }

    extension(ImageFormat imageFormat)
    {
        public SKEncodedImageFormat ToNativeInternal()
        {
            return imageFormat switch
            {
                ImageFormat.Png => SKEncodedImageFormat.Png,
                ImageFormat.Webp => SKEncodedImageFormat.Webp,
                ImageFormat.Jpeg or _ => SKEncodedImageFormat.Jpeg,
            };
        }
    }
}
