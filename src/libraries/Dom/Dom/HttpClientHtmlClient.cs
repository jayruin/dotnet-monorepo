using AngleSharp;
using AngleSharp.Html.Dom;
using AngleSharp.Html.Parser;
using System;
using System.IO;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace Dom;

internal sealed class HttpClientHtmlClient : IHtmlClient
{
    private readonly HttpClient _httpClient;
    private readonly IHtmlParser _htmlParser;

    public HttpClientHtmlClient(IHttpClientFactory httpClientFactory)
    {
        _httpClient = httpClientFactory.CreateClient(HtmlClientImplementation.HttpClient);
        _htmlParser = BrowsingContext.New(Configuration.Default).GetService<IHtmlParser>()
            ?? throw new InvalidOperationException("Could not construct html parser.");
        UserAgent = "Mozilla/5.0";
    }

    public Uri? BaseUri
    {
        get => _httpClient.BaseAddress;
        set => _httpClient.BaseAddress = value;
    }

    public string? UserAgent
    {
        get => _httpClient.DefaultRequestHeaders.UserAgent.ToString();
        set
        {
            if (string.IsNullOrWhiteSpace(value)) return;
            _httpClient.DefaultRequestHeaders.UserAgent.Clear();
            _httpClient.DefaultRequestHeaders.UserAgent.ParseAdd(value);
        }
    }

    public async Task<IHtmlDocument> GetHtmlDocumentAsync(string url, CancellationToken cancellationToken = default)
    {
        Stream stream = await _httpClient.GetStreamAsync(url, cancellationToken).ConfigureAwait(false);
        await using ConfiguredAsyncDisposable configuredStream = stream.ConfigureAwait(false);
        return await _htmlParser.ParseDocumentAsync(stream, cancellationToken).ConfigureAwait(false);
    }
}
