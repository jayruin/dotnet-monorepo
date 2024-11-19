using FileStorage.Memory;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace FileStorage.Tests;

[TestClass]
public class MemoryFileStorageTests : FileStorageTests
{
    [TestInitialize]
    public void Initialize()
    {
        FileStorage = new MemoryFileStorage();
    }

    [TestCleanup]
    public void Cleanup()
    {
    }
}
