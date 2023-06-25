namespace Databases;

public interface ISqliteClient : IDatabaseClient
{
    void SetConnectionString(string file, SqliteOpenMode mode);
}
