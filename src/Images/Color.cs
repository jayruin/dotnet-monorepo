using SkiaSharp;

namespace Images;

public sealed record Color(byte Red, byte Green, byte Blue, byte Alpha)
{
    internal SKColor ToInternalColor()
    {
        return new SKColor(Red, Green, Blue, Alpha);
    }
}
