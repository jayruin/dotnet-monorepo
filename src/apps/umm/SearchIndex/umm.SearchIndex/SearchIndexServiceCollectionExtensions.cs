using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Net.Http;

namespace umm.SearchIndex;

public static class SearchIndexServiceCollectionExtensions
{
    public static IServiceCollection AddSearchIndexServices(this IServiceCollection serviceCollection, IConfiguration configuration)
    {
        string searchIndexPrefix = "searchindex";
        IConfiguration searchIndexConfiguration = configuration.GetSection(searchIndexPrefix);
        string? searchIndexType = searchIndexConfiguration["type"];
        if (string.IsNullOrWhiteSpace(searchIndexType)) return serviceCollection;
        if (searchIndexType.Equals("elasticsearch", StringComparison.OrdinalIgnoreCase))
        {
            var options = searchIndexConfiguration.Get<ElasticsearchSearchIndexOptions>();
            string? url = options?.Url;
            string? username = options?.Username;
            string? password = options?.Password;
            bool insecure = options?.Insecure ?? false;
            if (!string.IsNullOrWhiteSpace(url))
            {
                ElasticsearchBasicAuthentication? authentication = null;
                if (!string.IsNullOrWhiteSpace(username) && !string.IsNullOrWhiteSpace(password))
                {
                    authentication = new(username, password);

                }
                if (authentication is not null)
                {
                    IHttpClientBuilder httpClientBuilder = serviceCollection.AddHttpClient<ISearchIndex, ElasticsearchSearchIndex>((hc, sp) =>
                    {
                        return new ElasticsearchSearchIndex(hc, new Uri(url), authentication);
                    });
                    if (insecure)
                    {
                        httpClientBuilder.ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler()
                        {
                            ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator,
                        });
                    }
                }
            }
        }
        return serviceCollection;
    }

    internal sealed class ElasticsearchSearchIndexOptions
    {
        public string? Url { get; set; }
        public string? Username { get; set; }
        public string? Password { get; set; }
        public bool Insecure { get; set; }
    }
}
