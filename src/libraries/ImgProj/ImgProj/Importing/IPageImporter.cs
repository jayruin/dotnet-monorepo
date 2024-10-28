using FileStorage;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading.Tasks;

namespace ImgProj.Importing;

public interface IPageImporter
{
    Task ImportPagesAsync(IImgProject project, ImmutableArray<int> coordinates, string? version, IDirectory sourceDirectory, IReadOnlyCollection<PageRange> pageRanges);
}