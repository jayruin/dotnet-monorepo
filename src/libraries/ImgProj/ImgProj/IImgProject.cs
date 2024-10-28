using FileStorage;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading.Tasks;

namespace ImgProj;

public interface IImgProject
{
    IDirectory ProjectDirectory { get; set; }
    string MainVersion { get; }
    IImmutableDictionary<string, IMetadataVersion> MetadataVersions { get; }
    IImmutableList<IImgProject> ChildProjects { get; }
    IImmutableSet<string> ValidPageExtensions { get; }
    IImgProject GetSubProject(ImmutableArray<int> coordinates);
    IAsyncEnumerable<IPage> EnumeratePagesAsync(string version, bool recursive);
    Task<IPage> GetPageAsync(ImmutableArray<int> pageCoordinates, string version);
    Task<IReadOnlyDictionary<int, IDirectory>> GetPageDirectoriesAsync();
    Task<IFile> FindPageFileAsync(IDirectory pageDirectory, string version);
}
