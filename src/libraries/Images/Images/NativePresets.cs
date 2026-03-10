using SkiaSharp;

namespace Images;

internal static class NativePresets
{
    public static SKSamplingOptions GetHighestQualitySamplingOptions(Size sourceSize, Size targetSize)
    {
        bool isUpscaling = sourceSize.Width < targetSize.Width || sourceSize.Height < targetSize.Height;
        return isUpscaling
            ? new SKSamplingOptions(SKCubicResampler.Mitchell)
            : new SKSamplingOptions(SKFilterMode.Linear, SKMipmapMode.Linear);
    }
}
