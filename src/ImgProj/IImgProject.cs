using FileStorage;
using System.Collections.Generic;
using System.Collections.Immutable;

namespace ImgProj;

public interface IImgProject
{
    public IDirectory ProjectDirectory { get; set; }

    public string MainVersion { get; }

    public IImmutableDictionary<string, IMetadataVersion> MetadataVersions { get; }

    public IImmutableList<IImgProject> ChildProjects { get; }

    public IImmutableSet<string> ValidPageExtensions { get; }

    public IImgProject GetSubProject(ImmutableArray<int> coordinates);

    public IEnumerable<IPage> EnumeratePages(string version, bool recursive);

    public IPage GetPage(ImmutableArray<int> pageCoordinates, string version);

    public IReadOnlyDictionary<int, IDirectory> GetPageDirectories();

    public IFile FindPageFile(IDirectory pageDirectory, string version);
}
