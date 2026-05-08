using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using umm.Library;
using umm.Vendors.Abstractions;

namespace umm.HashCache;

public static class HashCacheExtensions
{
    extension(IHashCache hashCache)
    {
        public async Task AddOrUpdateCacheAsync(IMediaVendor mediaVendor, IAsyncEnumerable<MediaEntry> entries, IMultiHashProvider multiHashProvider, CancellationToken cancellationToken = default)
        {
            if (multiHashProvider.SupportedHashFunctionNames.Count == 0) return;
            await foreach (MediaEntry entry in entries.ConfigureAwait(false))
            {
                await hashCache.AddOrUpdateCacheAsync(mediaVendor, entry, multiHashProvider, cancellationToken).ConfigureAwait(false);
            }
        }

        public async Task AddOrUpdateCacheAsync(IMediaVendor mediaVendor, IEnumerable<MediaEntry> entries, IMultiHashProvider multiHashProvider, CancellationToken cancellationToken = default)
        {
            if (multiHashProvider.SupportedHashFunctionNames.Count == 0) return;
            foreach (MediaEntry entry in entries)
            {
                await hashCache.AddOrUpdateCacheAsync(mediaVendor, entry, multiHashProvider, cancellationToken).ConfigureAwait(false);
            }
        }

        private async Task AddOrUpdateCacheAsync(IMediaVendor mediaVendor, MediaEntry entry, IMultiHashProvider multiHashProvider, CancellationToken cancellationToken = default)
        {
            foreach (MediaExportTarget exportTarget in entry.ExportTargets)
            {
                if (!exportTarget.SupportsFile
                    || !await hashCache.CanHandleAsync(exportTarget.MediaType, cancellationToken).ConfigureAwait(false))
                {
                    continue;
                }
                // Streaming the export by writing to a non-seekable stream produces a file with a different hash
                //Pipe pipe = new();
                //Task exportTask = Task.Run(async () =>
                //{
                //    Stream writeStream = pipe.Writer.AsStream();
                //    await using (writeStream.ConfigureAwait(false))
                //    {
                //        await mediaVendor.ExportAsync(entry.Id.ContentId, entry.Id.PartId, exportTarget.ExportId, writeStream).ConfigureAwait(false);
                //    }
                //}, cancellationToken);
                //Stream readStream = pipe.Reader.AsStream();
                //ImmutableSortedDictionary<string, string> hashes;
                //await using (readStream.ConfigureAwait(false))
                //{
                //    hashes = await multiHashProvider.ComputeHashesAsync(readStream, cancellationToken).ConfigureAwait(false);
                //}
                //await exportTask.ConfigureAwait(false);
                MemoryStream memoryStream = new();
                await using ConfiguredAsyncDisposable configuredMemoryStream = memoryStream.ConfigureAwait(false);
                await mediaVendor.ExportAsync(entry.Id.ContentId, entry.Id.PartId, exportTarget.ExportId, memoryStream, cancellationToken).ConfigureAwait(false);
                memoryStream.Seek(0, SeekOrigin.Begin);
                ImmutableSortedDictionary<string, string> hashes = await multiHashProvider.ComputeHashesAsync(memoryStream, cancellationToken).ConfigureAwait(false);
                await hashCache.SetHashesAsync(entry.Id, exportTarget.ExportId, hashes, cancellationToken).ConfigureAwait(false);
            }
        }
    }
}
