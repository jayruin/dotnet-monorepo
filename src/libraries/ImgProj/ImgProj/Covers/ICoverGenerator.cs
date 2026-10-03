using Images;
using System.Threading;
using System.Threading.Tasks;

namespace ImgProj.Covers;

public interface ICoverGenerator
{
    Task<IImage?> CreateCoverGridImageAsync(IImgProject project, string version, CancellationToken cancellationToken = default);
}
