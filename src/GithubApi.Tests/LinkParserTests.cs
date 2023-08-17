using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace GithubApi.Tests;

[TestClass]
public sealed class LinkParserTests
{
    [TestMethod]
    public void TestGetNextUri()
    {
        string link = """
            link:
            <https://api.github.com/repositories/1300192/issues?page=2>; rel="prev",
            <https://api.github.com/repositories/1300192/issues?page=4>; rel="next",
            <https://api.github.com/repositories/1300192/issues?page=515>; rel="last",
            <https://api.github.com/repositories/1300192/issues?page=1>; rel="first"
            """;
        link = link.Replace("\r\n", " ").Replace("\n", " ");
        Assert.AreEqual("https://api.github.com/repositories/1300192/issues?page=4", LinkParser.GetNextUri(link));
    }
}
