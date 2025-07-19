using FileStorage;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using umm.Library;
using umm.Vendors.Abstractions;

namespace umm.ExportCache;

public static class ExportCacheExtensions
{
    public static async Task AddOrUpdateCacheAsync(this IExportCache exportCache, IMediaVendor mediaVendor, IAsyncEnumerable<MediaEntry> entries, CancellationToken cancellationToken = default)
    {
        await foreach (MediaEntry entry in entries.ConfigureAwait(false))
        {
            await exportCache.AddOrUpdateCacheAsync(mediaVendor, entry, cancellationToken).ConfigureAwait(false);
        }
    }

    public static async Task AddOrUpdateCacheAsync(this IExportCache exportCache, IMediaVendor mediaVendor, IEnumerable<MediaEntry> entries, CancellationToken cancellationToken = default)
    {
        foreach (MediaEntry entry in entries)
        {
            await exportCache.AddOrUpdateCacheAsync(mediaVendor, entry, cancellationToken).ConfigureAwait(false);
        }
    }

    private static async Task AddOrUpdateCacheAsync(this IExportCache exportCache, IMediaVendor mediaVendor, MediaEntry entry, CancellationToken cancellationToken = default)
    {
        foreach (MediaExportTarget exportTarget in entry.ExportTargets)
        {
            if (exportTarget.SupportsFile
                && await exportCache.CanHandleFileAsync(
                    entry.VendorId,
                    entry.ContentId,
                    entry.PartId,
                    exportTarget.MediaType,
                    cancellationToken).ConfigureAwait(false))
            {
                Stream stream = await exportCache.GetStreamForCachingAsync(
                    entry.VendorId,
                    entry.ContentId,
                    entry.PartId,
                    exportTarget.MediaType,
                    cancellationToken).ConfigureAwait(false);
                await using (stream.ConfigureAwait(false))
                {
                    await mediaVendor.ExportAsync(
                        entry.ContentId,
                        entry.PartId,
                        exportTarget.MediaType,
                        stream,
                        cancellationToken).ConfigureAwait(false);
                }
            }
            if (exportTarget.SupportsDirectory
                && await exportCache.CanHandleDirectoryAsync(
                    entry.VendorId,
                    entry.ContentId,
                    entry.PartId,
                    exportTarget.MediaType,
                    cancellationToken).ConfigureAwait(false))
            {
                IDirectory directory = await exportCache.GetDirectoryForCachingAsync(
                    entry.VendorId,
                    entry.ContentId,
                    entry.PartId,
                    exportTarget.MediaType,
                    cancellationToken).ConfigureAwait(false);
                await mediaVendor.ExportAsync(
                    entry.ContentId,
                    entry.PartId,
                    exportTarget.MediaType,
                    directory,
                    cancellationToken).ConfigureAwait(false);
            }
        }
    }
}
