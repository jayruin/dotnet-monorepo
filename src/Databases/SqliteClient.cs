using Microsoft.Data.Sqlite;
using System;

namespace Databases;

public sealed class SqliteClient : DatabaseClient, ISqliteClient
{
    public SqliteClient() : base(SqliteFactory.Instance)
    {
    }

    public void SetConnectionString(string file, SqliteOpenMode mode)
    {
        Microsoft.Data.Sqlite.SqliteOpenMode internalMode = mode switch
        {
            SqliteOpenMode.ReadOnly => Microsoft.Data.Sqlite.SqliteOpenMode.ReadOnly,
            SqliteOpenMode.ReadWrite => Microsoft.Data.Sqlite.SqliteOpenMode.ReadWrite,
            SqliteOpenMode.ReadWriteCreate => Microsoft.Data.Sqlite.SqliteOpenMode.ReadWriteCreate,
            SqliteOpenMode.Memory => Microsoft.Data.Sqlite.SqliteOpenMode.Memory,
            _ => throw new ArgumentOutOfRangeException(nameof(mode)),
        };
        ConnectionString = new SqliteConnectionStringBuilder()
        {
            DataSource = file,
            Mode = internalMode,
        }.ToString();
    }
}
