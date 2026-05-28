using Apps;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.CommandLine;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using umm.ExportCache;
using umm.HashCache;
using umm.Library;
using umm.SearchIndex;
using umm.Vendors.Abstractions;

namespace umm.App;

internal static class IndexCacheCli
{
    public static Command CreateCommand()
    {
        return new("ic", "Manage indexes and caches.")
        {
            CreateRebuildCommand(),
            CreateUpdateCommand(),
        };
    }

    private static Command CreateRebuildCommand()
    {
        Argument<IEnumerable<string>> vendorIdsArgument = new("vendorIds")
        {
            DefaultValueFactory = _ => [],
        };
        Option<bool> searchIndexOption = CreateSearchQueryOption();
        Option<bool> exportCacheOption = CreateExportCacheOption();
        Option<bool> hashCacheOption = CreateHashCacheOption();
        Command command = new("rebuild")
        {
            vendorIdsArgument,
            searchIndexOption,
            exportCacheOption,
            hashCacheOption,
        };
        command.SetAction((parseResult, cancellationToken) => CliEndpoint.ExecuteAsync(
            sp => HandleRebuildCommandAsync(sp,
                parseResult.GetRequiredValue(vendorIdsArgument),
                parseResult.GetRequiredValue(searchIndexOption),
                parseResult.GetRequiredValue(exportCacheOption),
                parseResult.GetRequiredValue(hashCacheOption),
                cancellationToken),
            initializeServices: Initializations.InitializeServices));
        return command;
    }

    private static async Task HandleRebuildCommandAsync(IServiceProvider serviceProvider,
        IEnumerable<string> vendorIds, bool handleSearchIndex, bool handleExportCache, bool handleHashCache,
        CancellationToken cancellationToken)
    {
        if (!handleSearchIndex && !handleExportCache && !handleHashCache)
        {
            return;
        }

        HashSet<string> vendorIdsSet = [.. vendorIds];

        ISearchIndex? searchIndex = serviceProvider.GetService<ISearchIndex>();
        IExportCache? exportCache = serviceProvider.GetService<IExportCache>();
        IHashCache? hashCache = serviceProvider.GetService<IHashCache>();
        IMultiHashProvider? multiHashProvider = serviceProvider.GetService<IMultiHashProvider>();
        if (handleExportCache && exportCache is not null && vendorIdsSet.Count == 0)
        {
            await exportCache.ResetAsync(cancellationToken).ConfigureAwait(false);
        }
        if (handleHashCache && hashCache is not null && multiHashProvider is not null && vendorIdsSet.Count == 0)
        {
            await hashCache.ResetAsync(cancellationToken).ConfigureAwait(false);
        }
        foreach (IMediaVendor mediaVendor in serviceProvider.GetServices<IMediaVendor>())
        {
            if (vendorIdsSet.Count > 0 && !vendorIdsSet.Contains(mediaVendor.VendorId)) continue;

            List<SearchableMediaEntry> searchableMediaEntries = await mediaVendor.EnumerateAsync(cancellationToken)
                .ToListAsync(cancellationToken).ConfigureAwait(false);

            if (handleSearchIndex && searchIndex is not null)
            {
                await searchIndex.ClearAsync(mediaVendor.VendorId, cancellationToken).ConfigureAwait(false);
                await searchIndex.AddOrUpdateAsync(
                    searchableMediaEntries,
                    cancellationToken
                ).ConfigureAwait(false);
            }

            if (handleExportCache && exportCache is not null)
            {
                await exportCache.ClearAsync(mediaVendor.VendorId, cancellationToken).ConfigureAwait(false);
                await exportCache.AddOrUpdateCacheAsync(
                    mediaVendor,
                    searchableMediaEntries.Select(e => e.MediaEntry),
                    cancellationToken
                ).ConfigureAwait(false);
            }

            if (handleHashCache && hashCache is not null && multiHashProvider is not null)
            {
                await hashCache.ClearAsync(mediaVendor.VendorId, cancellationToken).ConfigureAwait(false);
                await hashCache.AddOrUpdateCacheAsync(
                    mediaVendor,
                    searchableMediaEntries.Select(e => e.MediaEntry),
                    multiHashProvider,
                    cancellationToken
                ).ConfigureAwait(false);
            }
        }
    }

    private static Command CreateUpdateCommand()
    {
        Argument<string> vendorIdArgument = new("vendorId");
        Argument<IEnumerable<string>> contentIdsArgument = new("contentIds");
        Option<bool> searchIndexOption = CreateSearchQueryOption();
        Option<bool> exportCacheOption = CreateExportCacheOption();
        Option<bool> hashCacheOption = CreateHashCacheOption();
        Command command = new("update")
        {
            vendorIdArgument,
            contentIdsArgument,
            searchIndexOption,
            exportCacheOption,
            hashCacheOption,
        };
        command.SetAction((parseResult, cancellationToken) => CliEndpoint.ExecuteAsync(
            sp => HandleUpdateCommandAsync(sp,
                parseResult.GetRequiredValue(vendorIdArgument),
                parseResult.GetRequiredValue(contentIdsArgument),
                parseResult.GetRequiredValue(searchIndexOption),
                parseResult.GetRequiredValue(exportCacheOption),
                parseResult.GetRequiredValue(hashCacheOption),
                cancellationToken),
            initializeServices: Initializations.InitializeServices));
        return command;
    }

    private static async Task HandleUpdateCommandAsync(IServiceProvider serviceProvider,
        string vendorId, IEnumerable<string> contentIds, bool handleSearchIndex, bool handleExportCache, bool handleHashCache,
        CancellationToken cancellationToken)
    {
        if (!handleSearchIndex && !handleExportCache && !handleHashCache)
        {
            return;
        }

        IMediaVendor mediaVendor = serviceProvider.GetServices<IMediaVendor>().First(m => m.VendorId == vendorId);
        List<SearchableMediaEntry> searchableMediaEntries = await contentIds.ToAsyncEnumerable()
            .SelectMany(contentId => mediaVendor.EnumerateAsync(contentId, cancellationToken))
            .ToListAsync(cancellationToken).ConfigureAwait(false);

        if (handleSearchIndex)
        {
            ISearchIndex searchIndex = serviceProvider.GetRequiredService<ISearchIndex>();
            await searchIndex.AddOrUpdateAsync(
                searchableMediaEntries,
                cancellationToken
            ).ConfigureAwait(false);
        }

        if (handleExportCache)
        {
            IExportCache exportCache = serviceProvider.GetRequiredService<IExportCache>();
            await exportCache.AddOrUpdateCacheAsync(
                mediaVendor,
                searchableMediaEntries.Select(e => e.MediaEntry),
                cancellationToken
            ).ConfigureAwait(false);
        }

        if (handleHashCache)
        {
            IHashCache hashCache = serviceProvider.GetRequiredService<IHashCache>();
            IMultiHashProvider multiHashProvider = serviceProvider.GetRequiredService<IMultiHashProvider>();
            await hashCache.AddOrUpdateCacheAsync(
                mediaVendor,
                searchableMediaEntries.Select(e => e.MediaEntry),
                multiHashProvider,
                cancellationToken
            ).ConfigureAwait(false);
        }
    }

    private static Option<bool> CreateSearchQueryOption() => new("--search-index", "-s")
    {
        DefaultValueFactory = _ => false,
    };

    private static Option<bool> CreateExportCacheOption() => new("--export-cache", "-e")
    {
        DefaultValueFactory = _ => false,
    };

    private static Option<bool> CreateHashCacheOption() => new("--hash-cache", "-a")
    {
        DefaultValueFactory = _ => false,
    };
}
