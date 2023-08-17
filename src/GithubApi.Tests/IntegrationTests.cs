using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Net.Http;

namespace GithubApi.Tests;

[TestClass]
public abstract class IntegrationTests
{
    public required HttpClient HttpClient { get; set; }
    public required IGithubApiClient ApiClient { get; set; }

    [TestInitialize]
    public void Initialize()
    {
        HttpClient = new();
        ApiClient = new GithubApiClient(HttpClient);
    }

    [TestCleanup]
    public void Cleanup()
    {
        HttpClient.Dispose();
    }
}
