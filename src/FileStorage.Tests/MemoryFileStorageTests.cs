using FileStorage.Memory;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace FileStorage.Tests;

[TestClass]
public class MemoryFileStorageTests : FileStorageTests
{
    [TestInitialize]
    public override void Initialize()
    {
        FileStorage = new MemoryFileStorage();
    }

    [TestCleanup]
    public override void Cleanup()
    {
    }
}
