using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;

namespace GithubApi.Tests;

[TestClass]
public sealed class RepositoriesIntegrationTests : IntegrationTests
{
    [TestMethod]
    public async Task TestGetRepositoryAsync()
    {
        string owner = "octocat";
        string repo = "Hello-World";
        var repository = await ApiClient.GetRepositoryAsync(owner, repo);
        Assert.AreEqual(1296269, repository.Id);
        Assert.AreEqual(owner, repository.Owner.Login);
        Assert.AreEqual(583231, repository.Owner.Id);
    }

    [TestMethod]
    public async Task TestGetNonexistentRepositoryAsync()
    {
        HttpRequestException exception = await Assert.ThrowsExceptionAsync<HttpRequestException>(() => ApiClient.GetRepositoryAsync("account", "repo"));
        Assert.AreEqual(exception.StatusCode, HttpStatusCode.NotFound);
    }
}
