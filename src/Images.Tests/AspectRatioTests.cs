using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;

namespace Images.Tests;

[TestClass]
public class AspectRatioTests
{
    [TestMethod]
    [DataRow(1, 1, 1, 1)]
    [DataRow(10, 5, 2, 1)]
    [DataRow(5, 10, 1, 2)]
    public void TestWidthHeight(int inputWidth, int inputHeight, int expectedWidth, int expectedHeight)
    {
        AspectRatio aspectRatio = new(inputWidth, inputHeight);
        Assert.AreEqual(expectedWidth, aspectRatio.Width);
        Assert.AreEqual(expectedHeight, aspectRatio.Height);
    }

    [TestMethod]
    public void TestInvalidWidth()
    {
        Assert.ThrowsException<ArgumentOutOfRangeException>(() => new AspectRatio(0, 1));
    }

    [TestMethod]
    public void TestInvalidHeight()
    {
        Assert.ThrowsException<ArgumentOutOfRangeException>(() => new AspectRatio(1, 0));
    }
}
