using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;

namespace Images.Tests;

[TestClass]
public class SizeTests
{
    [TestMethod]
    [DataRow(1, 1, 1, 1)]
    [DataRow(10, 5, 2, 1)]
    [DataRow(5, 10, 1, 2)]
    public void TestWidthHeight(int inputWidth, int inputHeight, int expectedAspectRatioWidth, int expectedAspectRatioHeight)
    {
        Size size = new(inputWidth, inputHeight);
        Assert.AreEqual(expectedAspectRatioWidth, size.AspectRatioWidth);
        Assert.AreEqual(expectedAspectRatioHeight, size.AspectRatioHeight);
    }

    [TestMethod]
    public void TestInvalidWidth()
    {
        Assert.ThrowsException<ArgumentOutOfRangeException>(() => new Size(0, 1));
    }

    [TestMethod]
    public void TestInvalidHeight()
    {
        Assert.ThrowsException<ArgumentOutOfRangeException>(() => new Size(1, 0));
    }
}
