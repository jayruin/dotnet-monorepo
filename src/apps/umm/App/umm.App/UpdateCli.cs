using Apps;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Primitives;
using System;
using System.Collections.Generic;
using System.CommandLine;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using umm.ExportCache;
using umm.Library;
using umm.SearchIndex;
using umm.Vendors.Abstractions;

namespace umm.App;

internal static class UpdateCli
{
    public static Command CreateCommand()
    {
        Option<string?> searchQueryOption = new("--search", "-s")
        {
            DefaultValueFactory = _ => null,
        };
        Option<bool> forceOption = new("--force", "-f")
        {
            DefaultValueFactory = _ => false,
        };
        Command command = new("update")
        {
            searchQueryOption,
            forceOption,
        };
        command.SetAction((parseResult, cancellationToken) => CliEndpoint.ExecuteAsync(
            sp => HandleCommandAsync(sp,
                parseResult.GetRequiredValue(searchQueryOption),
                parseResult.GetRequiredValue(forceOption),
                cancellationToken),
            initializeServices: Initializations.InitializeServices));
        return command;
    }

    private static async Task HandleCommandAsync(IServiceProvider serviceProvider,
        string? searchQueryString, bool force,
        CancellationToken cancellationToken)
    {
        Dictionary<string, StringValues> searchQuery = QueryHelpers.ParseQuery(searchQueryString);
        ISearchIndex? searchIndex = serviceProvider.GetService<ISearchIndex>();
        IExportCache? exportCache = serviceProvider.GetService<IExportCache>();
        foreach (IMediaVendor mediaVendor in serviceProvider.GetServices<IMediaVendor>())
        {
            bool matchesVendorId = SearchQuery.MatchesExactly(searchQuery, ["vendorid"], [mediaVendor.VendorId]);
            if (!matchesVendorId) continue;
            await foreach (string contentId in mediaVendor.UpdateContentAsync(searchQuery, force, cancellationToken).ConfigureAwait(false))
            {
                List<SearchableMediaEntry> searchableMediaEntries = await mediaVendor.EnumerateAsync(contentId, cancellationToken)
                    .ToListAsync(cancellationToken).ConfigureAwait(false);
                if (searchIndex is not null)
                {
                    await searchIndex.AddOrUpdateAsync(searchableMediaEntries, cancellationToken).ConfigureAwait(false);
                }
                if (exportCache is not null)
                {
                    await exportCache.AddOrUpdateCacheAsync(mediaVendor, searchableMediaEntries.Select(e => e.MediaEntry), cancellationToken).ConfigureAwait(false);
                }
            }
        }
    }
}
