using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Epubs.Tests;

[TestClass]
public class EpubPathsTests
{
    [TestMethod]
    [DataRow("a/b/c", "a", "b/c")]
    [DataRow("a/b/c", "a/b/d", "../c")]
    [DataRow("", "a/b/c", "../../..")]
    [DataRow("a/b/c", "", "a/b/c")]
    [DataRow("a/b/c", "b/c/d", "../../../a/b/c")]
    public void TestGetRelativePath(string path, string start, string expected)
    {
        EpubPath epubPath = new(path);
        EpubPath startEpubPath = new(start);
        EpubPath actualEpubPath = epubPath.GetRelativePath(startEpubPath);
        string actual = actualEpubPath.ToString();
        Assert.AreEqual(expected, actual);
    }

    [TestMethod]
    [DataRow("", "a/b/c", "a/b/c")]
    [DataRow("a/b/c", "d/e", "a/b/c/d/e")]
    [DataRow("a/b/c", "../c", "a/b/c")]
    [DataRow("a/b/c", "../../d/e", "a/d/e")]
    [DataRow("a/b/c", "../../../d/e", "d/e")]
    public void TestResolve(string currentDirectoryPath, string epubPath, string expected)
    {
        EpubPath path = new(currentDirectoryPath);
        EpubPath actualPath = path.Resolve(epubPath);
        string actual = actualPath.ToString();
        Assert.AreEqual(expected, actual);
    }
}
