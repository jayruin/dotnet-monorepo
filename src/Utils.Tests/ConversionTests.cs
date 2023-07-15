using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Utils.Tests;

[TestClass]
public class ConversionTests
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
    [DataRow(0, 100, "000")]
    [DataRow(1, 9, "1")]
    [DataRow(1, 10, "01")]
    [DataRow(1, 11, "01")]
    [DataRow(1, 99, "01")]
    [DataRow(1, 100, "001")]
    [DataRow(1, 101, "001")]
    [DataRow(-1, 9, "-1")]
    [DataRow(-1, 10, "-01")]
    [DataRow(-1, 11, "-01")]
    [DataRow(-1, 99, "-01")]
    [DataRow(-1, 100, "-001")]
    [DataRow(-1, 101, "-001")]

    public void TestIntToPaddedString(int num, int total, string expected)
    {
        string actual = num.ToPaddedString(total);
        Assert.AreEqual(expected, actual);
    }
}
