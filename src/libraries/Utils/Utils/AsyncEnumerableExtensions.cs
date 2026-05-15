using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;

namespace Utils;

public static class AsyncEnumerableExtensions
{
    extension<TSource>(IAsyncEnumerable<TSource> source)
    {
        public async ValueTask<ImmutableArray<TSource>> ToImmutableArrayAsync(
        CancellationToken cancellationToken = default)
        {
            ImmutableArray<TSource>.Builder builder = ImmutableArray.CreateBuilder<TSource>();
            await foreach (TSource element in source.WithCancellation(cancellationToken).ConfigureAwait(false))
            {
                builder.Add(element);
            }
            return builder.ToImmutable();
        }
    }
}
