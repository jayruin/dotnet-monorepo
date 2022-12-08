using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Data;
using System.Xml.Linq;


namespace Epub.Tests.WriterTests.PackageDocumentTests;

[TestClass]
public class ManifestTests
{
    [TestMethod]
    [DataRow(EpubVersion.Epub3, "cover.jpg", "cover-image", "cover-id", "epub3_properties_id.opf")]
    [DataRow(EpubVersion.Epub2, "cover.jpg", "cover-image", "cover-id", "epub2_properties_id.opf")]
    [DataRow(EpubVersion.Epub3, "cover.jpg", null, null, "epub3_noproperties_noid.opf")]
    [DataRow(EpubVersion.Epub2, "cover.jpg", null, null, "epub2_noproperties_noid.opf")]
    public void TestAddItemToManifest(EpubVersion epubVersion, string href, string? manifestProperties, string? itemId, string fileName)
    {
        PackageDocumentHandler handler = new(epubVersion);
        handler.AddItemToManifest(href, manifestProperties, itemId);
        XDocument document = handler.GetDocument();
        string expectedDocumentResource = $"WriterTests/Documents/PackageDocument/Manifest/{fileName}";
        CustomAssert.AreDocumentsEqual(expectedDocumentResource, document);
    }
}
