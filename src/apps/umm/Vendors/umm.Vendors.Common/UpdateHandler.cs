using Microsoft.Extensions.Primitives;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using umm.Library;
using umm.Storages.Metadata;

namespace umm.Vendors.Common;

public sealed class UpdateHandler<TMetadata>
    where TMetadata : ISerializableMetadata<TMetadata>, ISearchableMetadata, IUpdatableMetadata<TMetadata>
{
    private readonly IUpdateHandlerStrategy<TMetadata> _strategy;

    public UpdateHandler(IUpdateHandlerStrategy<TMetadata> strategy)
    {
        _strategy = strategy;
    }

    public async IAsyncEnumerable<string> UpdateContentAsync(IReadOnlyDictionary<string, StringValues> searchQuery, bool force, [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        int total = 0;
        int contentUpdates = 0;
        int metadataUpdates = 0;
        await foreach (TMetadata remoteMetadata in _strategy.EnumerateRemoteAsync(cancellationToken).ConfigureAwait(false))
        {
            MediaMainId id = new(_strategy.VendorContext.VendorId, remoteMetadata.ContentId);
            total += 1;
            bool matches = SearchQuery.Matches(searchQuery, remoteMetadata.GetSearchFields());
            if (!matches)
            {
                _strategy.VendorContext.Logger.LogDoesNotMatchSearchQuery(id.VendorId, id.ContentId);
                continue;
            }
            bool updated = false;
            bool metadataUpdated = false;
            if (force || !await _strategy.VendorContext.MetadataStorage.ContainsAsync(id, _strategy.MetadataKey, cancellationToken).ConfigureAwait(false))
            {
                await _strategy.PerformUpdateAsync(remoteMetadata, cancellationToken).ConfigureAwait(false);
                updated = true;
                metadataUpdated = true;
            }
            else
            {
                TMetadata localMetadata = await _strategy.VendorContext.MetadataStorage.GetAsync<TMetadata>(id, _strategy.MetadataKey, cancellationToken).ConfigureAwait(false);
                updated = await _strategy.AttemptPerformUpdateAsync(remoteMetadata, localMetadata, cancellationToken).ConfigureAwait(false);
                IReadOnlyCollection<MetadataPropertyChange> metadataPropertyChanges = remoteMetadata.GetChanges(localMetadata);
                metadataUpdated = metadataPropertyChanges.Count > 0;
                foreach (MetadataPropertyChange metadataPropertyChange in metadataPropertyChanges)
                {
                    _strategy.VendorContext.Logger.LogMetadataPropertyUpdated(id.VendorId, id.ContentId, metadataPropertyChange);
                }
            }
            if (updated)
            {
                _strategy.VendorContext.Logger.LogContentUpdated(id.VendorId, id.ContentId, remoteMetadata.LastUpdated ?? string.Empty);
                contentUpdates += 1;
            }
            else
            {
                _strategy.VendorContext.Logger.LogSkippedContentUpdate(id.VendorId, id.ContentId);
            }
            if (metadataUpdated)
            {
                await _strategy.VendorContext.MetadataStorage.SaveAsync(id, _strategy.MetadataKey, remoteMetadata, cancellationToken).ConfigureAwait(false);
                _strategy.VendorContext.Logger.LogMetadataUpdated(id.VendorId, id.ContentId, remoteMetadata.LastUpdated ?? string.Empty);
                metadataUpdates += 1;
            }
            else
            {
                _strategy.VendorContext.Logger.LogSkippedMetadataUpdate(id.VendorId, id.ContentId);
            }
            if (updated || metadataUpdated)
            {
                yield return id.ContentId;
            }
        }
        _strategy.VendorContext.Logger.LogContentUpdateSummary(_strategy.VendorContext.VendorId, contentUpdates, total);
        _strategy.VendorContext.Logger.LogMetadataUpdateSummary(_strategy.VendorContext.VendorId, metadataUpdates, total);
    }
}
