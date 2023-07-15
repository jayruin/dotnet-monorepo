using FileStorage.Mock;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace FileStorage.Tests;

[TestClass]
public class MockFileStorageTests : FileStorageTests
{
    [TestInitialize]
    public override void Initialize()
    {
        FileStorage = new MockFileStorage();
    }

    [TestCleanup]
    public override void Cleanup()
    {
    }
}
