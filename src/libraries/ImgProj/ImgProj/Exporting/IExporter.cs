using System.Collections.Immutable;
using System.IO;
using System.Threading.Tasks;

namespace ImgProj.Exporting;

public interface IExporter
{
    ExportFormat ExportFormat { get; }
    Task ExportAsync(IImgProject project, Stream stream, ImmutableArray<int> coordinates, string? version);
}
