using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Images;

public interface IImage : IDisposable
{
    int Width { get; }
    int Height { get; }
    IImage ResizeKeepAspectRatio(int width, int height, Color? backgroundColor = null);
    void SaveTo(Stream stream, ImageFormat imageFormat);
    Task SaveToAsync(Stream stream, ImageFormat imageFormat, CancellationToken cancellationToken = default);
}
