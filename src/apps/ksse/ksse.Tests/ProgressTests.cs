using ksse.Auth;
using ksse.Errors;
using ksse.ReadingProgress;
using ksse.Users;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading.Tasks;

namespace ksse.Tests;

[TestClass]
public sealed class ProgressTests
{
    private const string Username = "testuser";
    private const string Password = "Password1!";

    public TestContext TestContext { get; set; }

    public required WebApplication App { get; set; }
    public required HttpClient Client { get; set; }

    [TestInitialize]
    public async Task InitializeAsync()
    {
        App = WebApplicationForTesting.Create(new Dictionary<string, string?>()
        {
            { $"FeatureManagement:{UsersFeatures.CreateUsers}", "true" },
            { $"Databases:{UsersDbContext.Id}:Provider", "InMemory" },
            { $"Databases:{UsersDbContext.Id}:DatabaseName", nameof(ProgressTests) },
            { $"Databases:{ProgressDbContext.Id}:Provider", "InMemory" },
            { $"Databases:{ProgressDbContext.Id}:DatabaseName", nameof(ProgressTests) },
        });
        await App.StartAsync(TestContext.CancellationToken);
        Client = App.GetTestClient();
        UserManager<IdentityUser> userManager = App.Services.GetRequiredService<UserManager<IdentityUser>>();
        await userManager.CreateAsync(new()
        {
            UserName = Username,
        }, Password);
    }

    [TestCleanup]
    public async Task CleanupAsync()
    {
        Client.Dispose();
        await App.StopAsync(TestContext.CancellationToken);
        await App.DisposeAsync();
    }

    [TestMethod]
    public async Task Test_PutProgressWithoutAuth_Fails()
    {
        PutProgressRequest request = new()
        {
            Document = "document",
            Progress = "progress",
            Percentage = 0.5,
            Device = "device",
            DeviceId = "device_id",
        };
        using HttpResponseMessage responseMessage = await Client.PutAsJsonAsync("syncs/progress", request, ProgressJsonContext.Default.PutProgressRequest, TestContext.CancellationToken);
        Assert.AreEqual(HttpStatusCode.Unauthorized, responseMessage.StatusCode);
        ErrorResponse? response = await responseMessage.Content.ReadFromJsonAsync(ErrorsJsonContext.Default.ErrorResponse, TestContext.CancellationToken);
        Assert.IsNotNull(response);
        Assert.AreEqual(KoreaderErrors.UnauthorizedUser.Code, response.Code);
        Assert.AreEqual(KoreaderErrors.UnauthorizedUser.Message, response.Message);
    }

    [TestMethod]
    public async Task Test_PutProgressWithWrongPassword_Fails()
    {
        PutProgressRequest request = new()
        {
            Document = "document",
            Progress = "progress",
            Percentage = 0.5,
            Device = "device",
            DeviceId = "device_id",
        };
        using HttpRequestMessage requestMessage = new()
        {
            RequestUri = new("syncs/progress", UriKind.Relative),
            Method = HttpMethod.Put,
            Content = JsonContent.Create(request, ProgressJsonContext.Default.PutProgressRequest),
            Headers = {
                { KoreaderAuthOptions.UsernameHeader, Username },
                { KoreaderAuthOptions.PasswordHeader, $"Not{Password}" },
            },
        };
        using HttpResponseMessage responseMessage = await Client.SendAsync(requestMessage, TestContext.CancellationToken);
        Assert.AreEqual(HttpStatusCode.Unauthorized, responseMessage.StatusCode);
        ErrorResponse? response = await responseMessage.Content.ReadFromJsonAsync(ErrorsJsonContext.Default.ErrorResponse, TestContext.CancellationToken);
        Assert.IsNotNull(response);
        Assert.AreEqual(KoreaderErrors.UnauthorizedUser.Code, response.Code);
        Assert.AreEqual(KoreaderErrors.UnauthorizedUser.Message, response.Message);
    }

