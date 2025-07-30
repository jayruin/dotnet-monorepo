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
        int updates = 0;
        await foreach (TMetadata remoteMetadata in _strategy.EnumerateRemoteAsync(cancellationToken).ConfigureAwait(false))
        {
            total += 1;
            bool matches = SearchQuery.Matches(searchQuery, remoteMetadata.GetSearchFields());
            if (!matches)
            {
                _strategy.Logger.LogDoesNotMatchSearchQuery(_strategy.VendorId, remoteMetadata.ContentId);
                continue;
            }
            bool updated = false;
            bool metadataChanged = false;
            if (force || !await _strategy.MetadataStorage.ContainsAsync(_strategy.VendorId, remoteMetadata.ContentId, _strategy.MetadataKey, cancellationToken).ConfigureAwait(false))
            {
                await _strategy.PerformUpdateAsync(remoteMetadata, cancellationToken).ConfigureAwait(false);
                updated = true;
                metadataChanged = true;
            }
            else
            {
                TMetadata localMetadata = await _strategy.MetadataStorage.GetAsync<TMetadata>(_strategy.VendorId, remoteMetadata.ContentId, _strategy.MetadataKey, cancellationToken).ConfigureAwait(false);
                updated = await _strategy.AttemptPerformUpdateAsync(remoteMetadata, localMetadata, cancellationToken).ConfigureAwait(false);
                IReadOnlyCollection<MetadataPropertyChange> metadataPropertyChanges = remoteMetadata.GetChanges(localMetadata);
                metadataChanged = metadataPropertyChanges.Count > 0;
                foreach (MetadataPropertyChange metadataPropertyChange in metadataPropertyChanges)
                {
                    _strategy.Logger.LogMetadataChanged(_strategy.VendorId, remoteMetadata.ContentId, metadataPropertyChange);
                }
            }
            if (updated)
            {
                _strategy.Logger.LogUpdated(_strategy.VendorId, remoteMetadata.ContentId, remoteMetadata.LastUpdated ?? string.Empty);
                updates += 1;
            }
            else
            {
                _strategy.Logger.LogSkippedUpdate(_strategy.VendorId, remoteMetadata.ContentId);
            }
            if (metadataChanged)
            {
                await _strategy.MetadataStorage.SaveAsync(_strategy.VendorId, remoteMetadata.ContentId, _strategy.MetadataKey, remoteMetadata, cancellationToken).ConfigureAwait(false);
            }
            yield return remoteMetadata.ContentId;
        }
        _strategy.Logger.LogUpdateSummary(_strategy.VendorId, updates, total);
    }
}
