using MediaTypes;
using System.Collections.Immutable;
using System.Linq;

namespace Epubs;

public sealed class EpubPackageInfo
{
    public required int Version { get; init; }
    public required EpubPath OpfFilePath { get; init; }
    public required EpubCover? Cover { get; init; }
    public required IEpubMetadata Metadata { get; init; }
    public required EpubManifest Manifest { get; init; }
    public required EpubSpine Spine { get; init; }

    public ImmutableArray<EpubPath> GetOrderedXhtmlPaths(bool linear)
    {
        ImmutableArray<EpubPath>.Builder builder = ImmutableArray.CreateBuilder<EpubPath>();
        foreach (EpubSpineItem spineItem in Spine.Items)
        {
            if (linear && !string.IsNullOrWhiteSpace(spineItem.Linear) && spineItem.Linear != "yes") continue;
            EpubManifestItem manifestItem = Manifest.Items
                .Single(mi => mi.Id == spineItem.Idref);
            if (manifestItem.MediaType != MediaType.Application.Xhtml_Xml) continue;
            EpubPath xhtmlPath = new(manifestItem.AbsoluteHref);
            builder.Add(xhtmlPath);
        }
        return builder.ToImmutable();
    }
}
