using FileStorage;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace EpubProj;

public interface IEpubProjectLoader
{
    Task<IEpubProject> LoadFromDirectoryAsync(IDirectory projectDirectory, CancellationToken cancellationToken = default);
    Task<IEpubProject> LoadAsync(IDirectory contentsDirectory, MutableMetadata mutableMetadata, List<MutableNavItem> mutableNavItems, IFile? coverFile, CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<IFile>> GetImplicitGlobalFilesAsync(IDirectory projectDirectory, CancellationToken cancellationToken = default);
}
