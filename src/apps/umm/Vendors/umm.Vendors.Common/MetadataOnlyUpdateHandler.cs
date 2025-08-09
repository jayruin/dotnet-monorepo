using Microsoft.Extensions.Primitives;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using umm.Storages.Metadata;

namespace umm.Vendors.Common;

public sealed class MetadataOnlyUpdateHandler<TMetadata>
    where TMetadata : ISerializableMetadata<TMetadata>, ISearchableMetadata, IUpdatableMetadata<TMetadata>
{
    private readonly IMetadataOnlyUpdateHandlerStrategy<TMetadata> _strategy;

    public MetadataOnlyUpdateHandler(IMetadataOnlyUpdateHandlerStrategy<TMetadata> strategy)
    {
        _strategy = strategy;
    }

    public async IAsyncEnumerable<string> UpdateContentAsync(IReadOnlyDictionary<string, StringValues> searchQuery, bool force, [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        int total = 0;
        int metadataUpdates = 0;
        await foreach (string contentId in _strategy.EnumerateLocalContentIdsAsync(searchQuery, cancellationToken).ConfigureAwait(false))
        {
            total += 1;
            bool metadataChanged = false;
            TMetadata remoteMetadata = await _strategy.GetLatestMetadataAsync(contentId, cancellationToken).ConfigureAwait(false);
            TMetadata? localMetadata = await _strategy.VendorContext.MetadataStorage.ContainsAsync(_strategy.VendorContext.VendorId, contentId, _strategy.MetadataKey, cancellationToken).ConfigureAwait(false)
                ? await _strategy.VendorContext.MetadataStorage.GetAsync<TMetadata>(_strategy.VendorContext.VendorId, contentId, _strategy.MetadataKey, cancellationToken).ConfigureAwait(false)
                : default;
            bool shouldUpdate = force || localMetadata is null || _strategy.ShouldUpdate(remoteMetadata, localMetadata);
            if (shouldUpdate)
            {
                if (localMetadata is not null)
                {
                    IReadOnlyCollection<MetadataPropertyChange> metadataPropertyChanges = remoteMetadata.GetChanges(localMetadata);
                    metadataChanged = metadataPropertyChanges.Count > 0;
                    foreach (MetadataPropertyChange metadataPropertyChange in metadataPropertyChanges)
                    {
                        _strategy.VendorContext.Logger.LogMetadataPropertyUpdated(_strategy.VendorContext.VendorId, remoteMetadata.ContentId, metadataPropertyChange);
                    }
                }
                else
                {
                    metadataChanged = true;
                }
            }
            if (metadataChanged)
            {
                await _strategy.VendorContext.MetadataStorage.SaveAsync(_strategy.VendorContext.VendorId, remoteMetadata.ContentId, _strategy.MetadataKey, remoteMetadata, cancellationToken).ConfigureAwait(false);
                _strategy.VendorContext.Logger.LogMetadataUpdated(_strategy.VendorContext.VendorId, remoteMetadata.ContentId, remoteMetadata.LastUpdated ?? string.Empty);
                metadataUpdates += 1;
                yield return remoteMetadata.ContentId;
            }
            else
            {
                _strategy.VendorContext.Logger.LogSkippedMetadataUpdate(_strategy.VendorContext.VendorId, remoteMetadata.ContentId);
            }
        }
        _strategy.VendorContext.Logger.LogMetadataUpdateSummary(_strategy.VendorContext.VendorId, metadataUpdates, total);
    }
}
