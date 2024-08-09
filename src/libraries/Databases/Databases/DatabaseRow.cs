using System.Data.Common;

namespace Databases;

internal sealed class DatabaseRow : IDatabaseRow
{
    private readonly DbDataReader _dataReader;

    public DatabaseRow(DbDataReader dataReader)
    {
        _dataReader = dataReader;
    }

    public T GetValue<T>(int ordinal) => _dataReader.GetFieldValue<T>(ordinal);
    public T GetValue<T>(string column) => _dataReader.GetFieldValue<T>(_dataReader.GetOrdinal(column));
    public bool IsDBNull(int ordinal) => _dataReader.IsDBNull(ordinal);
    public bool IsDBNull(string column) => _dataReader.IsDBNull(_dataReader.GetOrdinal(column));
}
