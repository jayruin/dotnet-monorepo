using System.Xml.Linq;

namespace Epubs;

public static class EpubFxl
{
    public static XDocument CreateSingleImageXhtml(string src, int width, int height)
    {
        return new XDocument(
            new XDeclaration("1.0", "utf-8", null),
            new XDocumentType("html", null, null, null),
            new XElement((XNamespace)EpubXmlNamespaces.Xhtml + "html",
                new XAttribute("xmlns", EpubXmlNamespaces.Xhtml),
                new XAttribute(XNamespace.Xmlns + "epub", EpubXmlNamespaces.Ops),
                new XElement((XNamespace)EpubXmlNamespaces.Xhtml + "head",
                    new XElement((XNamespace)EpubXmlNamespaces.Xhtml + "title",
                        "Paginated Image"
                    ),
                    new XElement((XNamespace)EpubXmlNamespaces.Xhtml + "meta",
                        new XAttribute("charset", "utf-8")
                    ),
                    new XElement((XNamespace)EpubXmlNamespaces.Xhtml + "meta",
                        new XAttribute("name", "viewport"),
                        new XAttribute("content", $"width={width}, height={height}")
                    )
                ),
                new XElement((XNamespace)EpubXmlNamespaces.Xhtml + "body",
                    new XElement((XNamespace)EpubXmlNamespaces.Xhtml + "img",
                        new XAttribute("src", src)
                    )
                )
            )
        );
    }
}
