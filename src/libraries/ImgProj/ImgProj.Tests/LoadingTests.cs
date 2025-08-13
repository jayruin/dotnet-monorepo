using FileStorage;
using FileStorage.Memory;
using ImgProj.Loading;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Immutable;

namespace ImgProj.Tests;

[TestClass]
public class LoadingTests
{
    public required IFileStorage FileStorage { get; set; }

    [TestInitialize]
    public void Initialize()
    {
        FileStorage = new MemoryFileStorage();
    }

    [TestMethod]
    public void TestLoadPageSpreads()
    {
        string version = "main";
        IDirectory projectDirectory = FileStorage.GetDirectory(string.Empty);
        MetadataJson metadataJson = new()
        {
            Versions = [version],
            Spreads = [
                new SpreadJson()
                {
                    Left = [1, 2, 1],
                    Right = [1, 2, 2],
                }
            ],
            Root = new EntryJson()
            {
                Entries = [
                    new EntryJson()
                    {
                        Entries = [
                            new EntryJson(),
                            new EntryJson(),
                        ],
                    }
                ],
            },
        };
        IImgProject imgProject = ImgProjectLoader.LoadProject(projectDirectory, metadataJson);
        IImmutableList<IPageSpread> pageSpreads = imgProject.MetadataVersions[version].PageSpreads;
        Assert.HasCount(1, pageSpreads);
        IPageSpread pageSpread = pageSpreads[0];
        CollectionAssert.AreEqual(pageSpread.Left, (int[])[1, 2, 1]);
        CollectionAssert.AreEqual(pageSpread.Right, (int[])[1, 2, 2]);

        pageSpreads = imgProject.GetSubProject([1]).MetadataVersions[version].PageSpreads;
        Assert.HasCount(1, pageSpreads);
        pageSpread = pageSpreads[0];
        CollectionAssert.AreEqual(pageSpread.Left, (int[])[2, 1]);
        CollectionAssert.AreEqual(pageSpread.Right, (int[])[2, 2]);

        pageSpreads = imgProject.GetSubProject([1, 1]).MetadataVersions[version].PageSpreads;
        Assert.IsEmpty(pageSpreads);

        pageSpreads = imgProject.GetSubProject([1, 2]).MetadataVersions[version].PageSpreads;
        Assert.HasCount(1, pageSpreads);
        pageSpread = pageSpreads[0];
        CollectionAssert.AreEqual(pageSpread.Left, (int[])[1]);
        CollectionAssert.AreEqual(pageSpread.Right, (int[])[2]);
    }

    [TestMethod]
    [DataRow(ReadingDirection.LTR)]
    [DataRow(ReadingDirection.RTL)]
    public void TestLoadReadingDirection(ReadingDirection readingDirection)
    {
        string version = "main";
        IDirectory projectDirectory = FileStorage.GetDirectory(string.Empty);
        MetadataJson metadataJson = new()
        {
            Versions = [version],
            Direction = readingDirection,
            Root = new EntryJson()
            {
                Entries = [
                    new EntryJson(),
                ],
            },
        };
        IImgProject imgProject = ImgProjectLoader.LoadProject(projectDirectory, metadataJson);
        Assert.AreEqual(readingDirection, imgProject.MetadataVersions[version].ReadingDirection);
        Assert.AreEqual(readingDirection, imgProject.GetSubProject([1]).MetadataVersions[version].ReadingDirection);
    }
}
