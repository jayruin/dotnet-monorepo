using Images;
using ImgProj.Covers;
using System.Collections.Immutable;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace ImgProj.Exporting;

public sealed class WebpCoverExporter : IExporter
{
    private readonly CoverExporter _coverExporter;

    public WebpCoverExporter(ICoverResolver coverResolver)
    {
        _coverExporter = new(coverResolver, ImageFormat.Webp, ExportFormat.CoverWebp);
    }

    public ExportFormat ExportFormat => _coverExporter.ExportFormat;

    public Task ExportAsync(IImgProject project, Stream stream, ImmutableArray<int> coordinates, string? version, CancellationToken cancellationToken = default)
        => _coverExporter.ExportAsync(project, stream, coordinates, version, cancellationToken);
}
