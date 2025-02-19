using FileStorage;
using System.Collections.Immutable;
using System.Threading.Tasks;

namespace ImgProj.Comparing;

public interface IPageComparer
{
    Task ComparePageVersionsAsync(IImgProject project, ImmutableArray<int> coordinates, IDirectory outputDirectory);
}
