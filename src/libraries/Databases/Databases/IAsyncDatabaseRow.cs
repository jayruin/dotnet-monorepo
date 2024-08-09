using System.Threading.Tasks;

namespace Databases;

public interface IAsyncDatabaseRow
{
    public Task<T> GetValueAsync<T>(int ordinal);
    public Task<T> GetValueAsync<T>(string column);
    public Task<bool> IsDBNullAsync(int ordinal);
    public Task<bool> IsDBNullAsync(string column);
}
