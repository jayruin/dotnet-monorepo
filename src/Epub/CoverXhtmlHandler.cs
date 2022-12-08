using System.Xml.Linq;

namespace Epub;

internal sealed class CoverXhtmlHandler
{
    public EpubVersion Version { get; }

    public CoverXhtmlHandler(EpubVersion epubVersion)
    {
        Version = epubVersion;
    }

    public XDocument GetRasterDocument(string imgSrc)
    {
        return Version switch
        {
            EpubVersion.Epub3 => GetRasterEpub3Document(imgSrc),
            EpubVersion.Epub2 => GetRasterEpub2Document(imgSrc),
            _ => throw new InvalidEpubVersionException(),
        };
    }

    private static XDocument GetRasterEpub3Document(string imgSrc)
    {
        return new XDocument(
            new XDeclaration("1.0", "utf-8", null),
            new XDocumentType("html", null, null, null),
            new XElement((XNamespace)EpubXmlNamespaces.Xhtml + "html",
                new XAttribute("xmlns", EpubXmlNamespaces.Xhtml),
                new XAttribute(XNamespace.Xmlns + "epub", EpubXmlNamespaces.Ops),
                new XElement((XNamespace)EpubXmlNamespaces.Xhtml + "head",
                    new XElement((XNamespace)EpubXmlNamespaces.Xhtml + "title",
                        "Cover"
                    ),
                    new XElement((XNamespace)EpubXmlNamespaces.Xhtml + "meta",
                        new XAttribute("charset", "utf-8")
                    ),
                    new XElement((XNamespace)EpubXmlNamespaces.Xhtml + "style",
                        GetCss()
                    )
                ),
                new XElement((XNamespace)EpubXmlNamespaces.Xhtml + "body",
                    new XElement((XNamespace)EpubXmlNamespaces.Xhtml + "section",
                        new XAttribute((XNamespace)EpubXmlNamespaces.Ops + "type", "cover"),
                        new XElement((XNamespace)EpubXmlNamespaces.Xhtml + "div",
                            new XAttribute("class", "cover-container"),
                            new XElement((XNamespace)EpubXmlNamespaces.Xhtml + "img",
                                new XAttribute("alt", "Cover"),
                                new XAttribute("src", imgSrc)
                            )
                        )
                    )
                )
            )
        );
    }

    private static XDocument GetRasterEpub2Document(string imgSrc)
    {
        return new XDocument(
            new XDeclaration("1.0", "utf-8", null),
            new XDocumentType("html", "-//W3C//DTD XHTML 1.1//EN", "http://www.w3.org/TR/xhtml11/DTD/xhtml11.dtd", null),
            new XElement((XNamespace)EpubXmlNamespaces.Xhtml + "html",
                new XAttribute("xmlns", EpubXmlNamespaces.Xhtml),
                new XElement((XNamespace)EpubXmlNamespaces.Xhtml + "head",
                    new XElement((XNamespace)EpubXmlNamespaces.Xhtml + "title",
                        "Cover"
                    ),
                    new XElement((XNamespace)EpubXmlNamespaces.Xhtml + "meta",
                        new XAttribute("http-equiv", "content-type"),
                        new XAttribute("content", "application/xhtml+xml; charset=utf-8")
                    ),
                    new XElement((XNamespace)EpubXmlNamespaces.Xhtml + "style",
                        new XAttribute("type", "text/css"),
                        GetCss()
                    )
                ),
                new XElement((XNamespace)EpubXmlNamespaces.Xhtml + "body",
                    new XElement((XNamespace)EpubXmlNamespaces.Xhtml + "div",
                        new XAttribute("class", "cover-container"),
                        new XElement((XNamespace)EpubXmlNamespaces.Xhtml + "img",
                            new XAttribute("alt", "Cover"),
                            new XAttribute("src", imgSrc)
                        )
                    )
                )
            )
        );
    }

    private static string GetCss()
    {
        return string.Join(' ',
            ".cover-container",
            "{",
            "height: 100%;",
            "padding: 0px;",
            "margin: 0px;",
            "display: flex;",
            "flex-direction: column;",
            "justify-content: center;",
            "align-items: center;",
            "text-align: center;",
            "}",
            ".cover-container img",
            "{",
            "max-height: 100%;",
            "max-width: 100%;",
            "object-fit: contain;",
            "}"
        );
    }
}
