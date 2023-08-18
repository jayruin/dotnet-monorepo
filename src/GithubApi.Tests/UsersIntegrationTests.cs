using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;

namespace GithubApi.Tests;

[TestClass]
public sealed class UsersIntegrationTests : IntegrationTests
{
    [TestMethod]
    public async Task TestGetAuthenticatedUser()
    {
        // Use client with no token
        using HttpClient httpClient = new();
        GithubApiClient apiClient = new(httpClient);
        HttpRequestException exception = await Assert.ThrowsExceptionAsync<HttpRequestException>(() => apiClient.GetAuthenticatedUserAsync());
        Assert.AreEqual(HttpStatusCode.Unauthorized, exception.StatusCode);
    }

    [TestMethod]
    public async Task TestGetUserAsync()
    {
        string username = "octocat";
        var account = await ApiClient.GetUserAsync(username);
        Assert.AreEqual(username, account.Login);
        Assert.AreEqual(583231, account.Id);
    }

    [TestMethod]
    public async Task TestGetNonexistentUserAsync()
    {
        HttpRequestException exception = await Assert.ThrowsExceptionAsync<HttpRequestException>(() => ApiClient.GetUserAsync("account"));
        Assert.AreEqual(exception.StatusCode, HttpStatusCode.NotFound);
    }
}
