using System.Net.Http.Headers;

namespace umm.SearchIndex;

public interface IElasticsearchAuthentication
{
    AuthenticationHeaderValue Header { get; }
}
