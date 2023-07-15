using System;
using System.IO;

namespace Images;

public interface IImage : IDisposable
{
    public int Width { get; }

    public int Height { get; }

    public IImage ResizeKeepAspectRatio(int width, int height, Color? backgroundColor = null);

    public void SaveTo(Stream stream, ImageFormat imageFormat);
}
