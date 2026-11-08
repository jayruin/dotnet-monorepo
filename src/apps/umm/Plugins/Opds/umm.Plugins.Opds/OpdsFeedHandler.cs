using Opds;
using System;
using System.Collections.Generic;
using System.Linq;
using umm.Library;
using umm.Server;
using Utils;

namespace umm.Plugins.Opds;

internal static class OpdsFeedHandler
{
    public static OpdsFeed CreateOpdsFeed(string opdsVersion, string selfUrl, IEnumerable<MediaEntry> entries,
       TimeProvider timeProvider,
       bool isAcquisitionFeed, bool isPaginatedFeed,
       string? prevUrl, string? nextUrl)
    {
        string modified = timeProvider.GetUtcNow().UtcDateTime.ToString(DateTimeFormatting.Iso8601);
        OpdsFeed feed = new()
        {
            Title = "umm OPDS Feed",
            Modified = modified,
            Self = selfUrl,
            Prev = prevUrl,
            Next = nextUrl,
            NavigationEntries = isAcquisitionFeed
                ? []
                : [..entries.Select(mediaEntry => new OpdsNavigationEntry()
                    {
                        Identifier = mediaEntry.Id.GetDeterministicGuid().ToUniformResourceName(),
                        Title = mediaEntry.Metadata.Title,
                        NavigationLink = new()
                        {
                            Href = $"/opds/v{opdsVersion}/advancedsearch/{nameof(MediaFullId.VendorId)}={mediaEntry.Id.VendorId}&{nameof(MediaFullId.ContentId)}={mediaEntry.Id.ContentId}/?acquisition=true&paginated={isPaginatedFeed}",
                            Title = mediaEntry.Metadata.Title,
                        },
                        Modified = modified,
                        ImageLinks = [..GetResourceLinks(mediaEntry, true)],
                    })
                ],
            AcquisitionEntries = isAcquisitionFeed
                ? [..entries.Select(mediaEntry => new OpdsAcquisitionEntry()
                    {
                        Identifier = mediaEntry.Id.GetDeterministicGuid().ToUniformResourceName(),
                        Title = mediaEntry.Metadata.Title,
                        Modified = modified,
                        Creators = mediaEntry.Metadata.Creators,
                        Description = mediaEntry.Metadata.Description,
                        Tags = [..mediaEntry.Tags],
                        ImageLinks = [..GetResourceLinks(mediaEntry, true)],
                        AcquisitionLinks = [..GetResourceLinks(mediaEntry, false)],
                    })
                ]
                : [],
        };
        return feed;
    }

    public static IEnumerable<OpdsResourceLink> GetResourceLinks(MediaEntry mediaEntry, bool isImage)
    {
        return mediaEntry.ExportTargets
            .Where(exportTarget => isImage == exportTarget.MediaFormats.Contains(MediaFormat.Artwork))
            .Select(exportTarget => new OpdsResourceLink()
            {
                Href = DownloadEndpoints.GetDownloadUrl(mediaEntry.Id, exportTarget.ExportId),
                Type = exportTarget.MediaType,
                Title = exportTarget.ExportId,
            });
    }
}
