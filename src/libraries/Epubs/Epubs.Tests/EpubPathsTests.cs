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
        string actual = string.Join('/',
            EpubPaths.GetRelativePath(
                string.IsNullOrWhiteSpace(path) ? [] : [.. path.Split('/')],
                string.IsNullOrWhiteSpace(start) ? [] : [.. start.Split('/')]));
        Assert.AreEqual(expected, actual);
    }
}
