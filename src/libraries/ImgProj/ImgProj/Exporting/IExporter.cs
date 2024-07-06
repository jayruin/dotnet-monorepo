using System.Collections.Immutable;
using System.IO;
using System.Threading.Tasks;

namespace ImgProj.Exporting;

public interface IExporter
{
    public ExportFormat ExportFormat { get; }

    public Task ExportAsync(IImgProject project, Stream stream, ImmutableArray<int> coordinates, string? version);
}
