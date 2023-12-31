using FileStorage;
using FileStorage.Memory;
using ImgProj.Loading;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Immutable;
using System.Linq;

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
        Assert.AreEqual(1, pageSpreads.Count);
        IPageSpread pageSpread = pageSpreads[0];
        Assert.IsTrue(pageSpread.Left.SequenceEqual([1, 2, 1]));
        Assert.IsTrue(pageSpread.Right.SequenceEqual([1, 2, 2]));

        pageSpreads = imgProject.GetSubProject([1]).MetadataVersions[version].PageSpreads;
        Assert.AreEqual(1, pageSpreads.Count);
        pageSpread = pageSpreads[0];
        Assert.IsTrue(pageSpread.Left.SequenceEqual([2, 1]));
        Assert.IsTrue(pageSpread.Right.SequenceEqual([2, 2]));

        pageSpreads = imgProject.GetSubProject([1, 1]).MetadataVersions[version].PageSpreads;
        Assert.AreEqual(0, pageSpreads.Count);

        pageSpreads = imgProject.GetSubProject([1, 2]).MetadataVersions[version].PageSpreads;
        Assert.AreEqual(1, pageSpreads.Count);
        pageSpread = pageSpreads[0];
        Assert.IsTrue(pageSpread.Left.SequenceEqual([1]));
        Assert.IsTrue(pageSpread.Right.SequenceEqual([2]));
    }
}
