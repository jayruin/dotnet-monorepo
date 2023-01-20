using FileStorage.FileSystem;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Testing;

namespace FileStorage.Tests;

[TestClass]
public class FileSystemFileStorageTests : FileStorageTests
{
    private TempDirectory? _tempDirectory;

    [TestInitialize]
    public override void Initialize()
    {
        _tempDirectory = new();
        FileStorage = new FileSystemFileStorage()
        {
            BasePath = _tempDirectory.DirectoryPath,
        };
    }

    [TestCleanup]
    public override void Cleanup()
    {
        _tempDirectory?.Dispose();
    }
}
