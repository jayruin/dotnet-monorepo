using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;

namespace GithubApi.Tests;

[TestClass]
public sealed class RepositoriesIntegrationTests : IntegrationTests
{
    public TestContext TestContext { get; set; }

    [TestMethod]
    public async Task TestGetRepositoryAsync()
    {
        string owner = "octocat";
        string repo = "Hello-World";
        var repository = await ApiClient.GetRepositoryAsync(owner, repo, TestContext.CancellationTokenSource.Token);
        Assert.AreEqual(1296269, repository.Id);
        Assert.AreEqual(owner, repository.Owner.Login);
        Assert.AreEqual(583231, repository.Owner.Id);
    }

    [TestMethod]
    public async Task TestGetNonexistentRepositoryAsync()
    {
        HttpRequestException exception = await Assert.ThrowsExactlyAsync<HttpRequestException>(() => ApiClient.GetRepositoryAsync("account", "repo", TestContext.CancellationTokenSource.Token));
        Assert.AreEqual(HttpStatusCode.NotFound, exception.StatusCode);
    }
}
