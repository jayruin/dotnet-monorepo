using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;
using System.Collections.Generic;
using System.Linq;

namespace Images.Tests;

[TestClass]
public class GridTests
{
    [TestMethod]
    [DynamicData(nameof(GetTestData))]
    public void TestGrid((int, int)?[] imageSizes,
        int? rows, int? columns, bool expand,
        int expectedRows, int expectedColumns,
        int expecteditemWidth, int expecteditemHeight,
        int expectedgridWidth, int expectedgridHeight)
    {
        List<IImage?> images = imageSizes.Select(t =>
        {
            if (t is not (int, int) imageSize) return null;
            (int imageWidth, int imageHeight) = imageSize;
            IImage image = Substitute.For<IImage>();
            image.Width.Returns(imageWidth);
            image.Height.Returns(imageHeight);
            return image;
        }).ToList();
        ImageGridOptions options = new()
        {
            Rows = rows,
            Columns = columns,
            Expand = expand,
        };
        Grid grid = new(images, options);
        Assert.AreEqual(expectedRows, grid.Rows);
        Assert.AreEqual(expectedColumns, grid.Columns);
        Assert.AreEqual(expecteditemWidth, grid.ItemSize.Width);
        Assert.AreEqual(expecteditemHeight, grid.ItemSize.Height);
        Assert.AreEqual(expectedgridWidth, grid.GridSize.Width);
        Assert.AreEqual(expectedgridHeight, grid.GridSize.Height);
    }

    public static IEnumerable<object?[]> GetTestData
    {
        get
        {
            yield return new object?[]
            {
                new (int, int)?[] { (50, 100), (50, 100), (50, 100) },
                null, null, true,
                2, 2,
                50, 100,
                100, 200,
            };
            yield return new object?[]
            {
                new (int, int)?[] { (50, 100), (50, 100), (100, 50) },
                null, null, true,
                2, 2,
                50, 100,
                100, 200,
            };
            yield return new object?[]
            {
                new (int, int)?[] { (50, 100), (100, 50), (100, 50) },
                null, null, true,
                2, 2,
                100, 50,
                200, 100,
            };
            yield return new object?[]
            {
                new (int, int)?[] { (50, 100), (50, 100), (50, 100) },
                null, null, false,
                2, 2,
                25, 50,
                50, 100,
            };
            yield return new object?[]
            {
                new (int, int)?[] { (50, 100), (50, 100), (50, 100), (50, 100), (50, 100) },
                null, null, true,
                3, 3,
                50, 100,
                150, 300,
            };
            yield return new object?[]
            {
                new (int, int)?[] { (50, 100), (50, 100), (50, 100), (50, 100), (50, 100) },
                2, null, true,
                2, 3,
                50, 100,
                150, 200,
            };
            yield return new object?[]
            {
                new (int, int)?[] { (50, 100), (50, 100), (50, 100), (50, 100), (50, 100) },
                null, 2, true,
                3, 2,
                50, 100,
                100, 300,
            };
            yield return new object?[]
            {
                new (int, int)?[] { (50, 100), (50, 100), (50, 100), (50, 100), (50, 100) },
                null, 2, true,
                3, 2,
                50, 100,
                100, 300,
            };
            yield return new object?[]
            {
                new (int, int)?[] { null, null, null, null, (50, 100) },
                null, null, true,
                3, 3,
                50, 100,
                150, 300,
            };
            yield return new object?[]
            {
                new (int, int)?[] { null, (50, 100), (50, 100) },
                1, null, true,
                1, 3,
                50, 100,
                150, 100,
            };
        }
    }
}
