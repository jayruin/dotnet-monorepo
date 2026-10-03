using Images;
using ImgProj.Covers;
using System.Collections.Immutable;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace ImgProj.Exporting;

public sealed class JpegCoverExporter : IExporter
{
    private readonly CoverExporter _coverExporter;

    public JpegCoverExporter(ICoverResolver coverResolver)
    {
        _coverExporter = new(coverResolver, ImageFormat.Jpeg, ExportFormat.CoverJpeg);
    }

    public ExportFormat ExportFormat => _coverExporter.ExportFormat;

    public Task ExportAsync(IImgProject project, Stream stream, ImmutableArray<int> coordinates, string? version, CancellationToken cancellationToken = default)
        => _coverExporter.ExportAsync(project, stream, coordinates, version, cancellationToken);
}
