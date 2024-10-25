using System.Threading.Tasks;

namespace ImgProj.Covers;

public interface ICoverGenerator
{
    public Task<IPage?> CreateCoverGridAsync(IImgProject project, string version);
}
