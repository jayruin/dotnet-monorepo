using System;
using System.Linq;
using System.Xml.Linq;

namespace Epub;

internal sealed class NavigationDocumentHandler
{
    private readonly XDocument _document;

    private readonly XElement _sectionOrBodyElement;

    private readonly Lazy<XElement> _tocOlElementLazy;

    private readonly Lazy<XElement> _landmarksOlElementLazy;

    public XElement TocOlElement => _tocOlElementLazy.Value;

    public XElement LandmarksOlElement => _landmarksOlElementLazy.Value;

    public EpubVersion Version { get; }

    public NavigationDocumentHandler(EpubVersion epubVersion)
    {
        Version = epubVersion;
        if (Version == EpubVersion.Epub3)
        {
            _sectionOrBodyElement = CreateEpub3SectionElement();
            _document = CreateEpub3Document(_sectionOrBodyElement);
            _tocOlElementLazy = new Lazy<XElement>(CreateEpub3TocOlElement);
            _landmarksOlElementLazy = new Lazy<XElement>(CreateEpub3LandmarksOlElement);
        }
        else if (Version == EpubVersion.Epub2)
        {
            _sectionOrBodyElement = CreateEpub2BodyElement();
            _document = CreateEpub2Document(_sectionOrBodyElement);
            _tocOlElementLazy = new Lazy<XElement>(CreateEpub2TocOlElement);
            _landmarksOlElementLazy = new Lazy<XElement>(() => throw new InvalidEpubVersionException());
        }
        else
        {
            throw new InvalidEpubVersionException();
        }
    }

    public XDocument GetDocument() => _document;

    public void AddNavItem(EpubNavItem navItem)
    {
        AddNavLi(navItem, TocOlElement);
    }

    public void AddItemToLandmarks(string type, string title, string href)
    {
        if (Version == EpubVersion.Epub3)
        {
            LandmarksOlElement.Add(
                new XElement((XNamespace)EpubXmlNamespaces.Xhtml + "li",
                    new XElement((XNamespace)EpubXmlNamespaces.Xhtml + "a",
                        new XAttribute((XNamespace)EpubXmlNamespaces.Ops + "type", type),
                        new XAttribute("href", href),
                        title
                    )
                )
            );
        }
    }

    private static XElement CreateEpub3SectionElement()
    {
        return new XElement((XNamespace)EpubXmlNamespaces.Xhtml + "section",
            new XAttribute((XNamespace)EpubXmlNamespaces.Ops + "type", "bodymatter chapter"),
            new XElement((XNamespace)EpubXmlNamespaces.Xhtml + "h1",
                "Navigation"
            )
        );
    }

    private static XDocument CreateEpub3Document(XElement sectionElement)
    {
        return new XDocument(
                new XDeclaration("1.0", "utf-8", null),
            new XDocumentType("html", null, null, null),
            new XElement((XNamespace)EpubXmlNamespaces.Xhtml + "html",
                new XAttribute("xmlns", EpubXmlNamespaces.Xhtml),
                new XAttribute(XNamespace.Xmlns + "epub", EpubXmlNamespaces.Ops),
                new XElement((XNamespace)EpubXmlNamespaces.Xhtml + "head",
                    new XElement((XNamespace)EpubXmlNamespaces.Xhtml + "title",
                        "Navigation"
                    ),
                    new XElement((XNamespace)EpubXmlNamespaces.Xhtml + "meta",
                        new XAttribute("charset", "utf-8")
                    ),
                    new XElement((XNamespace)EpubXmlNamespaces.Xhtml + "style",
                        GetCss()
                    )
                ),
                new XElement((XNamespace)EpubXmlNamespaces.Xhtml + "body",
                    sectionElement
                )
            )
        );
    }

    private static XElement CreateEpub2BodyElement()
    {
        return new XElement((XNamespace)EpubXmlNamespaces.Xhtml + "body",
            new XElement((XNamespace)EpubXmlNamespaces.Xhtml + "h1",
                "Navigation"
            )
        );
    }

    private static XDocument CreateEpub2Document(XElement bodyElement)
    {
        return new XDocument(
            new XDeclaration("1.0", "utf-8", null),
            new XDocumentType("html", "-//W3C//DTD XHTML 1.1//EN", "http://www.w3.org/TR/xhtml11/DTD/xhtml11.dtd", null),
            new XElement((XNamespace)EpubXmlNamespaces.Xhtml + "html",
                new XAttribute("xmlns", EpubXmlNamespaces.Xhtml),
                new XElement((XNamespace)EpubXmlNamespaces.Xhtml + "head",
                    new XElement((XNamespace)EpubXmlNamespaces.Xhtml + "title",
                        "Navigation"
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
                bodyElement
            )
        );
    }

    private XElement CreateEpub3TocOlElement()
    {
        XElement olElement = new((XNamespace)EpubXmlNamespaces.Xhtml + "ol");
        _sectionOrBodyElement.Add(
            new XElement((XNamespace)EpubXmlNamespaces.Xhtml + "nav",
                new XAttribute((XNamespace)EpubXmlNamespaces.Ops + "type", "toc"),
                new XElement((XNamespace)EpubXmlNamespaces.Xhtml + "h2",
                    "Table of Contents"
                ),
                olElement
            )
        );
        return olElement;
    }

    private XElement CreateEpub2TocOlElement()
    {
        XElement olElement = new((XNamespace)EpubXmlNamespaces.Xhtml + "ol");
        _sectionOrBodyElement.Add(
            new XElement((XNamespace)EpubXmlNamespaces.Xhtml + "h2",
                "Table of Contents"
            ),
            olElement
        );
        return olElement;
    }

    private XElement CreateEpub3LandmarksOlElement()
    {
        XElement olElement = new((XNamespace)EpubXmlNamespaces.Xhtml + "ol");
        _sectionOrBodyElement.Add(
            new XElement((XNamespace)EpubXmlNamespaces.Xhtml + "nav",
                new XAttribute((XNamespace)EpubXmlNamespaces.Ops + "type", "landmarks"),
                new XAttribute("hidden", "hidden"),
                new XElement((XNamespace)EpubXmlNamespaces.Xhtml + "h2",
                    "Landmarks"
                ),
                olElement
            )
        );
        return olElement;
    }

    private void AddNavLi(EpubNavItem navItem, XElement parent)
    {
        XElement liElement = new((XNamespace)EpubXmlNamespaces.Xhtml + "li",
            new XElement((XNamespace)EpubXmlNamespaces.Xhtml + "a",
                new XAttribute("href", navItem.Reference),
                navItem.Text
            )
        );
        if (navItem.Children.Count > 0)
        {
            XElement olElement = new((XNamespace)EpubXmlNamespaces.Xhtml + "ol");
            foreach (EpubNavItem childNavItem in navItem.Children)
            {
                AddNavLi(childNavItem, olElement);
            }
            liElement.Add(olElement);
        }
        parent.Add(liElement);
    }

    private static string GetCss()
    {
        return string.Join(' ',
            "a",
            "{",
            "text-decoration: none;",
            "}"
        );
    }
}