    [TestMethod]
    public async Task Test_PutProgressWithCorrectAuth_Succeeds()
    {
        PutProgressRequest request = new()
        {
            Document = "document",
            Progress = "progress",
            Percentage = 0.5,
            Device = "device",
            DeviceId = "device_id",
        };
        using HttpRequestMessage requestMessage = new()
        {
            RequestUri = new("syncs/progress", UriKind.Relative),
            Method = HttpMethod.Put,
            Content = JsonContent.Create(request, ProgressJsonContext.Default.PutProgressRequest),
            Headers = {
                { KoreaderAuthOptions.UsernameHeader, Username },
                { KoreaderAuthOptions.PasswordHeader, Password },
            },
        };
        using HttpResponseMessage responseMessage = await Client.SendAsync(requestMessage, TestContext.CancellationToken);
        Assert.AreEqual(HttpStatusCode.OK, responseMessage.StatusCode);
        PutProgressResponse? response = await responseMessage.Content.ReadFromJsonAsync(ProgressJsonContext.Default.PutProgressResponse, TestContext.CancellationToken);
        Assert.IsNotNull(response);
        Assert.AreEqual(request.Document, response.Document);
        IProgressManager progressManager = App.Services.GetRequiredService<IProgressManager>();
        UserManager<IdentityUser> userManager = App.Services.GetRequiredService<UserManager<IdentityUser>>();
        string? userId = (await userManager.FindByNameAsync(Username))?.Id;
        Assert.IsNotNull(userId);
        ProgressDocument? progressDocument = await progressManager.GetAsync(userId, request.Document, TestContext.CancellationToken);
        Assert.IsNotNull(progressDocument);
        Assert.AreEqual(request.Progress, progressDocument.Progress);
        Assert.AreEqual(request.Percentage, progressDocument.Percentage);
        Assert.AreEqual(request.Device, progressDocument.Device);
        Assert.AreEqual(request.DeviceId, progressDocument.DeviceId);
    }

    [TestMethod]
    public async Task Test_PutProgressTwiceWithCorrectAuth_SucceedsAndOverwrites()
    {
        PutProgressRequest request1 = new()
        {
            Document = "document",
            Progress = "progress",
            Percentage = 0.5,
            Device = "device",
            DeviceId = "device_id",
        };
        using HttpRequestMessage requestMessage1 = new()
        {
            RequestUri = new("syncs/progress", UriKind.Relative),
            Method = HttpMethod.Put,
            Content = JsonContent.Create(request1, ProgressJsonContext.Default.PutProgressRequest),
            Headers = {
                { KoreaderAuthOptions.UsernameHeader, Username },
                { KoreaderAuthOptions.PasswordHeader, Password },
            },
        };
        using HttpResponseMessage responseMessage1 = await Client.SendAsync(requestMessage1, TestContext.CancellationToken);
        Assert.AreEqual(HttpStatusCode.OK, responseMessage1.StatusCode);
        PutProgressResponse? response1 = await responseMessage1.Content.ReadFromJsonAsync(ProgressJsonContext.Default.PutProgressResponse, TestContext.CancellationToken);
        Assert.IsNotNull(response1);
        Assert.AreEqual(request1.Document, response1.Document);
        PutProgressRequest request2 = new()
        {
            Document = "document",
            Progress = "progress2",
            Percentage = 0.7,
            Device = "device2",
            DeviceId = "device_id2",
        };
        using HttpRequestMessage requestMessage2 = new()
        {
            RequestUri = new("syncs/progress", UriKind.Relative),
            Method = HttpMethod.Put,
            Content = JsonContent.Create(request2, ProgressJsonContext.Default.PutProgressRequest),
            Headers = {
                { KoreaderAuthOptions.UsernameHeader, Username },
                { KoreaderAuthOptions.PasswordHeader, Password },
            },
        };
        using HttpResponseMessage responseMessage2 = await Client.SendAsync(requestMessage2, TestContext.CancellationToken);
        Assert.AreEqual(HttpStatusCode.OK, responseMessage2.StatusCode);
        PutProgressResponse? response2 = await responseMessage2.Content.ReadFromJsonAsync(ProgressJsonContext.Default.PutProgressResponse, TestContext.CancellationToken);
        Assert.IsNotNull(response2);
        Assert.AreEqual(request2.Document, response2.Document);
        IProgressManager progressManager = App.Services.GetRequiredService<IProgressManager>();
        UserManager<IdentityUser> userManager = App.Services.GetRequiredService<UserManager<IdentityUser>>();
        string? userId = (await userManager.FindByNameAsync(Username))?.Id;
        Assert.IsNotNull(userId);
        ProgressDocument? progressDocument = await progressManager.GetAsync(userId, request1.Document, TestContext.CancellationToken);
        Assert.IsNotNull(progressDocument);
        Assert.AreEqual(request2.Progress, progressDocument.Progress);
        Assert.AreEqual(request2.Percentage, progressDocument.Percentage);
        Assert.AreEqual(request2.Device, progressDocument.Device);
        Assert.AreEqual(request2.DeviceId, progressDocument.DeviceId);
    }

