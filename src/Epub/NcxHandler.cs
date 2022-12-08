using System;
using System.Linq;
using System.Xml.Linq;

namespace Epub;

internal sealed class NcxHandler
{
    private readonly XDocument _document;

    private readonly XElement _ncxElement;

    private readonly Lazy<XElement> _navMapElementLazy;

    private int _topLevelNavItemCount = 1;

    public XElement NavMapElement => _navMapElementLazy.Value;

    public EpubVersion Version { get; }

    public NcxHandler(EpubVersion epubVersion)
    {
        if (epubVersion == EpubVersion.Unknown) throw new InvalidEpubVersionException();
        Version = epubVersion;
        _ncxElement = new XElement((XNamespace)EpubXmlNamespaces.Ncx + "ncx",
            new XAttribute("xmlns", EpubXmlNamespaces.Ncx),
            new XAttribute("version", "2005-1")
        );
        _document = new XDocument(
            new XDeclaration("1.0", "utf-8", null),
            _ncxElement
        );
        _navMapElementLazy = new Lazy<XElement>(() =>
        {
            XElement navMapElement = new((XNamespace)EpubXmlNamespaces.Ncx + "navMap");
            _ncxElement.Add(navMapElement);
            return navMapElement;
        });
    }

    public XDocument GetDocument() => _document;

    public void AddIdentifier(string identifier)
    {
        _ncxElement.Add(
            new XElement((XNamespace)EpubXmlNamespaces.Ncx + "head",
                new XElement((XNamespace)EpubXmlNamespaces.Ncx + "meta",
                    new XAttribute("name", "dtb:uid"),
                    new XAttribute("content", identifier)
                )
            )
        );
    }

    public void AddTitle(string title)
    {
        _ncxElement.Add(
            new XElement((XNamespace)EpubXmlNamespaces.Ncx + "docTitle",
                new XElement((XNamespace)EpubXmlNamespaces.Ncx + "text",
                    title
                )
            )
        );
    }

    public void AddNavItem(EpubNavItem navItem)
    {
        AddNavPoint(navItem, NavMapElement, new int[] { _topLevelNavItemCount });
        _topLevelNavItemCount += 1;
    }

    private void AddNavPoint(EpubNavItem navItem, XElement parent, int[] levelCounts)
    {
        XElement navPoint = new((XNamespace)EpubXmlNamespaces.Ncx + "navPoint",
            new XAttribute("id", $"ncx-{string.Join('-', levelCounts)}"),
            new XElement((XNamespace)EpubXmlNamespaces.Ncx + "navLabel",
                new XElement((XNamespace)EpubXmlNamespaces.Ncx + "text",
                    navItem.Text
                )
            ),
            new XElement((XNamespace)EpubXmlNamespaces.Ncx + "content",
                new XAttribute("src", navItem.Reference)
            )
        );
        parent.Add(navPoint);
        for (int i = 0; i < navItem.Children.Count; i++)
        {
            AddNavPoint(navItem.Children[i], navPoint, levelCounts.Append(i + 1).ToArray());
        }
    }
}
