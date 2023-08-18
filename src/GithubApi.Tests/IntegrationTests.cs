using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
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
        // If running on gh actions, might run into rate limit without token
        ApiClient = new GithubApiClient(HttpClient, Environment.GetEnvironmentVariable("GH_TOKEN"));
    }

    [TestCleanup]
    public void Cleanup()
    {
        HttpClient.Dispose();
    }
}
