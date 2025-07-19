using MediaTypes;
using System.Xml.Linq;

namespace Epubs;

internal interface IEpubOpfMetadata : IEpubMetadata
{
    void WriteToOpf(XDocument document, string? newCover, IMediaTypeFileExtensionsMapping mediaTypeFileExtensionsMapping);
}
