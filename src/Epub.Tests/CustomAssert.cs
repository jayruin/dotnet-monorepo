using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.IO;
using System.Xml.Linq;

namespace Epub.Tests;

public static class CustomAssert
{
    public static void AreDocumentsEqual(string expectedDocumentResource, XDocument actualDocument)
    {
        using Stream? expectedStream = EmbeddedResources.Read(expectedDocumentResource);
        using Stream actualStream = new MemoryStream();
        Assert.IsNotNull(expectedStream);
        EpubXml.Save(actualDocument, actualStream);
        actualStream.Seek(0, SeekOrigin.Begin);
        using StreamReader expectedReader = new(expectedStream);
        using StreamReader actualReader = new(actualStream);
        string expectedText = expectedReader.ReadToEnd().Replace("\r\n", "\n");
        string actualText = actualReader.ReadToEnd().Replace("\r\n", "\n");
        Assert.AreEqual(expectedText, actualText);
    }
}
