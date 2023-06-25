using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Databases;

public interface IDatabaseClient
{
    string? ConnectionString { get; set; }

    Task ExecuteNonQueryAsync(FormattableString commandText);

    void ExecuteNonQuery(FormattableString commandText);

    IAsyncEnumerable<T> ExecuteQueryAsync<T>(FormattableString commandText, Func<IDatabaseRow, Task<T>> mapper);

    IEnumerable<T> ExecuteQuery<T>(FormattableString commandText, Func<IDatabaseRow, T> mapper);
}