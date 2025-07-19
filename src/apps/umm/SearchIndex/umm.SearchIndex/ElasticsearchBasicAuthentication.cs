using System;
using System.Net.Http.Headers;
using System.Text;

namespace umm.SearchIndex;

public sealed class ElasticsearchBasicAuthentication : IElasticsearchAuthentication
{
    public ElasticsearchBasicAuthentication(string username, string password)
    {
        Header = new("Basic", Convert.ToBase64String(new UTF8Encoding().GetBytes($"{username}:{password}")));
    }

    public AuthenticationHeaderValue Header { get; }
}
