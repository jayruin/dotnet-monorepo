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
        int updates = 0;
        await foreach (string contentId in _strategy.EnumerateLocalContentIdsAsync(searchQuery, cancellationToken).ConfigureAwait(false))
        {
            total += 1;
            bool metadataChanged = false;
            TMetadata remoteMetadata = await _strategy.GetLatestMetadataAsync(contentId, cancellationToken).ConfigureAwait(false);
            TMetadata? localMetadata = await _strategy.MetadataStorage.ContainsAsync(_strategy.VendorId, contentId, _strategy.MetadataKey, cancellationToken).ConfigureAwait(false)
                ? await _strategy.MetadataStorage.GetAsync<TMetadata>(_strategy.VendorId, contentId, _strategy.MetadataKey, cancellationToken).ConfigureAwait(false)
                : default;
            bool shouldUpdate = force || localMetadata is null || _strategy.ShouldUpdate(remoteMetadata, localMetadata);
            if (shouldUpdate)
            {
                _strategy.Logger.LogUpdated(_strategy.VendorId, remoteMetadata.ContentId, remoteMetadata.LastUpdated ?? string.Empty);
                updates += 1;
                if (localMetadata is not null)
                {
                    IReadOnlyCollection<MetadataPropertyChange> metadataPropertyChanges = remoteMetadata.GetChanges(localMetadata);
                    metadataChanged = metadataPropertyChanges.Count > 0;
                    foreach (MetadataPropertyChange metadataPropertyChange in metadataPropertyChanges)
                    {
                        _strategy.Logger.LogMetadataChanged(_strategy.VendorId, remoteMetadata.ContentId, metadataPropertyChange);
                    }
                }
                else
                {
                    metadataChanged = true;
                }
            }
            else
            {
                _strategy.Logger.LogSkippedUpdate(_strategy.VendorId, remoteMetadata.ContentId);
            }
            if (metadataChanged)
            {
                await _strategy.MetadataStorage.SaveAsync(_strategy.VendorId, remoteMetadata.ContentId, _strategy.MetadataKey, remoteMetadata, cancellationToken).ConfigureAwait(false);
                yield return remoteMetadata.ContentId;
            }
        }
        _strategy.Logger.LogUpdateSummary(_strategy.VendorId, updates, total);
    }
}
