using Images;
using ImgProj.Core;
using System.Threading;
using System.Threading.Tasks;

namespace ImgProj.Covers;

public static class CoverGeneratorExtensions
{
    extension(ICoverGenerator coverGenerator)
    {
        public async Task<IPage?> CreateCoverGridPageAsync(IImgProject project, string version, CancellationToken cancellationToken = default)
        {
            IImage? coverGrid = await coverGenerator.CreateCoverGridImageAsync(project, version, cancellationToken).ConfigureAwait(false);
            if (coverGrid is null) return null;
            using (coverGrid)
            {
                byte[] data = await coverGrid.ToBytesAsync(ImageFormat.Jpeg, cancellationToken).ConfigureAwait(false);
                return new MemoryPage(data, version, ".jpg");
            }
        }
    }
}
