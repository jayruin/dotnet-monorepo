using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Utils.Tests;

[TestClass]
public class StringTests
{
    [TestMethod]
    [DataRow("!- -a- -!", "a")]
    [DataRow("a!=b", "ab")]
    [DataRow("a \t\nb", "a-b")]
    [DataRow("a- -b", "a-b")]
    [DataRow("a - b", "a-b")]
    [DataRow("Abc-DEF", "abc-def")]
    [DataRow("a_b", "a-b")]
    [DataRow("ä ö ü", "a-o-u")]
    [DataRow("Ä Ö Ü", "a-o-u")]
    [DataRow("a1 b2 c3", "a1-b2-c3")]
    public void TestSlugify(string input, string expected)
    {
        string actual = input.Slugify();
        Assert.AreEqual(expected, actual);
    }

    [TestMethod]
    [DataRow("")]
    [DataRow("a", "a1", "a2", "a3")]
    [DataRow("ab", "ab", "abc")]
    [DataRow("ab", "abc", "ab")]
    [DataRow("", "", "abc")]
    [DataRow("", "abc", "")]
    public void TestLongestCommonPrefix(string expected, params string[] strings)
    {
        Assert.AreEqual(expected, strings.LongestCommonPrefix());
    }
}
