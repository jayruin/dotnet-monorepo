using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Images;

public interface IImageLoader
{
    IImage LoadImage(Stream stream);
    Task<IImage> LoadImageAsync(Stream stream, CancellationToken cancellationToken = default);
    IImage LoadImagesToGrid(IEnumerable<Stream?> streams, ImageGridOptions? options = null);
    Task<IImage> LoadImagesToGridAsync(IEnumerable<Stream?> streams, ImageGridOptions? options = null, CancellationToken cancellationToken = default);
}
