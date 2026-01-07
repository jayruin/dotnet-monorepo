using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using umm.Library;

namespace umm.Storages.Tags;

public sealed class CompositeTagsStorage : ITagsStorage
{
    private readonly IReadOnlyCollection<ITagsStorage> _tagsStorages;
    private readonly ILogger<CompositeTagsStorage> _logger;

    public CompositeTagsStorage(IReadOnlyCollection<ITagsStorage> tagsStorages, ILogger<CompositeTagsStorage> logger)
    {
        _tagsStorages = tagsStorages;
        _logger = logger;
    }

    public bool Supports(string vendorId) => _tagsStorages.Any(s => s.Supports(vendorId));

    public Task<bool> ContainsAsync(MediaMainId id, CancellationToken cancellationToken = default)
        => _tagsStorages.ToAsyncEnumerable().AnyAsync((s, ct) => new ValueTask<bool>(s.ContainsAsync(id, ct)), cancellationToken).AsTask();

    public IAsyncEnumerable<MediaMainId> EnumerateContentAsync(CancellationToken cancellationToken = default)
        => _tagsStorages.ToAsyncEnumerable().SelectMany(s => s.EnumerateContentAsync(cancellationToken));

    public async Task SaveAsync(MediaMainId id, IReadOnlySet<string> tags, CancellationToken cancellationToken = default)
    {
        ITagsStorage tagsStorage = await _tagsStorages.ToAsyncEnumerable().FirstOrDefaultAsync((s, ct) => new ValueTask<bool>(s.ContainsAsync(id, ct)), cancellationToken).ConfigureAwait(false)
            ?? _tagsStorages.First(s => s.Supports(id.VendorId));
        _logger.LogSavingTags(id.VendorId, id.ContentId);
        await tagsStorage.SaveAsync(id, tags, cancellationToken).ConfigureAwait(false);
    }

    public async Task<ImmutableSortedSet<string>> GetAsync(MediaMainId id, CancellationToken cancellationToken = default)
    {
        foreach (ITagsStorage tagsStorage in _tagsStorages)
        {
            if (!await tagsStorage.ContainsAsync(id, cancellationToken).ConfigureAwait(false)) continue;
            _logger.LogGettingTags(id.VendorId, id.ContentId);
            return await tagsStorage.GetAsync(id, cancellationToken).ConfigureAwait(false);
        }
        throw new InvalidOperationException($"Tags for {id.ToCombinedString()} not found.");
    }
}
