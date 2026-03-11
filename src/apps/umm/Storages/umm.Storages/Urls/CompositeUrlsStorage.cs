using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using umm.Library;

namespace umm.Storages.Urls;

public sealed class CompositeUrlsStorage : IUrlsStorage
{
    private readonly IReadOnlyCollection<IUrlsStorage> _urlsStorages;
    private readonly ILogger<CompositeUrlsStorage> _logger;

    public CompositeUrlsStorage(IReadOnlyCollection<IUrlsStorage> urlsStorages, ILogger<CompositeUrlsStorage> logger)
    {
        _urlsStorages = urlsStorages;
        _logger = logger;
    }

    public bool Supports(string vendorId) => _urlsStorages.Any(s => s.Supports(vendorId));

    public Task<bool> ContainsAsync(MediaMainId id, CancellationToken cancellationToken = default)
        => _urlsStorages.ToAsyncEnumerable().AnyAsync((s, ct) => new ValueTask<bool>(s.ContainsAsync(id, ct)), cancellationToken).AsTask();

    public IAsyncEnumerable<MediaMainId> EnumerateContentAsync(CancellationToken cancellationToken = default)
        => _urlsStorages.ToAsyncEnumerable().SelectMany(s => s.EnumerateContentAsync(cancellationToken));

    public async Task SaveAsync(MediaFullId id, IReadOnlyList<string> urls, CancellationToken cancellationToken = default)
    {
        IUrlsStorage urlsStorage = await _urlsStorages.ToAsyncEnumerable().FirstOrDefaultAsync((s, ct) => new ValueTask<bool>(s.ContainsAsync(id.ToMainId(), ct)), cancellationToken).ConfigureAwait(false)
            ?? _urlsStorages.First(s => s.Supports(id.VendorId));
        _logger.LogSavingUrls(id.VendorId, id.ContentId, id.PartId);
        await urlsStorage.SaveAsync(id, urls, cancellationToken).ConfigureAwait(false);
    }

    public async Task<ImmutableArray<string>> GetAsync(MediaFullId id, CancellationToken cancellationToken = default)
    {
        foreach (IUrlsStorage urlsStorage in _urlsStorages)
        {
            if (!await urlsStorage.ContainsAsync(id.ToMainId(), cancellationToken).ConfigureAwait(false)) continue;
            _logger.LogGettingUrls(id.VendorId, id.ContentId, id.PartId);
            return await urlsStorage.GetAsync(id, cancellationToken).ConfigureAwait(false);
        }
        throw new InvalidOperationException($"Urls for {id.ToCombinedString()} not found.");
    }
}
