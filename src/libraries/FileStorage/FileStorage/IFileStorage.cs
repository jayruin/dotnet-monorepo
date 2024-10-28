namespace FileStorage;

public interface IFileStorage
{
    IFile GetFile(params string[] paths);
    IDirectory GetDirectory(params string[] paths);
}
