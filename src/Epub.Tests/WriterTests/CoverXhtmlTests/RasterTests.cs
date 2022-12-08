using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Xml.Linq;

namespace Epub.Tests.WriterTests.CoverXhtmlTests;

[TestClass]
public class RasterTests
{
    [TestMethod]
    [DataRow(EpubVersion.Epub3)]
    [DataRow(EpubVersion.Epub2)]
    public void TestGetRasterDocument(EpubVersion epubVersion)
    {
        CoverXhtmlHandler handler = new(epubVersion);
        XDocument document = handler.GetRasterDocument("cover.jpg");
        string expectedDocumentResource = $"WriterTests/Documents/CoverXhtml/Raster/{epubVersion.ToString().ToLowerInvariant()}.xhtml";
        CustomAssert.AreDocumentsEqual(expectedDocumentResource, document);
    }
}
