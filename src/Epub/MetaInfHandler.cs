using System.Xml.Linq;

namespace Epub;

public sealed class MetaInfHandler
{
    public EpubVersion Version { get; }

    public MetaInfHandler(EpubVersion epubVersion)
    {
        Version = epubVersion;
    }

    public XDocument GetContainerXmlDocument(string packageDocumentPath)
    {
        if (Version == EpubVersion.Unknown) throw new InvalidEpubVersionException();
        return new XDocument(
            new XDeclaration("1.0", "utf-8", null),
            new XElement((XNamespace)EpubXmlNamespaces.Container + "container",
                new XAttribute("xmlns", EpubXmlNamespaces.Container),
                new XAttribute("version", "1.0"),
                new XElement((XNamespace)EpubXmlNamespaces.Container + "rootfiles",
                    new XElement((XNamespace)EpubXmlNamespaces.Container + "rootfile",
                        new XAttribute("full-path", packageDocumentPath),
                        new XAttribute("media-type", EpubMimetypes.Application.OebpsPackageXml)
                    )
                )
            )
        );
    }
}
