using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;

namespace Epub;

public static class EpubXml
{
    public static async Task SaveAsync(XDocument document, Stream stream)
    {
        XmlWriterSettings settings = CreateXmlWriterSettings();
        await using XmlWriter writer = XmlWriter.Create(stream, settings);
        await document.SaveAsync(writer, CancellationToken.None);
    }

    public static void Save(XDocument document, Stream stream)
    {
        XmlWriterSettings settings = CreateXmlWriterSettings();
        using XmlWriter writer = XmlWriter.Create(stream, settings);
        document.Save(writer);
    }

    private static XmlWriterSettings CreateXmlWriterSettings()
    {
        // Trailing whitespace after doctype declaration can be removed using XmlTextWriter
        // However, there is no async support for the older XmlTextWriter

        return new XmlWriterSettings()
        {
            Async = true,
            Encoding = new UTF8Encoding(),
            Indent = true,
            IndentChars = "    ",
            NamespaceHandling = NamespaceHandling.OmitDuplicates,
        };
    }
}
