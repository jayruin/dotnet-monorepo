using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Caching.Tests;

[TestClass]
public class MemoryFileCacheTests : FileCacheTests
{
    [TestInitialize]
    public void Initialize()
    {
        FileCache = new MemoryFileCache();
    }
}
