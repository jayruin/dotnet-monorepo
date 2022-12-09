using FileStorage;
using ImgProj.Models;
using System.Collections.Immutable;

namespace ImgProj.Services.PageComparers;

public interface IPageComparer
{
    public void CompareVersions(ImgProject project, ImmutableArray<int> coordinates, IDirectory outputDirectory);
}
