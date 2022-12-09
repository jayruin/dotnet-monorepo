using ImgProj.Models;
using System.Collections.Immutable;
using System.IO;
using System.Threading.Tasks;

namespace ImgProj.Services.Exporters;

public interface IExporter
{
    public ExportFormat ExportFormat { get; }

    public Task ExportAsync(ImgProject project, Stream stream, ImmutableArray<int> coordinates, string? version);
}