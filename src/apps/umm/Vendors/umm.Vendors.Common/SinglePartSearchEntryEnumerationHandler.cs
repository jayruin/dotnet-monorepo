using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using umm.Library;

namespace umm.Vendors.Common;

public sealed class SinglePartSearchEntryEnumerationHandler<TMetadata>
    where TMetadata : ISearchableMetadata, IUniversalizableMediaMetadata
{
    private readonly ISinglePartSearchEntryEnumerationStrategy<TMetadata> _strategy;

    public SinglePartSearchEntryEnumerationHandler(ISinglePartSearchEntryEnumerationStrategy<TMetadata> strategy)
    {
        _strategy = strategy;
    }

    public async IAsyncEnumerable<SearchableMediaEntry> EnumerateAsync([EnumeratorCancellation] CancellationToken cancellationToken)
    {
        await foreach (string contentId in _strategy.EnumerateContentIdsAsync(cancellationToken).ConfigureAwait(false))
        {
            yield return await GetRequiredEntryAsync(contentId, string.Empty, cancellationToken).ConfigureAwait(false);
        }
    }

    public async IAsyncEnumerable<SearchableMediaEntry> EnumerateAsync(string contentId, [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        yield return await GetRequiredEntryAsync(contentId, string.Empty, cancellationToken).ConfigureAwait(false);
    }

    public async Task<SearchableMediaEntry?> GetEntryAsync(string contentId, string partId, CancellationToken cancellationToken)
    {
        if (partId.Length != 0) return null;
        if (!await _strategy.ContainsMetadataAsync(contentId, cancellationToken).ConfigureAwait(false)) return null;
        return await GetRequiredEntryAsync(contentId, partId, cancellationToken).ConfigureAwait(false);
    }

    private async Task<SearchableMediaEntry> GetRequiredEntryAsync(string contentId, string partId, CancellationToken cancellationToken)
    {
        TMetadata metadata = await _strategy.GetMetadataAsync(contentId, cancellationToken).ConfigureAwait(false);
        UniversalMediaMetadata universalMetadata = metadata.Universalize();
        // TODO ToImmutableArrayAsync
        // TODO LINQ
        ImmutableArray<MediaExportTarget> exportTargets = [.. await _strategy.EnumerateExportTargetsAsync(contentId, partId, cancellationToken).ToListAsync(cancellationToken).ConfigureAwait(false)];
        ImmutableSortedSet<string> tags = await _strategy.GetTagsAsync(contentId, cancellationToken).ConfigureAwait(false);
        ImmutableArray<MetadataSearchField> metadataSearchFields = [
            ..metadata.GetSearchFields(),
                new()
                {
                    Aliases = ["tag"],
                    Values = [..tags],
                    ExactMatch = true,
                },
                new()
                {
                    Aliases = ["vendorid"],
                    Values = [_strategy.VendorId],
                    ExactMatch = true,
                },
                new()
                {
                    Aliases = ["contentid"],
                    Values = [contentId],
                    ExactMatch = true,
                },
                new()
                {
                    Aliases = ["partid"],
                    Values = [string.Empty],
                    ExactMatch = true,
                },
            ];
        return new()
        {
            MediaEntry = new()
            {
                VendorId = _strategy.VendorId,
                ContentId = contentId,
                PartId = string.Empty,
                Metadata = universalMetadata,
                ExportTargets = exportTargets,
                Tags = tags,
            },
            MetadataSearchFields = metadataSearchFields,
        };
    }
}
