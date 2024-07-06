using System.Threading.Tasks;

namespace Databases;

public interface IDatabaseRow
{
    public string GetColumn(int ordinal);

    public T GetValue<T>(int ordinal);

    public Task<T> GetValueAsync<T>(int ordinal);

    public bool IsDBNull(int ordinal);

    public Task<bool> IsDBNullAsync(int ordinal);

    public int GetOrdinal(string column);

    public T GetValue<T>(string column);

    public Task<T> GetValueAsync<T>(string column);

    public bool IsDBNull(string column);

    public Task<bool> IsDBNullAsync(string column);
}
