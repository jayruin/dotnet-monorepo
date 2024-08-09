using System.Data.Common;

namespace Databases;

public interface ISqliteDataSourceFactory
{
    DbDataSource CreateDataSource(string file, SqliteOpenMode mode);
}
