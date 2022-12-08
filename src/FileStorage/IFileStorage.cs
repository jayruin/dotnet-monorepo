namespace FileStorage;

public interface IFileStorage
{
    public IFile GetFile(params string[] paths);

    public IDirectory GetDirectory(params string[] paths);
}
