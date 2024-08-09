namespace Databases;

public interface IDatabaseRow
{
    public T GetValue<T>(int ordinal);
    public T GetValue<T>(string column);
    public bool IsDBNull(int ordinal);
    public bool IsDBNull(string column);
}
