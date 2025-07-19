using System;
using System.Xml.Linq;

namespace Epubs;

internal static class EpubMetadata
{
    public static IEpubOpfMetadata ReadFromOpf(int version, XDocument opfDocument)
    {
        IEpubOpfMetadata metadata = version switch
        {
            3 => Epub3Metadata.ReadFromOpf(opfDocument),
            2 => Epub2Metadata.ReadFromOpf(opfDocument),
            _ => throw new InvalidOperationException($"Epub version {version} is not supported."),
        };
        return metadata;
    }
}
