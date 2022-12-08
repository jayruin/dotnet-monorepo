using FileStorage;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.IO;

namespace Archivist.Tests.FileStorage;

[TestClass]
public class FileSystemTests : FileStorageTests
{
    private string _tempDirectoryPath = string.Empty;

    [TestInitialize]
    public override void Initialize()
    {
        _tempDirectoryPath = Path.Join(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(_tempDirectoryPath);
        FileStorage = new FileSystem()
        {
            BasePath = _tempDirectoryPath,
        };
    }

    [TestCleanup]
    public override void Cleanup()
    {
        try { Directory.Delete(_tempDirectoryPath, true); }
        catch { }
    }
}
