using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Caching.Tests;

[TestClass]
public class MemoryFileCacheTests : FileCacheTests
{
    [TestInitialize]
    public override void Initialize()
    {
        FileCache = new MemoryFileCache();
    }
}
