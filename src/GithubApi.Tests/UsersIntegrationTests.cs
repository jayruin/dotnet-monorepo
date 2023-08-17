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
        HttpRequestException exception = await Assert.ThrowsExceptionAsync<HttpRequestException>(() => ApiClient.GetAuthenticatedUserAsync());
        Assert.AreEqual(exception.StatusCode, HttpStatusCode.Unauthorized);
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
