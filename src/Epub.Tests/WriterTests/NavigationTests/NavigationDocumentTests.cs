using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using System.Xml.Linq;

namespace Epub.Tests.WriterTests.NavigationTests;

[TestClass]
public class NavigationDocumentTests
{
    [TestMethod]
    [DataRow(EpubVersion.Epub3)]
    [DataRow(EpubVersion.Epub2)]
    public void TestAddNavItem(EpubVersion epubVersion)
    {
        string fileName = $"{epubVersion.ToString().ToLowerInvariant()}_toc.xhtml";
        NavigationDocumentHandler handler = new(epubVersion);
        EpubNavItem navItem1 = new()
        {
            Text = "Chapter 1",
            Reference = "chapter-1.xhtml",
        };
        EpubNavItem navItem2 = new()
        {
            Text = "Chapter 2",
            Reference = "chapter-2.xhtml",
            Children = new List<EpubNavItem>()
            {
                new EpubNavItem()
                {
                    Text = "Chapter 2.1",
                    Reference = "chapter-2-1.xhtml",
                },
                new EpubNavItem()
                {
                    Text = "Chapter 2.2",
                    Reference = "chapter-2-2.xhtml",
                }
            },
        };
        handler.AddNavItem(navItem1);
        handler.AddNavItem(navItem2);
        XDocument document = handler.GetDocument();
        string expectedDocumentResource = $"WriterTests/Documents/Navigation/NavigationDocument/{fileName}";
        CustomAssert.AreDocumentsEqual(expectedDocumentResource, document);
    }

    [TestMethod]
    [DataRow(EpubVersion.Epub3)]
    [DataRow(EpubVersion.Epub2)]
    public void TestAddItemToLandmarks(EpubVersion epubVersion)
    {
        string fileName = $"{epubVersion.ToString().ToLowerInvariant()}_landmarks.xhtml";
        NavigationDocumentHandler handler = new(epubVersion);
        handler.AddItemToLandmarks("cover", "Cover", "cover.xhtml");
        XDocument document = handler.GetDocument();
        string expectedDocumentResource = $"WriterTests/Documents/Navigation/NavigationDocument/{fileName}";
        CustomAssert.AreDocumentsEqual(expectedDocumentResource, document);
    }
}
