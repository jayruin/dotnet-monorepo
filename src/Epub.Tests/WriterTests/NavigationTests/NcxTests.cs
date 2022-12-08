using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using System.Xml.Linq;

namespace Epub.Tests.WriterTests.NavigationTests;

[TestClass]
public class NcxTests
{
    [TestMethod]
    [DataRow(EpubVersion.Epub3)]
    [DataRow(EpubVersion.Epub2)]
    public void TestAddIdentifier(EpubVersion epubVersion)
    {
        string fileName = "identifier.ncx";
        NcxHandler handler = new(epubVersion);
        handler.AddIdentifier("urn:uuid:12345678-1234-1234-1234-123456789012");
        XDocument document = handler.GetDocument();
        string expectedDocumentResource = $"WriterTests/Documents/Navigation/Ncx/{fileName}";
        CustomAssert.AreDocumentsEqual(expectedDocumentResource, document);
    }

    [TestMethod]
    [DataRow(EpubVersion.Epub3)]
    [DataRow(EpubVersion.Epub2)]
    public void TestAddTitle(EpubVersion epubVersion)
    {
        string fileName = "title.ncx";
        NcxHandler handler = new(epubVersion);
        handler.AddTitle("Title");
        XDocument document = handler.GetDocument();
        string expectedDocumentResource = $"WriterTests/Documents/Navigation/Ncx/{fileName}";
        CustomAssert.AreDocumentsEqual(expectedDocumentResource, document);
    }

    [TestMethod]
    [DataRow(EpubVersion.Epub3)]
    [DataRow(EpubVersion.Epub2)]
    public void TestAddNavItem(EpubVersion epubVersion)
    {
        string fileName = "navMap.ncx";
        NcxHandler handler = new(epubVersion);
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
        string expectedDocumentResource = $"WriterTests/Documents/Navigation/Ncx/{fileName}";
        CustomAssert.AreDocumentsEqual(expectedDocumentResource, document);
    }
}
