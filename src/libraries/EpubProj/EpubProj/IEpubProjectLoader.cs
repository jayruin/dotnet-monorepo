using FileStorage;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace EpubProj;

public interface IEpubProjectLoader
{
    Task<IEpubProject> LoadFromDirectoryAsync(IDirectory projectDirectory);
    Task<IReadOnlyCollection<IFile>> GetImplicitGlobalFilesAsync(IDirectory projectDirectory);
}
