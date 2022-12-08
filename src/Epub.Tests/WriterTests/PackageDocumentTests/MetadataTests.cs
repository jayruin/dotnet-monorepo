using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Xml.Linq;

namespace Epub.Tests.WriterTests.PackageDocumentTests;

[TestClass]
public class MetadataTests
{
    [TestMethod]
    [DataRow(EpubVersion.Epub3)]
    [DataRow(EpubVersion.Epub2)]
    public void TestAddIdentifier(EpubVersion epubVersion)
    {
        string fileName = $"{epubVersion.ToString().ToLowerInvariant()}.opf";
        PackageDocumentHandler handler = new(epubVersion);
        handler.AddIdentifier("urn:uuid:12345678-1234-1234-1234-123456789012");
        XDocument document = handler.GetDocument();
        string expectedDocumentResource = $"WriterTests/Documents/PackageDocument/Metadata/Identifier/{fileName}";
        CustomAssert.AreDocumentsEqual(expectedDocumentResource, document);
    }

    [TestMethod]
    [DataRow(EpubVersion.Epub3)]
    [DataRow(EpubVersion.Epub2)]
    public void TestAddTitle(EpubVersion epubVersion)
    {
        string fileName = $"{epubVersion.ToString().ToLowerInvariant()}.opf";
        PackageDocumentHandler handler = new(epubVersion);
        handler.AddTitle("Title");
        XDocument document = handler.GetDocument();
        string expectedDocumentResource = $"WriterTests/Documents/PackageDocument/Metadata/Title/{fileName}";
        CustomAssert.AreDocumentsEqual(expectedDocumentResource, document);
    }

    [TestMethod]
    [DataRow(EpubVersion.Epub3)]
    [DataRow(EpubVersion.Epub2)]
    public void TestAddLanguage(EpubVersion epubVersion)
    {
        string fileName = $"{epubVersion.ToString().ToLowerInvariant()}.opf";
        PackageDocumentHandler handler = new(epubVersion);
        handler.AddLanguage("en");
        XDocument document = handler.GetDocument();
        string expectedDocumentResource = $"WriterTests/Documents/PackageDocument/Metadata/Language/{fileName}";
        CustomAssert.AreDocumentsEqual(expectedDocumentResource, document);
    }

    [TestMethod]
    [DataRow(EpubVersion.Epub3, new string[0], "epub3_no_roles.opf")]
    [DataRow(EpubVersion.Epub2, new string[0], "epub2_no_roles.opf")]
    [DataRow(EpubVersion.Epub3, new string[] { "aut", }, "epub3_one_role.opf")]
    [DataRow(EpubVersion.Epub2, new string[] { "aut", }, "epub2_one_role.opf")]
    [DataRow(EpubVersion.Epub3, new string[] { "aut", "ill", }, "epub3_two_roles.opf")]
    [DataRow(EpubVersion.Epub2, new string[] { "aut", "ill", }, "epub2_two_roles.opf")]
    public void TestAddCreator(EpubVersion epubVersion, IEnumerable<string> roles, string fileName)
    {
        PackageDocumentHandler handler = new(epubVersion);
        handler.AddCreator("Creator", roles);
        XDocument document = handler.GetDocument();
        string expectedDocumentResource = $"WriterTests/Documents/PackageDocument/Metadata/Creator/{fileName}";
        CustomAssert.AreDocumentsEqual(expectedDocumentResource, document);
    }

    [TestMethod]
    [DataRow(EpubVersion.Epub3)]
    [DataRow(EpubVersion.Epub2)]
    public void TestAddDate(EpubVersion epubVersion)
    {
        string fileName = $"{epubVersion.ToString().ToLowerInvariant()}.opf";
        PackageDocumentHandler handler = new(epubVersion);
        handler.AddDate(new DateTimeOffset(2000, 1, 1, 0, 0, 0, TimeSpan.Zero));
        XDocument document = handler.GetDocument();
        string expectedDocumentResource = $"WriterTests/Documents/PackageDocument/Metadata/Date/{fileName}";
        CustomAssert.AreDocumentsEqual(expectedDocumentResource, document);
    }

    [TestMethod]
    [DataRow(EpubVersion.Epub3)]
    [DataRow(EpubVersion.Epub2)]
    public void TestAddPrePaginated(EpubVersion epubVersion)
    {
        string fileName = $"{epubVersion.ToString().ToLowerInvariant()}.opf";
        PackageDocumentHandler handler = new(epubVersion);
        handler.AddPrePaginated();
        XDocument document = handler.GetDocument();
        string expectedDocumentResource = $"WriterTests/Documents/PackageDocument/Metadata/PrePaginated/{fileName}";
        CustomAssert.AreDocumentsEqual(expectedDocumentResource, document);
    }

    [TestMethod]
    [DataRow(EpubVersion.Epub3)]
    [DataRow(EpubVersion.Epub2)]
    public void TestAddModified(EpubVersion epubVersion)
    {
        string fileName = $"{epubVersion.ToString().ToLowerInvariant()}.opf";
        PackageDocumentHandler handler = new(epubVersion);
        handler.AddModified(new DateTimeOffset(2000, 1, 1, 0, 0, 0, TimeSpan.Zero));
        XDocument document = handler.GetDocument();
        string expectedDocumentResource = $"WriterTests/Documents/PackageDocument/Metadata/Modified/{fileName}";
        CustomAssert.AreDocumentsEqual(expectedDocumentResource, document);
    }
}
