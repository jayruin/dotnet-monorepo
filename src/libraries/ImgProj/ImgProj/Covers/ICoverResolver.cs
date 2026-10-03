using Images;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;

namespace ImgProj.Covers;

public interface ICoverResolver
{
    Task<IImage> GetCoverImageAsync(IImgProject project, ImmutableArray<int> coordinates, string version, CancellationToken cancellationToken = default);
}
