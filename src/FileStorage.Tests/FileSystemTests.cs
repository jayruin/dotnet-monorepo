using Microsoft.VisualStudio.TestTools.UnitTesting;
using Testing;

namespace FileStorage.Tests;

[TestClass]
public class FileSystemTests : FileStorageTests
{
    private TempDirectory? _tempDirectory;

    [TestInitialize]
    public override void Initialize()
    {
        _tempDirectory = new();
        FileStorage = new FileSystem()
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
