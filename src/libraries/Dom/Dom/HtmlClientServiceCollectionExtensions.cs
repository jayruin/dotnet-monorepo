using Browsers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Dom;

public static class HtmlClientServiceCollectionExtensions
{
    extension(IServiceCollection serviceCollection)
    {
        public IServiceCollection TryAddHtmlClient(string implementation)
        {
            if (implementation == HtmlClientImplementation.Browser)
            {
                serviceCollection
                    .TryAddPlaywrightServices()
                    .TryAddKeyedTransient<IHtmlClient, BrowserHtmlClient>(HtmlClientImplementation.Browser);
            }
            if (implementation == HtmlClientImplementation.HttpClient)
            {
                serviceCollection.AddHttpClient(HtmlClientImplementation.HttpClient)
                    .AddStandardResilienceHandler().Services
                    .TryAddKeyedTransient<IHtmlClient, HttpClientHtmlClient>(HtmlClientImplementation.HttpClient);
            }
            return serviceCollection;
        }
    }
}
