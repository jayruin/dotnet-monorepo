using FileStorage;
using ImgProj.Models;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading.Tasks;

namespace ImgProj.Services.Importers;

public interface IPageImporter
{
    public Task ImportPagesAsync(ImgProject project, ImmutableArray<int> coordinates, string? version, IDirectory sourceDirectory, IReadOnlyCollection<PageRange> pageRanges);
}