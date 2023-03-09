using System.Collections.Generic;
using System.IO;

namespace Images;

public interface IImageLoader
{
    public IImage LoadImage(Stream stream);

    public IImage LoadImagesToGrid(IEnumerable<Stream?> streams, int? rows = null, int? columns = null, Color? backgroundColor = null);
}