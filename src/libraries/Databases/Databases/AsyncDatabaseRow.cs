using System.Data.Common;
using System.Threading;
using System.Threading.Tasks;

namespace Databases;

internal sealed class AsyncDatabaseRow : IAsyncDatabaseRow
{
    private readonly DbDataReader _dataReader;
    private readonly CancellationToken _cancellationToken;

    public AsyncDatabaseRow(DbDataReader dataReader, CancellationToken cancellationToken)
    {
        _dataReader = dataReader;
        _cancellationToken = cancellationToken;
    }

    public Task<T> GetValueAsync<T>(int ordinal) => _dataReader.GetFieldValueAsync<T>(ordinal, _cancellationToken);
    public Task<T> GetValueAsync<T>(string column) => _dataReader.GetFieldValueAsync<T>(_dataReader.GetOrdinal(column), _cancellationToken);
    public Task<bool> IsDBNullAsync(int ordinal) => _dataReader.IsDBNullAsync(ordinal, _cancellationToken);
    public Task<bool> IsDBNullAsync(string column) => _dataReader.IsDBNullAsync(_dataReader.GetOrdinal(column), _cancellationToken);
}