    [TestMethod]
    public async Task Test_GetProgressWithoutAuth_Fails()
    {
        IProgressManager progressManager = App.Services.GetRequiredService<IProgressManager>();
        UserManager<IdentityUser> userManager = App.Services.GetRequiredService<UserManager<IdentityUser>>();
        string? userId = (await userManager.FindByNameAsync(Username))?.Id;
        Assert.IsNotNull(userId);
        ProgressDocument progressDocument = new()
        {
            User = userId,
            Hash = "document",
            Progress = "progress",
            Percentage = 0.5,
            Device = "device",
            DeviceId = "device_id",
            Timestamp = DateTimeOffset.UtcNow,
        };
        await progressManager.PutAsync(progressDocument, TestContext.CancellationToken);
        using HttpRequestMessage requestMessage = new()
        {
            RequestUri = new($"syncs/progress/{progressDocument.Hash}", UriKind.Relative),
            Method = HttpMethod.Get,
        };
        using HttpResponseMessage responseMessage = await Client.SendAsync(requestMessage, TestContext.CancellationToken);
        Assert.AreEqual(HttpStatusCode.Unauthorized, responseMessage.StatusCode);
        ErrorResponse? response = await responseMessage.Content.ReadFromJsonAsync(ErrorsJsonContext.Default.ErrorResponse, TestContext.CancellationToken);
        Assert.IsNotNull(response);
        Assert.AreEqual(KoreaderErrors.UnauthorizedUser.Code, response.Code);
        Assert.AreEqual(KoreaderErrors.UnauthorizedUser.Message, response.Message);
    }

    [TestMethod]
    public async Task Test_GetProgressWithWrongPassword_Fails()
    {
        IProgressManager progressManager = App.Services.GetRequiredService<IProgressManager>();
        UserManager<IdentityUser> userManager = App.Services.GetRequiredService<UserManager<IdentityUser>>();
        string? userId = (await userManager.FindByNameAsync(Username))?.Id;
        Assert.IsNotNull(userId);
        ProgressDocument progressDocument = new()
        {
            User = userId,
            Hash = "document",
            Progress = "progress",
            Percentage = 0.5,
            Device = "device",
            DeviceId = "device_id",
            Timestamp = DateTimeOffset.UtcNow,
        };
        await progressManager.PutAsync(progressDocument, TestContext.CancellationToken);
        using HttpRequestMessage requestMessage = new()
        {
            RequestUri = new($"syncs/progress/{progressDocument.Hash}", UriKind.Relative),
            Method = HttpMethod.Get,
            Headers = {
                { KoreaderAuthOptions.UsernameHeader, Username },
                { KoreaderAuthOptions.PasswordHeader, $"Not{Password}" },
            },
        };
        using HttpResponseMessage responseMessage = await Client.SendAsync(requestMessage, TestContext.CancellationToken);
        Assert.AreEqual(HttpStatusCode.Unauthorized, responseMessage.StatusCode);
        ErrorResponse? response = await responseMessage.Content.ReadFromJsonAsync(ErrorsJsonContext.Default.ErrorResponse, TestContext.CancellationToken);
        Assert.IsNotNull(response);
        Assert.AreEqual(KoreaderErrors.UnauthorizedUser.Code, response.Code);
        Assert.AreEqual(KoreaderErrors.UnauthorizedUser.Message, response.Message);
    }

