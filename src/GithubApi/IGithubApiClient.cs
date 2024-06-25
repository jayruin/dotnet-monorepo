using GithubApi.Models;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace GithubApi;

public interface IGithubApiClient
{
    Task<Stream> DownloadAsync(string url, CancellationToken cancellationToken = default);

    // Releases
    IAsyncEnumerable<Release> GetReleasesAsync(string owner, string repo, PaginationOptions? paginationOptions = null, CancellationToken cancellationToken = default);
    IAsyncEnumerable<ReleaseAsset> GetReleaseAssetsAsync(string owner, string repo, int releaseId, PaginationOptions? paginationOptions = null, CancellationToken cancellationToken = default);
    Task<Release> GetLatestReleaseAsync(string owner, string repo, CancellationToken cancellationToken = default);

    // Repositories
    Task<Repository> GetRepositoryAsync(string owner, string repo, CancellationToken cancellationToken = default);
    Task<Repository> CreateRepositoryAsync(CreateRepositoryRequest request, CancellationToken cancellationToken = default);
    Task<Repository> CreateRepositoryAsync(string organization, CreateRepositoryRequest request, CancellationToken cancellationToken = default);

    // Users
    Task<Account> GetAuthenticatedUserAsync(CancellationToken cancellationToken = default);
    Task<Account> GetUserAsync(string username, CancellationToken cancellationToken = default);
}
