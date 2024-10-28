using System.Collections.Immutable;
using System.Threading.Tasks;

namespace ImgProj.Deleting;

public interface IPageDeleter
{
    Task DeletePagesAsync(IImgProject project, ImmutableArray<int> coordinates, string? version);
}
