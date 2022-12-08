using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Xml.Linq;

namespace Epub.Tests.WriterTests.MetaInfTests;

[TestClass]
public class ContainerXmlTests
{
    [TestMethod]
    [DataRow(EpubVersion.Epub3)]
    [DataRow(EpubVersion.Epub2)]
    public void TestGetContainerXmlDocument(EpubVersion epubVersion)
    {
        MetaInfHandler handler = new(epubVersion);
        XDocument document = handler.GetContainerXmlDocument("OEBPS/package.opf");
        string expectedDocumentResource = $"WriterTests/Documents/MetaInf/container.xml";
        CustomAssert.AreDocumentsEqual(expectedDocumentResource, document);
    }
}
