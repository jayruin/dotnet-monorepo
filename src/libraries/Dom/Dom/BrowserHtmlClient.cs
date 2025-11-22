using AngleSharp;
using AngleSharp.Html.Dom;
using AngleSharp.Html.Parser;
using Microsoft.Playwright;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Dom;

internal sealed class BrowserHtmlClient : IHtmlClient, IAsyncDisposable
{
    private readonly IPlaywright _playwright;
    private readonly IHtmlParser _htmlParser;
    private IBrowser? _browser;
    private IBrowserContext? _browserContext;
    private IPage? _page;

    public BrowserHtmlClient(IPlaywright playwright)
    {
        _playwright = playwright;
        _htmlParser = BrowsingContext.New(Configuration.Default).GetService<IHtmlParser>()
            ?? throw new InvalidOperationException("Could not construct html parser.");
    }

    public Uri? BaseUri { get; set; }

    public string? UserAgent { get; set; }

    public async ValueTask DisposeAsync()
    {
        if (_page is not null)
        {
            await _page.CloseAsync().ConfigureAwait(false);
        }
        if (_browserContext is not null)
        {
            await _browserContext.DisposeAsync().ConfigureAwait(false);
        }
        if (_browser is not null)
        {
            await _browser.DisposeAsync().ConfigureAwait(false);
        }
    }

    public async Task<IHtmlDocument> GetHtmlDocumentAsync(string url, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        IPage page = await GetPageAsync(cancellationToken).ConfigureAwait(false);
        string finalUrl = BaseUri is null ? url : new Uri(BaseUri, url).AbsoluteUri;
        await page.GotoAsync(finalUrl).ConfigureAwait(false);
        await page.WaitForLoadStateAsync(LoadState.NetworkIdle).ConfigureAwait(false);
        string htmlString = await page.ContentAsync().ConfigureAwait(false);
        return await _htmlParser.ParseDocumentAsync(htmlString, cancellationToken).ConfigureAwait(false);
    }

    private async Task<IPage> GetPageAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        _browser ??= await _playwright.Firefox.LaunchAsync(new()
        {
            Headless = false,
        }).ConfigureAwait(false);
        _browserContext = await _browser.NewContextAsync(new()
        {
            UserAgent = UserAgent,
        }).ConfigureAwait(false);
        _page ??= await _browserContext.NewPageAsync().ConfigureAwait(false);
        return _page;
    }
}
