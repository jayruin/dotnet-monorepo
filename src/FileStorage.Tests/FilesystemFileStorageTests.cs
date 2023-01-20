using FileStorage.Filesystem;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Testing;

namespace FileStorage.Tests;

[TestClass]
public class FilesystemFileStorageTests : FileStorageTests
{
    private TempDirectory? _tempDirectory;

    [TestInitialize]
    public override void Initialize()
    {
        _tempDirectory = new();
        FileStorage = new FilesystemFileStorage()
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
