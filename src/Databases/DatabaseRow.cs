using System.Data.Common;
using System.Threading.Tasks;

namespace Databases;

internal sealed class DatabaseRow : IDatabaseRow
{
    private readonly DbDataReader _dataReader;

    public DatabaseRow(DbDataReader dataReader)
    {
        _dataReader = dataReader;
    }

    public string GetColumn(int ordinal) => _dataReader.GetName(ordinal);

    public T GetValue<T>(int ordinal) => _dataReader.GetFieldValue<T>(ordinal);

    public Task<T> GetValueAsync<T>(int ordinal) => _dataReader.GetFieldValueAsync<T>(ordinal);

    public bool IsDBNull(int ordinal) => _dataReader.IsDBNull(ordinal);

    public Task<bool> IsDBNullAsync(int ordinal) => _dataReader.IsDBNullAsync(ordinal);

    public int GetOrdinal(string column) => _dataReader.GetOrdinal(column);

    public T GetValue<T>(string column) => GetValue<T>(GetOrdinal(column));

    public Task<T> GetValueAsync<T>(string column) => GetValueAsync<T>(GetOrdinal(column));

    public bool IsDBNull(string column) => IsDBNull(GetOrdinal(column));

    public Task<bool> IsDBNullAsync(string column) => IsDBNullAsync(GetOrdinal(column));
}
