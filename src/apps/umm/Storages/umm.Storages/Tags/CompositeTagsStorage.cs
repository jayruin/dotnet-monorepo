using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

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

    // TODO LINQ
    public Task<bool> ContainsAsync(string vendorId, string contentId, CancellationToken cancellationToken = default)
        => _tagsStorages.ToAsyncEnumerable().AnyAwaitAsync(s => new ValueTask<bool>(s.ContainsAsync(vendorId, contentId, cancellationToken)), cancellationToken).AsTask();

    // TODO LINQ
    public IAsyncEnumerable<(string VendorId, string ContentId)> EnumerateContentAsync(CancellationToken cancellationToken = default)
        => _tagsStorages.ToAsyncEnumerable().SelectMany(s => s.EnumerateContentAsync(cancellationToken));

    // TODO LINQ
    public async Task SaveAsync(string vendorId, string contentId, IReadOnlySet<string> tags, CancellationToken cancellationToken = default)
    {
        ITagsStorage tagsStorage = await _tagsStorages.ToAsyncEnumerable().FirstOrDefaultAwaitAsync(s => new ValueTask<bool>(s.ContainsAsync(vendorId, contentId, cancellationToken)), cancellationToken).ConfigureAwait(false)
            ?? _tagsStorages.First(s => s.Supports(vendorId));
        _logger.LogSavingTags(vendorId, contentId);
        await tagsStorage.SaveAsync(vendorId, contentId, tags, cancellationToken).ConfigureAwait(false);
    }

    // TODO LINQ
    public async Task<ImmutableSortedSet<string>> GetAsync(string vendorId, string contentId, CancellationToken cancellationToken = default)
    {
        foreach (ITagsStorage tagsStorage in _tagsStorages)
        {
            if (!await tagsStorage.ContainsAsync(vendorId, contentId, cancellationToken).ConfigureAwait(false)) continue;
            _logger.LogGettingTags(vendorId, contentId);
            return await tagsStorage.GetAsync(vendorId, contentId, cancellationToken).ConfigureAwait(false);
        }
        throw new InvalidOperationException($"Tags for {vendorId} {contentId} not found.");
    }
}
