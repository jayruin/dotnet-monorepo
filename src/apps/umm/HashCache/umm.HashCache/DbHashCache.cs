using Databases;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Data.Common;
using System.Threading;
using System.Threading.Tasks;
using umm.Library;

namespace umm.HashCache;

public sealed class DbHashCache : IHashCache
{
    private readonly DbDataSource _dataSource;
    private readonly HashCacheOptions _options;

    public DbHashCache(DbDataSource dataSource, HashCacheOptions options)
    {
        _dataSource = dataSource;
        _options = options;
    }

    public Task<bool> CanHandleAsync(string mediaType, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(_options.MediaTypes.Contains(mediaType));
    }

    public async Task<ImmutableSortedDictionary<string, string>> GetHashesAsync(MediaFullId id, string exportId, CancellationToken cancellationToken = default)
    {
        IAsyncEnumerable<KeyValuePair<string, string>> hashes = _dataSource.ExecuteCommandAsync($"""
                SELECT hash_name, hash_value
                FROM hashes
                WHERE vendor_id = {id.VendorId} AND content_id = {id.ContentId} AND part_id = {id.PartId} AND export_id = {exportId};
            """, MapRowAsync, cancellationToken);
        ImmutableSortedDictionary<string, string>.Builder builder = ImmutableSortedDictionary.CreateBuilder<string, string>();
        await foreach ((string hashName, string hashValue) in hashes.ConfigureAwait(false))
        {
            builder.Add(hashName, hashValue);
        }
        return builder.ToImmutable();
    }

    public async Task SetHashesAsync(MediaFullId id, string exportId, ImmutableSortedDictionary<string, string> hashes, CancellationToken cancellationToken = default)
    {
        foreach ((string hashName, string hashValue) in hashes)
        {
            await _dataSource.ExecuteCommandAsync($"""
                INSERT INTO hashes(vendor_id, content_id, part_id, export_id, hash_name, hash_value)
                VALUES ({id.VendorId}, {id.ContentId}, {id.PartId}, {exportId}, {hashName}, {hashValue})
                ON CONFLICT(vendor_id, content_id, part_id, export_id, hash_name) DO
                UPDATE
                SET
                    hash_value = excluded.hash_value;
            """, cancellationToken).ConfigureAwait(false);
        }
    }

    public Task ClearAsync(string vendorId, CancellationToken cancellationToken = default)
    {
        return _dataSource.ExecuteCommandAsync($"""
                DELETE FROM hashes
                WHERE vendor_id = {vendorId};
            """, cancellationToken);
    }

    public async Task ResetAsync(CancellationToken cancellationToken = default)
    {
        await _dataSource.ExecuteCommandAsync($"""
                DROP TABLE IF EXISTS hashes;
            """, cancellationToken).ConfigureAwait(false);
        await _dataSource.ExecuteCommandAsync($"""
                CREATE TABLE IF NOT EXISTS hashes(
                    vendor_id TEXT,
                    content_id TEXT,
                    part_id TEXT,
                    export_id TEXT,
                    hash_name TEXT,
                    hash_value TEXT,
                    PRIMARY KEY(vendor_id, content_id, part_id, export_id, hash_name)
                );
            """, cancellationToken).ConfigureAwait(false);
    }

    private static async Task<KeyValuePair<string, string>> MapRowAsync(IAsyncDatabaseRow row)
    {
        return new(
            await row.GetValueAsync<string>("hash_name"),
            await row.GetValueAsync<string>("hash_value"));
    }
}
