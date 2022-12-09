using System;

namespace ImgProj.Models;

public sealed record AspectRatio
{
    public int Width { get; }

    public int Height { get; }

    public AspectRatio(int width, int height)
    {
        if (width <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(width), "Width must be grater than 0!");
        }
        if (height <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(height), "Height must be grater than 0!");
        }
        int gcd = GCD(width, height);
        if (gcd > 0)
        {
            (Width, Height) = (width / gcd, height / gcd);
        }
        else
        {
            (Width, Height) = (width, height);
        }
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

    public static implicit operator double(AspectRatio aspectRatio)
    {
        return aspectRatio.Width / (double)aspectRatio.Height;
    }
}
