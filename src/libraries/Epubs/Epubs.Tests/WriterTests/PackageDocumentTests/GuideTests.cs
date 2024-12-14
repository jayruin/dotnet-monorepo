using MediaTypes;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Data;
using System.Xml.Linq;

namespace Epubs.Tests.WriterTests.PackageDocumentTests;

[TestClass]
public class GuideTests
{
    [TestMethod]
    [DataRow(EpubVersion.Epub3)]
    [DataRow(EpubVersion.Epub2)]
    public void TestAddReferenceToGuide(EpubVersion epubVersion)
    {
        string fileName = $"{epubVersion.ToString().ToLowerInvariant()}.opf";
        PackageDocumentHandler handler = new(epubVersion, MediaTypeFileExtensionsMapping.Default);
        handler.AddReferenceToGuide("cover", "Cover", "cover.xhtml");
        XDocument document = handler.GetDocument();
        string expectedDocumentResource = $"WriterTests/Documents/PackageDocument/Guide/{fileName}";
        CustomAssert.AreDocumentsEqual(expectedDocumentResource, document);
    }
}
