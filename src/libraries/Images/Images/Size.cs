using System;

namespace Images;

internal sealed class Size
{
    public int Width { get; }

    public int Height { get; }

    public int AspectRatioWidth { get; }

    public int AspectRatioHeight { get; }

    public double AspectRatio { get; }

    public Size(int width, int height)
    {
        if (width <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(width), "Width must be greater than 0!");
        }
        if (height <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(height), "Height must be greater than 0!");
        }

        Width = width;
        Height = height;

        int gcd = GCD(width, height);
        (AspectRatioWidth, AspectRatioHeight) = gcd > 0
            ? (width / gcd, height / gcd)
            : (width, height);

        AspectRatio = AspectRatioWidth / (double)AspectRatioHeight;
    }

    public void Deconstruct(out int width, out int height)
    {
        width = Width;
        height = Height;
    }

    private static int GCD(int a, int b)
    {
        if (a < 0) a = -a;
        if (b < 0) b = -b;
        while (a > 0 && b > 0)
        {
            if (a > b) a %= b;
            else b %= a;
        }
        return a | b;
    }
}
