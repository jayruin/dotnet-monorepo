using FileStorage;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;

namespace ImgProj.Exporting;

public interface IDirectoryExporter : IExporter
{
    Task ExportAsync(IImgProject project, IDirectory directory, ImmutableArray<int> coordinates, string? version, CancellationToken cancellationToken = default);
}
