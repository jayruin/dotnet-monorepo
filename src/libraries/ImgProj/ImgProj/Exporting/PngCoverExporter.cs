using Images;
using ImgProj.Covers;
using System.Collections.Immutable;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace ImgProj.Exporting;

public sealed class PngCoverExporter : IExporter
{
    private readonly CoverExporter _coverExporter;

    public PngCoverExporter(ICoverResolver coverResolver)
    {
        _coverExporter = new(coverResolver, ImageFormat.Png, ExportFormat.CoverPng);
    }

    public ExportFormat ExportFormat => _coverExporter.ExportFormat;

    public Task ExportAsync(IImgProject project, Stream stream, ImmutableArray<int> coordinates, string? version, CancellationToken cancellationToken = default)
        => _coverExporter.ExportAsync(project, stream, coordinates, version, cancellationToken);
}
