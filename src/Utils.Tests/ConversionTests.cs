using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Utils.Tests;

[TestClass]
public class ConversionTests
{
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
