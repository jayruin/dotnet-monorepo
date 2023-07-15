using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.IO;
using System.Threading.Tasks;

namespace FileCaching.Tests;

[TestClass]
public class TempFileCacheTests : FileCacheTests
{
    [TestInitialize]
    public override void Initialize()
    {
        FileCache = new TempFileCache();
    }

    [TestMethod]
    public async Task TestDispose()
    {
        TempCachedFile cachedFile = (TempCachedFile)await FileCache.CacheAsync(Stream.Null, ".txt");
        Assert.IsTrue(File.Exists(cachedFile.FilePath));
        FileCache.Dispose();
        Assert.IsFalse(File.Exists(cachedFile.FilePath));
    }

    [TestMethod]
    public async Task TestDisposeAsync()
    {
        TempCachedFile cachedFile = (TempCachedFile)await FileCache.CacheAsync(Stream.Null, ".txt");
        Assert.IsTrue(File.Exists(cachedFile.FilePath));
        await FileCache.DisposeAsync();
        Assert.IsFalse(File.Exists(cachedFile.FilePath));
    }
}
