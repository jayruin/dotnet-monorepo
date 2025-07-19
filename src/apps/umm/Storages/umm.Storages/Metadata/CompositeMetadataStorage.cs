using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace umm.Storages.Metadata;

public sealed class CompositeMetadataStorage : IMetadataStorage
{
    private readonly IReadOnlyCollection<IMetadataStorage> _metadataStorages;
    private readonly ILogger<CompositeMetadataStorage> _logger;

    public CompositeMetadataStorage(IReadOnlyCollection<IMetadataStorage> metadataStorages, ILogger<CompositeMetadataStorage> logger)
    {
        _metadataStorages = metadataStorages;
        _logger = logger;
    }

    public bool Supports(string vendorId) => _metadataStorages.Any(s => s.Supports(vendorId));

    // TODO LINQ
    public Task<bool> ContainsAsync(string vendorId, string contentId, CancellationToken cancellationToken = default)
        => _metadataStorages.ToAsyncEnumerable().AnyAwaitAsync(s => new ValueTask<bool>(s.ContainsAsync(vendorId, contentId, cancellationToken)), cancellationToken).AsTask();

    // TODO LINQ
    public Task<bool> ContainsAsync(string vendorId, string contentId, string key, CancellationToken cancellationToken = default)
        => _metadataStorages.ToAsyncEnumerable().AnyAwaitAsync(s => new ValueTask<bool>(s.ContainsAsync(vendorId, contentId, key, cancellationToken)), cancellationToken).AsTask();

    // TODO LINQ
    public IAsyncEnumerable<(string VendorId, string ContentId)> EnumerateContentAsync(CancellationToken cancellationToken = default)
        => _metadataStorages.ToAsyncEnumerable().SelectMany(s => s.EnumerateContentAsync(cancellationToken));

    // TODO LINQ
    public async Task SaveAsync<TMetadata>(string vendorId, string contentId, string key, TMetadata metadata, CancellationToken cancellationToken = default)
        where TMetadata : ISerializableMetadata<TMetadata>
    {
        IMetadataStorage metadataStorage = await _metadataStorages.ToAsyncEnumerable().FirstOrDefaultAwaitAsync(s => new ValueTask<bool>(s.ContainsAsync(vendorId, contentId, cancellationToken)), cancellationToken).ConfigureAwait(false)
            ?? _metadataStorages.First(s => s.Supports(vendorId));
        _logger.LogSavingMetadata(vendorId, contentId, key, typeof(TMetadata).Name);
        await metadataStorage.SaveAsync(vendorId, contentId, key, metadata, cancellationToken).ConfigureAwait(false);
    }

    public async Task<TMetadata> GetAsync<TMetadata>(string vendorId, string contentId, string key, CancellationToken cancellationToken = default)
        where TMetadata : ISerializableMetadata<TMetadata>
    {
        foreach (IMetadataStorage metadataStorage in _metadataStorages)
        {
            if (!await metadataStorage.ContainsAsync(vendorId, contentId, key, cancellationToken).ConfigureAwait(false)) continue;
            _logger.LogGettingMetadata(vendorId, contentId, key, typeof(TMetadata).Name);
            return await metadataStorage.GetAsync<TMetadata>(vendorId, contentId, key, cancellationToken).ConfigureAwait(false);
        }
        throw new InvalidOperationException($"Metadata for {vendorId} {contentId} {key} not found.");
    }

    public async Task DeleteAsync(string vendorId, string contentId, string key, CancellationToken cancellationToken = default)
    {
        foreach (IMetadataStorage metadataStorage in _metadataStorages)
        {
            if (!await metadataStorage.ContainsAsync(vendorId, contentId, key, cancellationToken).ConfigureAwait(false)) continue;
            _logger.LogDeletingMetadata(vendorId, contentId, key);
            await metadataStorage.DeleteAsync(vendorId, contentId, key, cancellationToken).ConfigureAwait(false);
        }
    }
}
