using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Data;
using System.Xml.Linq;

namespace Epub.Tests.WriterTests.PackageDocumentTests;

[TestClass]
public class SpineTests
{
    [TestMethod]
    [DataRow(EpubVersion.Epub3, "page-spread-left", "epub3_properties.opf")]
    [DataRow(EpubVersion.Epub2, "page-spread-left", "epub2_properties.opf")]
    [DataRow(EpubVersion.Epub3, null, "epub3_noproperties.opf")]
    [DataRow(EpubVersion.Epub2, null, "epub2_noproperties.opf")]
    public void TestAddItemToSpine(EpubVersion epubVersion, string? spineProperties, string fileName)
    {
        PackageDocumentHandler handler = new(epubVersion);
        handler.AddItemToSpine(spineProperties, "item-id-1");
        XDocument document = handler.GetDocument();
        string expectedDocumentResource = $"WriterTests/Documents/PackageDocument/Spine/{fileName}";
        CustomAssert.AreDocumentsEqual(expectedDocumentResource, document);
    }

    [TestMethod]
    [DataRow(EpubVersion.Epub3, "epub3_ncx.opf")]
    [DataRow(EpubVersion.Epub2, "epub2_ncx.opf")]
    public void TestAddNcx(EpubVersion epubVersion, string fileName)
    {
        PackageDocumentHandler handler = new(epubVersion);
        handler.AddNcx("ncx-id");
        XDocument document = handler.GetDocument();
        string expectedDocumentResource = $"WriterTests/Documents/PackageDocument/Spine/{fileName}";
        CustomAssert.AreDocumentsEqual(expectedDocumentResource, document);
    }

    [TestMethod]
    [DataRow(EpubVersion.Epub3, "epub3_ltr.opf")]
    [DataRow(EpubVersion.Epub2, "epub2_ltr.opf")]
    public void TestAddLeftToRight(EpubVersion epubVersion, string fileName)
    {
        PackageDocumentHandler handler = new(epubVersion);
        handler.AddLeftToRight();
        XDocument document = handler.GetDocument();
        string expectedDocumentResource = $"WriterTests/Documents/PackageDocument/Spine/{fileName}";
        CustomAssert.AreDocumentsEqual(expectedDocumentResource, document);
    }

    [TestMethod]
    [DataRow(EpubVersion.Epub3, "epub3_rtl.opf")]
    [DataRow(EpubVersion.Epub2, "epub2_rtl.opf")]
    public void TestAddRightToLeft(EpubVersion epubVersion, string fileName)
    {
        PackageDocumentHandler handler = new(epubVersion);
        handler.AddRightToLeft();
        XDocument document = handler.GetDocument();
        string expectedDocumentResource = $"WriterTests/Documents/PackageDocument/Spine/{fileName}";
        CustomAssert.AreDocumentsEqual(expectedDocumentResource, document);
    }
}
