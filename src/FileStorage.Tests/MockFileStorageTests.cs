using FileStorage;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Archivist.Tests.FileStorage;

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
