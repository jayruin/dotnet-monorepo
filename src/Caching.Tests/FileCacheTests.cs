using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Caching.Tests;

[TestClass]
public abstract class FileCacheTests
{
    public required IFileCache FileCache { get; set; }

    [TestInitialize]
    public abstract void Initialize();

    [TestCleanup]
    public void Cleanup()
    {
        FileCache.Dispose();
    }

    [TestMethod]
    public async Task TestRead()
    {
        byte[] data = Encoding.ASCII.GetBytes("abc");
        await using MemoryStream originalStream = new(data, false);
        ICachedFile cachedFile = await FileCache.CacheAsync(originalStream, ".txt");
        await using Stream stream = cachedFile.OpenRead();
        await using MemoryStream memoryStream = new();
        await stream.CopyToAsync(memoryStream);
        Assert.IsTrue(memoryStream.ToArray().SequenceEqual(data));
    }

    [TestMethod]
    public async Task TestExtension()
    {
        string extension = ".txt";
        ICachedFile cachedFile = await FileCache.CacheAsync(Stream.Null, extension);
        Assert.AreEqual(extension, cachedFile.Extension);
    }
}
