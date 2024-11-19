using System.Collections.Generic;

namespace FileStorage;

public interface IFileStorage
{
    IFile GetFile(params IEnumerable<string> paths);
    IDirectory GetDirectory(params IEnumerable<string> paths);
}
