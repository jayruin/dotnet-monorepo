using System.Collections.Generic;
using System.Collections.Immutable;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using umm.Library;
using Utils;

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
        MediaFullId mediaFullId = new(_strategy.VendorContext.VendorId, contentId, string.Empty);
        TMetadata metadata = await _strategy.GetMetadataAsync(contentId, cancellationToken).ConfigureAwait(false);
        UniversalMediaMetadata universalMetadata = metadata.Universalize();
        ImmutableArray<MediaExportTarget> exportTargets = await _strategy.EnumerateExportTargetsAsync(contentId, partId, cancellationToken)
            .ToImmutableArrayAsync(cancellationToken).ConfigureAwait(false);
        ImmutableSortedSet<string> tags = await _strategy.GetTagsAsync(contentId, cancellationToken).ConfigureAwait(false);
        ImmutableArray<string> urls = await _strategy.GetUrlsAsync(metadata, cancellationToken).ConfigureAwait(false);
        ImmutableArray<MetadataSearchField> sharedMetadataSearchFields = SharedMetadataSearchFields.GetAllSharedFields(mediaFullId, tags, exportTargets);
        ImmutableArray<MetadataSearchField> metadataSearchFields = [
            ..metadata.GetSearchFields(),
            ..sharedMetadataSearchFields,
        ];
        return new()
        {
            MediaEntry = new()
            {
                Id = mediaFullId,
                Metadata = universalMetadata,
                ExportTargets = exportTargets,
                Tags = tags,
                Urls = urls,
            },
            MetadataSearchFields = metadataSearchFields,
        };
    }
}
