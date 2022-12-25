using FileStorage;
using System.Collections.Immutable;

namespace ImgProj.Comparing;

public interface IPageComparer
{
    public void ComparePageVersions(IImgProject project, ImmutableArray<int> coordinates, IDirectory outputDirectory);
}
