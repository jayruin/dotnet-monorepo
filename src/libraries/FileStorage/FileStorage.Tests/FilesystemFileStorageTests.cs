using FileStorage.Filesystem;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Temp;

namespace FileStorage.Tests;

[TestClass]
public class FilesystemFileStorageTests : FileStorageTests
{
    private TempDirectory? _tempDirectory;

    [TestInitialize]
    public void Initialize()
    {
        _tempDirectory = new();
        FileStorage = new FilesystemFileStorage()
        {
            BasePath = _tempDirectory.DirectoryPath,
        };
    }

    [TestCleanup]
    public void Cleanup()
    {
        _tempDirectory?.Dispose();
    }
}
