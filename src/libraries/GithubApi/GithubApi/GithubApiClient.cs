using GithubApi.Models;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using System.Threading;
using System.Threading.Tasks;
using Utils;

namespace GithubApi;

public sealed class GithubApiClient : IGithubApiClient
{
    private static JsonContext JsonContext => JsonContext.Default;

    private readonly HttpClient _httpClient;

    public GithubApiClient(HttpClient httpClient, string? authToken = null, ProductInfoHeaderValue? userAgent = null)
    {
        _httpClient = httpClient;
        _httpClient.BaseAddress = new("https://api.github.com/");
        _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/vnd.github+json"));
        if (!string.IsNullOrWhiteSpace(authToken))
        {
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", authToken);
        }
        _httpClient.DefaultRequestHeaders.Add("X-GitHub-Api-Version", "2022-11-28");
        _httpClient.DefaultRequestHeaders.UserAgent.Add(userAgent ?? new ProductInfoHeaderValue(new ProductHeaderValue("Mozilla", "5.0")));
    }

    public async Task<Stream> DownloadAsync(string url, CancellationToken cancellationToken = default)
    {
        using HttpRequestMessage request = new(HttpMethod.Get, url)
        {
            Headers = {
                { HttpRequestHeader.Accept.ToString(), Mimetypes.Application.OctetStream },
            },
        };
        // Only thing response should do is dispose of stream, so it should be ok to not dispose
        // If we do dispose, returned stream would be disposed and thus unusable
        HttpResponseMessage response = await _httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
    }

    // Releases
    public IAsyncEnumerable<Release> GetReleasesAsync(string owner, string repo, PaginationOptions? paginationOptions = null, CancellationToken cancellationToken = default)
    {
        return EnumeratePagesAsync($"repos/{owner}/{repo}/releases", JsonContext.Release, paginationOptions, cancellationToken);
    }
    public IAsyncEnumerable<ReleaseAsset> GetReleaseAssetsAsync(string owner, string repo, int releaseId, PaginationOptions? paginationOptions = null, CancellationToken cancellationToken = default)
    {
        return EnumeratePagesAsync($"repos/{owner}/{repo}/releases/{releaseId}/assets", JsonContext.ReleaseAsset, paginationOptions, cancellationToken);
    }
    public Task<Release> GetLatestReleaseAsync(string owner, string repo, CancellationToken cancellationToken = default)
    {
        return _httpClient.GetJsonAsync($"repos/{owner}/{repo}/releases/latest", JsonContext.Release, cancellationToken);
    }

    // Repositories
    public Task<Repository> GetRepositoryAsync(string owner, string repo, CancellationToken cancellationToken = default)
    {
        return _httpClient.GetJsonAsync($"repos/{owner}/{repo}", JsonContext.Repository, cancellationToken);
    }

    public Task<Repository> CreateRepositoryAsync(CreateRepositoryRequest request, CancellationToken cancellationToken = default)
    {
        return _httpClient.PostJsonAsync("user/repos", request, JsonContext.CreateRepositoryRequest, JsonContext.Repository, cancellationToken);
    }

    public Task<Repository> CreateRepositoryAsync(string organization, CreateRepositoryRequest request, CancellationToken cancellationToken = default)
    {
        return _httpClient.PostJsonAsync($"orgs/{organization}/repos", request, JsonContext.CreateRepositoryRequest, JsonContext.Repository, cancellationToken);
    }

    // Users
    public Task<Account> GetAuthenticatedUserAsync(CancellationToken cancellationToken = default)
    {
        return _httpClient.GetJsonAsync("user", JsonContext.Account, cancellationToken);
    }

    public Task<Account> GetUserAsync(string username, CancellationToken cancellationToken = default)
    {
        return _httpClient.GetJsonAsync($"users/{username}", JsonContext.Account, cancellationToken);
    }

    // Private
    private async IAsyncEnumerable<T> EnumeratePagesAsync<T>(string initialUrl, JsonTypeInfo<T> jsonTypeInfo, PaginationOptions? paginationOptions, [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        string? nextPageUri = initialUrl;
        if (paginationOptions is not null)
        {
            nextPageUri += $"?per_page={paginationOptions.PerPage}&page={paginationOptions.Page}";
        }
        while (!string.IsNullOrWhiteSpace(nextPageUri))
        {
            using HttpResponseMessage response = await _httpClient.GetAsync(nextPageUri, cancellationToken).ConfigureAwait(false);
            response.EnsureSuccessStatusCode();
            using Stream stream = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
            await foreach (T? item in JsonSerializer.DeserializeAsyncEnumerable(stream, jsonTypeInfo, cancellationToken).ConfigureAwait(false))
            {
                if (item is not null) yield return item;
            }
            nextPageUri = response.Headers.TryGetValues("link", out IEnumerable<string>? linkValues)
                ? LinkParser.GetNextUri(linkValues?.FirstOrDefault())
                : null;
        }
    }
}
