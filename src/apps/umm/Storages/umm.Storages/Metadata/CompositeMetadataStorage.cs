using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using umm.Library;

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

    public Task<bool> ContainsAsync(MediaMainId id, CancellationToken cancellationToken = default)
        => _metadataStorages.ToAsyncEnumerable().AnyAsync((s, ct) => new ValueTask<bool>(s.ContainsAsync(id, ct)), cancellationToken).AsTask();

    public Task<bool> ContainsAsync(MediaMainId id, string key, CancellationToken cancellationToken = default)
        => _metadataStorages.ToAsyncEnumerable().AnyAsync((s, ct) => new ValueTask<bool>(s.ContainsAsync(id, key, ct)), cancellationToken).AsTask();

    public IAsyncEnumerable<MediaMainId> EnumerateContentAsync(CancellationToken cancellationToken = default)
        => _metadataStorages.ToAsyncEnumerable().SelectMany(s => s.EnumerateContentAsync(cancellationToken));

    public async Task SaveAsync<TMetadata>(MediaMainId id, string key, TMetadata metadata, CancellationToken cancellationToken = default)
        where TMetadata : ISerializableMetadata<TMetadata>
    {
        IMetadataStorage metadataStorage = await _metadataStorages.ToAsyncEnumerable().FirstOrDefaultAsync((s, ct) => new ValueTask<bool>(s.ContainsAsync(id, ct)), cancellationToken).ConfigureAwait(false)
            ?? _metadataStorages.First(s => s.Supports(id.VendorId));
        _logger.LogSavingMetadata(id.VendorId, id.ContentId, key, typeof(TMetadata).Name);
        await metadataStorage.SaveAsync(id, key, metadata, cancellationToken).ConfigureAwait(false);
    }

    public async Task<TMetadata> GetAsync<TMetadata>(MediaMainId id, string key, CancellationToken cancellationToken = default)
        where TMetadata : ISerializableMetadata<TMetadata>
    {
        foreach (IMetadataStorage metadataStorage in _metadataStorages)
        {
            if (!await metadataStorage.ContainsAsync(id, key, cancellationToken).ConfigureAwait(false)) continue;
            _logger.LogGettingMetadata(id.VendorId, id.ContentId, key, typeof(TMetadata).Name);
            return await metadataStorage.GetAsync<TMetadata>(id, key, cancellationToken).ConfigureAwait(false);
        }
        throw new InvalidOperationException($"Metadata for {id.ToCombinedString()} {key} not found.");
    }

    public async Task DeleteAsync(MediaMainId id, string key, CancellationToken cancellationToken = default)
    {
        foreach (IMetadataStorage metadataStorage in _metadataStorages)
        {
            if (!await metadataStorage.ContainsAsync(id, key, cancellationToken).ConfigureAwait(false)) continue;
            _logger.LogDeletingMetadata(id.VendorId, id.ContentId, key);
            await metadataStorage.DeleteAsync(id, key, cancellationToken).ConfigureAwait(false);
        }
    }
}
