using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace Images;

public sealed class ImageLoader : IImageLoader
{
    private readonly IImageLoader _internalImageLoader;

    public ImageLoader()
    {
        _internalImageLoader = RuntimeFeature.IsDynamicCodeSupported
            ? new NativeImageLoader()
            : new ManagedImageLoader();
    }

    public IImage LoadImage(Stream stream) => _internalImageLoader.LoadImage(stream);

    public Task<IImage> LoadImageAsync(Stream stream) => _internalImageLoader.LoadImageAsync(stream);

    public IImage LoadImagesToGrid(IEnumerable<Stream?> streams, ImageGridOptions? options = null)
        => _internalImageLoader.LoadImagesToGrid(streams, options);

    public Task<IImage> LoadImagesToGridAsync(IEnumerable<Stream?> streams, ImageGridOptions? options = null)
        => _internalImageLoader.LoadImagesToGridAsync(streams, options);
}
