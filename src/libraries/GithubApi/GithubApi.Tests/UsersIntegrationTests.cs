using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;

namespace GithubApi.Tests;

[TestClass]
public sealed class UsersIntegrationTests : IntegrationTests
{
    public TestContext TestContext { get; set; }

    [TestMethod]
    public async Task TestGetAuthenticatedUser()
    {
        // Use client with no token
        using HttpClient httpClient = new();
        GithubApiClient apiClient = new(httpClient);
        await Assert.ThrowsExactlyAsync<HttpRequestException>(() => apiClient.GetAuthenticatedUserAsync(TestContext.CancellationTokenSource.Token));
    }

    [TestMethod]
    public async Task TestGetUserAsync()
    {
        string username = "octocat";
        var account = await ApiClient.GetUserAsync(username, TestContext.CancellationTokenSource.Token);
        Assert.AreEqual(username, account.Login);
        Assert.AreEqual(583231, account.Id);
    }

    [TestMethod]
    public async Task TestGetNonexistentUserAsync()
    {
        HttpRequestException exception = await Assert.ThrowsExactlyAsync<HttpRequestException>(() => ApiClient.GetUserAsync("account", TestContext.CancellationTokenSource.Token));
        Assert.AreEqual(HttpStatusCode.NotFound, exception.StatusCode);
    }
}
