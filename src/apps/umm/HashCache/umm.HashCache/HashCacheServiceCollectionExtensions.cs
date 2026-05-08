using Databases;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Npgsql;
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Runtime.CompilerServices;

namespace umm.HashCache;

public static class HashCacheServiceCollectionExtensions
{
    public static IServiceCollection AddHashCache(this IServiceCollection serviceCollection, IConfiguration configuration)
    {
        if (RuntimeFeature.IsDynamicCodeSupported)
        {
            serviceCollection.TryAddSingleton<ISqliteDataSourceFactory, SqliteDataSourceFactory>();
        }

        string hashCachePrefix = "HashCache";
        IConfiguration hashCacheConfiguration = configuration.GetSection(hashCachePrefix);
        return serviceCollection
            .AddTransient<IMultiHashProvider>(_ =>
            {
                IEnumerable<string> hashFunctionNames = hashCacheConfiguration
                    .GetSection("HashFunctions")
                    .GetChildren()
                    .Select(c => c.Value)
                    .OfType<string>();
                return new MultiHashProvider(hashFunctionNames);
            })
            .AddTransient(sp => GetHashCache(sp, hashCacheConfiguration));
    }

    private static IHashCache GetHashCache(IServiceProvider serviceProvider, IConfiguration hashCacheConfiguration)
    {
        IConfiguration storageConfiguration = hashCacheConfiguration.GetSection("Storage");
        HashCacheMutableOptions? hashCacheMutableOptions = hashCacheConfiguration.Get<HashCacheMutableOptions>();
        HashCacheOptions hashCacheOptions = new()
        {
            MediaTypes = [.. hashCacheMutableOptions?.MediaTypes ?? []],
        };
        string? storageType = storageConfiguration["type"];
        if (string.IsNullOrWhiteSpace(storageType))
        {
            return new NullHashCache();
        }
        else if (storageType.Equals("sqlite", StringComparison.OrdinalIgnoreCase) && RuntimeFeature.IsDynamicCodeSupported)
        {
            var options = storageConfiguration.Get<SqliteOptions>() ?? new();
            if (string.IsNullOrWhiteSpace(options.File)) throw new InvalidOperationException("Sqlite hash cache storage has no specified file.");
            DbDataSource dataSource = serviceProvider.GetRequiredService<ISqliteDataSourceFactory>()
                .CreateDataSource(options.File, SqliteOpenMode.ReadWriteCreate);
            return new DbHashCache(dataSource, hashCacheOptions);
        }
        else if (storageType.Equals("postgresql", StringComparison.OrdinalIgnoreCase))
        {
            var options = storageConfiguration.Get<PostgresqlOptions>() ?? new();
            if (string.IsNullOrWhiteSpace(options.ConnectionString)) throw new InvalidOperationException("Postgresql hash cache storage has no specified connection string.");
            DbDataSource dataSource = NpgsqlDataSource.Create(options.ConnectionString);
            return new DbHashCache(dataSource, hashCacheOptions);
        }
        throw new InvalidOperationException("Unknown hash cache storage type.");
    }

    internal sealed class HashCacheMutableOptions
    {
        public List<string>? MediaTypes { get; set; }
    }

    internal sealed class SqliteOptions
    {
        public string? File { get; set; }
    }

    internal sealed class PostgresqlOptions
    {
        public string? ConnectionString { get; set; }
    }
}
