using FileStorage;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;

namespace ImgProj;

public interface IImgProject
{
    IDirectory ProjectDirectory { get; }
    string MainVersion { get; }
    IImmutableDictionary<string, IMetadataVersion> MetadataVersions { get; }
    IImmutableList<IImgProject> ChildProjects { get; }
    IImmutableSet<string> ValidPageExtensions { get; }
    IImgProject GetSubProject(ImmutableArray<int> coordinates);
    IAsyncEnumerable<IPage> EnumeratePagesAsync(string version, bool recursive, CancellationToken cancellationToken = default);
    Task<IPage> GetPageAsync(ImmutableArray<int> pageCoordinates, string version, CancellationToken cancellationToken = default);
    Task<IReadOnlyDictionary<int, IDirectory>> GetPageDirectoriesAsync(CancellationToken cancellationToken = default);
    Task<IFile> FindPageFileAsync(IDirectory pageDirectory, string version, CancellationToken cancellationToken = default);
}
