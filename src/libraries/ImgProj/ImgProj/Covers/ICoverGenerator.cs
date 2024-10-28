using System.Threading.Tasks;

namespace ImgProj.Covers;

public interface ICoverGenerator
{
    Task<IPage?> CreateCoverGridAsync(IImgProject project, string version);
}
