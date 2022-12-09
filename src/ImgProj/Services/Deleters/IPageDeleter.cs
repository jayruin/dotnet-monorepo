using ImgProj.Models;
using System.Collections.Immutable;

namespace ImgProj.Services.Deleters;

public interface IPageDeleter
{
    public void DeletePages(ImgProject project, ImmutableArray<int> coordinates, string? version);
}