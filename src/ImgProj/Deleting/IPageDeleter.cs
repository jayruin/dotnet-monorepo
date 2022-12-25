using System.Collections.Immutable;

namespace ImgProj.Deleting;

public interface IPageDeleter
{
    public void DeletePages(IImgProject project, ImmutableArray<int> coordinates, string? version);
}