    [TestMethod]
    public async Task Test_GetProgressWithCorrectAuth_Succeeds()
    {
        IProgressManager progressManager = App.Services.GetRequiredService<IProgressManager>();
        UserManager<IdentityUser> userManager = App.Services.GetRequiredService<UserManager<IdentityUser>>();
        string? userId = (await userManager.FindByNameAsync(Username))?.Id;
        Assert.IsNotNull(userId);
        ProgressDocument progressDocument = new()
        {
            User = userId,
            Hash = "document",
            Progress = "progress",
            Percentage = 0.5,
            Device = "device",
            DeviceId = "device_id",
            Timestamp = DateTimeOffset.UtcNow,
        };
        await progressManager.PutAsync(progressDocument, TestContext.CancellationToken);
        using HttpRequestMessage requestMessage = new()
        {
            RequestUri = new($"syncs/progress/{progressDocument.Hash}", UriKind.Relative),
            Method = HttpMethod.Get,
            Headers = {
                { KoreaderAuthOptions.UsernameHeader, Username },
                { KoreaderAuthOptions.PasswordHeader, Password },
            },
        };
        using HttpResponseMessage responseMessage = await Client.SendAsync(requestMessage, TestContext.CancellationToken);
        Assert.AreEqual(HttpStatusCode.OK, responseMessage.StatusCode);
        GetProgressResponse? response = await responseMessage.Content.ReadFromJsonAsync(ProgressJsonContext.Default.GetProgressResponse, TestContext.CancellationToken);
        Assert.IsNotNull(response);
        Assert.AreEqual(progressDocument.Hash, response.Document);
        Assert.AreEqual(progressDocument.Progress, response.Progress);
        Assert.AreEqual(progressDocument.Percentage, response.Percentage);
        Assert.AreEqual(progressDocument.Device, response.Device);
        Assert.AreEqual(progressDocument.DeviceId, response.DeviceId);
        Assert.AreEqual(progressDocument.Timestamp.ToUnixTimeSeconds(), response.Timestamp);
    }

    [TestMethod]
    public async Task Test_GetProgressWithCorrectAuthButNoProgressDocument_SucceedsAndReturnsEmptyObject()
    {
        using HttpRequestMessage requestMessage = new()
        {
            RequestUri = new($"syncs/progress/document", UriKind.Relative),
            Method = HttpMethod.Get,
            Headers = {
                { KoreaderAuthOptions.UsernameHeader, Username },
                { KoreaderAuthOptions.PasswordHeader, Password },
            },
        };
        using HttpResponseMessage responseMessage = await Client.SendAsync(requestMessage, TestContext.CancellationToken);
        Assert.AreEqual(HttpStatusCode.OK, responseMessage.StatusCode);
        await using Stream responseStream = await responseMessage.Content.ReadAsStreamAsync(TestContext.CancellationToken);
        JsonNode? response = await JsonNode.ParseAsync(responseStream, cancellationToken: TestContext.CancellationToken);
        Assert.IsNotNull(response);
        Assert.AreEqual(JsonValueKind.Object, response.GetValueKind());
        Assert.IsEmpty(response.AsObject());
    }

