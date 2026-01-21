using GithubApi.Models;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Utils;

namespace GithubApi.Tests;

[TestClass]
public sealed class ReleasesIntegrationTests : IntegrationTests
{
    public TestContext TestContext { get; set; }

    [TestMethod]
    public async Task TestGetReleasesAsync()
    {
        List<Release> releases = [];
        await foreach (Release release in ApiClient.GetReleasesAsync("actions", "setup-dotnet", new PaginationOptions { PerPage = 10, Page = 1 }, TestContext.CancellationToken))
        {
            releases.Add(release);
        }
        Assert.IsGreaterThan(10, releases.Count);
    }

    [TestMethod]
    public async Task TestGetReleaseAssetsAsync()
    {
        List<ReleaseAsset> releaseAssets = [];
        await foreach (ReleaseAsset releaseAsset in ApiClient.GetReleaseAssetsAsync("cli", "cli", 22808788, new PaginationOptions { PerPage = 5, Page = 1 }, TestContext.CancellationToken))
        {
            releaseAssets.Add(releaseAsset);
        }
        Assert.HasCount(7, releaseAssets);
    }

    [TestMethod]
    public async Task TestDownloadReleaseAsync()
    {
        string url = "https://api.github.com/repos/cli/cli/releases/assets/17320341";
        await using Stream stream = await ApiClient.DownloadAsync(url, TestContext.CancellationToken);
        string actual = Encoding.UTF8.GetString(await stream.ToByteArrayAsync(TestContext.CancellationToken));
        string expected = string.Join('\n',
            "85f9b895aab95c9882dfdb55193f9330514ab6e80777a972da04483c7a117010  gh_0.4.0_macOS_amd64.tar.gz",
            "e050d6f6a760a6093d3ea62a3d0aea26f45489a6c6954aa105ed951593a537fe  gh_0.4.0_linux_amd64.rpm",
            "3bcdb6673da1c35edc24b04b29f4698368f13c697c6111db45b966915b96676f  gh_0.4.0_linux_amd64.tar.gz",
            "d4446dce47c4fd37440b6231856a03d59abc071c77401152c8619ef198864af7  gh_0.4.0_linux_amd64.deb",
            "87d2cc70d9ccca03a02c85fb3d18096670a2375af48017f3ca8345be7911197d  gh_0.4.0_windows_amd64.exe",
            "");
        Assert.AreEqual(expected, actual);
    }
}
