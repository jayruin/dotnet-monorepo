using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace FileCaching.Tests;

[TestClass]
public class MemoryFileCacheTests : FileCacheTests
{
    [TestInitialize]
    public override void Initialize()
    {
        FileCache = new MemoryFileCache();
    }
}