    [TestMethod]
    public async Task Test_DeleteProgressWithCorrectAuth_Succeeds()
    {
        IProgressManager progressManager = App.Services.GetRequiredService<IProgressManager>();
        UserManager<IdentityUser> userManager = App.Services.GetRequiredService<UserManager<IdentityUser>>();
        string? userId = (await userManager.FindByNameAsync(Username))?.Id;
        Assert.IsNotNull(userId);
        ProgressDocument progressDocument = new()
        {
            User = userId,
            Hash = "document",
            Progress = "progress",
            Percentage = 0.5,
            Device = "device",
            DeviceId = "device_id",
            Timestamp = DateTimeOffset.UtcNow,
        };
        await progressManager.PutAsync(progressDocument, TestContext.CancellationToken);
        ProgressDocument? progressDocumentAfterPut = await progressManager.GetAsync(userId, progressDocument.Hash, TestContext.CancellationToken);
        Assert.IsNotNull(progressDocumentAfterPut);
        using HttpRequestMessage requestMessage = new()
        {
            RequestUri = new($"syncs/progress/{progressDocument.Hash}", UriKind.Relative),
            Method = HttpMethod.Delete,
            Headers = {
                { KoreaderAuthOptions.UsernameHeader, Username },
                { KoreaderAuthOptions.PasswordHeader, Password },
            },
        };
        using HttpResponseMessage responseMessage = await Client.SendAsync(requestMessage, TestContext.CancellationToken);
        Assert.AreEqual(HttpStatusCode.OK, responseMessage.StatusCode);
        ProgressDocument? progressDocumentAfterDelete = await progressManager.GetAsync(userId, progressDocument.Hash, TestContext.CancellationToken);
        Assert.IsNull(progressDocumentAfterDelete);
    }

    [TestMethod]
    public async Task Test_DeleteAllProgressWithCorrectAuth_Succeeds()
    {
        IProgressManager progressManager = App.Services.GetRequiredService<IProgressManager>();
        UserManager<IdentityUser> userManager = App.Services.GetRequiredService<UserManager<IdentityUser>>();
        string? userId = (await userManager.FindByNameAsync(Username))?.Id;
        Assert.IsNotNull(userId);
        ProgressDocument progressDocument1 = new()
        {
            User = userId,
            Hash = "document1",
            Progress = "progress",
            Percentage = 0.5,
            Device = "device",
            DeviceId = "device_id",
            Timestamp = DateTimeOffset.UtcNow,
        };
        ProgressDocument progressDocument2 = new()
        {
            User = userId,
            Hash = "document2",
            Progress = "progress",
            Percentage = 0.5,
            Device = "device",
            DeviceId = "device_id",
            Timestamp = DateTimeOffset.UtcNow,
        };
        await progressManager.PutAsync(progressDocument1, TestContext.CancellationToken);
        await progressManager.PutAsync(progressDocument2, TestContext.CancellationToken);
        ProgressDocument? progressDocument1AfterPut = await progressManager.GetAsync(userId, progressDocument1.Hash, TestContext.CancellationToken);
        Assert.IsNotNull(progressDocument1AfterPut);
        ProgressDocument? progressDocument2AfterPut = await progressManager.GetAsync(userId, progressDocument2.Hash, TestContext.CancellationToken);
        Assert.IsNotNull(progressDocument2AfterPut);
        using HttpRequestMessage requestMessage = new()
        {
            RequestUri = new($"syncs/progress", UriKind.Relative),
            Method = HttpMethod.Delete,
            Headers = {
                { KoreaderAuthOptions.UsernameHeader, Username },
                { KoreaderAuthOptions.PasswordHeader, Password },
            },
        };
        using HttpResponseMessage responseMessage = await Client.SendAsync(requestMessage, TestContext.CancellationToken);
        Assert.AreEqual(HttpStatusCode.OK, responseMessage.StatusCode);
        ProgressDocument? progressDocument1AfterDelete = await progressManager.GetAsync(userId, progressDocument1.Hash, TestContext.CancellationToken);
        Assert.IsNull(progressDocument1AfterDelete);
        ProgressDocument? progressDocument2AfterDelete = await progressManager.GetAsync(userId, progressDocument2.Hash, TestContext.CancellationToken);
        Assert.IsNull(progressDocument2AfterDelete);
    }
}
