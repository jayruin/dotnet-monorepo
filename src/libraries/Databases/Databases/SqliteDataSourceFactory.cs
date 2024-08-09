using Microsoft.Data.Sqlite;
using System;
using System.Data.Common;

namespace Databases;

public sealed class SqliteDataSourceFactory : ISqliteDataSourceFactory
{
    public DbDataSource CreateDataSource(string file, SqliteOpenMode mode)
    {
        Microsoft.Data.Sqlite.SqliteOpenMode internalMode = mode switch
        {
            SqliteOpenMode.ReadOnly => Microsoft.Data.Sqlite.SqliteOpenMode.ReadOnly,
            SqliteOpenMode.ReadWrite => Microsoft.Data.Sqlite.SqliteOpenMode.ReadWrite,
            SqliteOpenMode.ReadWriteCreate => Microsoft.Data.Sqlite.SqliteOpenMode.ReadWriteCreate,
            SqliteOpenMode.Memory => Microsoft.Data.Sqlite.SqliteOpenMode.Memory,
            _ => throw new ArgumentOutOfRangeException(nameof(mode)),
        };
        string connectionString = new SqliteConnectionStringBuilder()
        {
            DataSource = file,
            Mode = internalMode,
        }.ToString();
        return SqliteFactory.Instance.CreateDataSource(connectionString);
    }
}
