using ksse.Health;
using ksse.ReadingProgress;
using ksse.Users;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.TestHost;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;

namespace ksse.Tests;

[TestClass]
public sealed class HealthTests
{
    public TestContext TestContext { get; set; }

    [TestMethod]
    public async Task Test_ValidDatabases_ReturnsOkState()
    {
        await using WebApplication app = WebApplicationForTesting.Create(new Dictionary<string, string?>()
        {
            { $"Databases:{UsersDbContext.Id}:Provider", "InMemory" },
            { $"Databases:{UsersDbContext.Id}:DatabaseName", nameof(HealthTests) },
            { $"Databases:{ProgressDbContext.Id}:Provider", "InMemory" },
            { $"Databases:{ProgressDbContext.Id}:DatabaseName", nameof(HealthTests) },
        });
        await app.StartAsync(TestContext.CancellationToken);
        using HttpClient client = app.GetTestClient();
        try
        {
            using HttpResponseMessage responseMessage = await client.GetAsync("healthcheck", TestContext.CancellationToken);
            Assert.AreEqual(HttpStatusCode.OK, responseMessage.StatusCode);
            HealthCheckResponse? response = await responseMessage.Content.ReadFromJsonAsync(HealthChecksJsonContext.Default.HealthCheckResponse, TestContext.CancellationToken);
            Assert.IsNotNull(response);
            Assert.AreEqual(HealthCheckResponse.Ok.State, response.State);
        }
        finally
        {
            await app.StopAsync(TestContext.CancellationToken);
        }
    }

    [TestMethod]
    public async Task Test_InvalidUsersDatabase_ReturnsServiceUnavailable()
    {
        await using WebApplication app = WebApplicationForTesting.Create(new Dictionary<string, string?>()
        {
            { $"Databases:{UsersDbContext.Id}:Provider", "Sqlite" },
            { $"Databases:{UsersDbContext.Id}:ConnectionString", "Data Source=invalid.db" },
            { $"Databases:{ProgressDbContext.Id}:Provider", "InMemory" },
            { $"Databases:{ProgressDbContext.Id}:DatabaseName", nameof(HealthTests) },
        });
        await app.StartAsync(TestContext.CancellationToken);
        using HttpClient client = app.GetTestClient();
        try
        {
            using HttpResponseMessage responseMessage = await client.GetAsync("healthcheck", TestContext.CancellationToken);
            Assert.AreEqual(HttpStatusCode.ServiceUnavailable, responseMessage.StatusCode);
        }
        finally
        {
            await app.StopAsync(TestContext.CancellationToken);
        }
    }

    [TestMethod]
    public async Task Test_InvalidProgressDatabase_ReturnsServiceUnavailable()
    {
        await using WebApplication app = WebApplicationForTesting.Create(new Dictionary<string, string?>()
        {
            { $"Databases:{UsersDbContext.Id}:Provider", "InMemory" },
            { $"Databases:{UsersDbContext.Id}:DatabaseName", nameof(HealthTests) },
            { $"Databases:{ProgressDbContext.Id}:Provider", "Sqlite" },
            { $"Databases:{ProgressDbContext.Id}:ConnectionString", "Data Source=invalid.db" },
        });
        await app.StartAsync(TestContext.CancellationToken);
        using HttpClient client = app.GetTestClient();
        try
        {
            using HttpResponseMessage responseMessage = await client.GetAsync("healthcheck", TestContext.CancellationToken);
            Assert.AreEqual(HttpStatusCode.ServiceUnavailable, responseMessage.StatusCode);
        }
        finally
        {
            await app.StopAsync(TestContext.CancellationToken);
        }
    }

    [TestMethod]
    public async Task Test_InvalidAllDatabases_ReturnsServiceUnavailable()
    {
        await using WebApplication app = WebApplicationForTesting.Create(new Dictionary<string, string?>()
        {
            { $"Databases:{UsersDbContext.Id}:Provider", "Sqlite" },
            { $"Databases:{UsersDbContext.Id}:ConnectionString", "Data Source=invalid.db" },
            { $"Databases:{ProgressDbContext.Id}:Provider", "Sqlite" },
            { $"Databases:{ProgressDbContext.Id}:ConnectionString", "Data Source=invalid.db" },
        });
        await app.StartAsync(TestContext.CancellationToken);
        using HttpClient client = app.GetTestClient();
        try
        {
            using HttpResponseMessage responseMessage = await client.GetAsync("healthcheck", TestContext.CancellationToken);
            Assert.AreEqual(HttpStatusCode.ServiceUnavailable, responseMessage.StatusCode);
        }
        finally
        {
            await app.StopAsync(TestContext.CancellationToken);
        }
    }
}
