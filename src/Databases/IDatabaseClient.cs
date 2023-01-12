using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Threading.Tasks;

namespace Databases;

public interface IDatabaseClient
{
    public DatabaseProvider DefaultProvider { get; set; }

    public string DefaultConnectionString { get; set; }

    public DbProviderFactory GetFactory(DatabaseProvider provider);

    public Task ExecuteNonQueryAsync(DatabaseProvider provider, string connectionString,
        FormattableString commandText);

    public void ExecuteNonQuery(DatabaseProvider provider, string connectionString,
        FormattableString commandText);

    public IAsyncEnumerable<T> ExecuteQueryAsync<T>(DatabaseProvider provider, string connectionString,
        FormattableString commandText, Func<IDatabaseRow, Task<T>> mapper);

    public IEnumerable<T> ExecuteQuery<T>(DatabaseProvider provider, string connectionString,
        FormattableString commandText, Func<IDatabaseRow, T> mapper);

    public Task ExecuteNonQueryAsync(FormattableString commandText);

    public void ExecuteNonQuery(FormattableString commandText);

    public IAsyncEnumerable<T> ExecuteQueryAsync<T>(FormattableString commandText, Func<IDatabaseRow, Task<T>> mapper);

    public IEnumerable<T> ExecuteQuery<T>(FormattableString commandText, Func<IDatabaseRow, T> mapper);
}