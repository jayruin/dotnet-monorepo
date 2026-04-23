using ksse.Auth;
using ksse.Errors;
using ksse.Users;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.TestHost;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;

namespace ksse.Tests;

[TestClass]
public sealed class UsersTests
{
    public TestContext TestContext { get; set; }

    [TestMethod]
    public async Task Test_CreateSingleUser_Succeeds()
    {
        await using WebApplication app = WebApplicationForTesting.Create(new Dictionary<string, string?>()
        {
            { $"FeatureManagement:{UsersFeatures.CreateUsers}", "true" },
            { $"Databases:{UsersDbContext.Id}:Provider", "InMemory" },
            { $"Databases:{UsersDbContext.Id}:DatabaseName", nameof(UsersTests) },
        });
        await app.StartAsync(TestContext.CancellationToken);
        using HttpClient client = app.GetTestClient();
        try
        {
            CreateUserRequest request = new()
            {
                Username = "testuser",
                Password = "Password1!",
            };
            using HttpRequestMessage requestMessage = new()
            {
                RequestUri = new("users/create", UriKind.Relative),
                Method = HttpMethod.Post,
                Content = JsonContent.Create(request, UsersJsonContext.Default.CreateUserRequest),
            };
            using HttpResponseMessage responseMessage = await client.SendAsync(requestMessage, TestContext.CancellationToken);
            Assert.AreEqual(HttpStatusCode.Created, responseMessage.StatusCode);
            CreateUserResponse? response = await responseMessage.Content.ReadFromJsonAsync(UsersJsonContext.Default.CreateUserResponse, TestContext.CancellationToken);
            Assert.IsNotNull(response);
            Assert.AreEqual(request.Username, response.UserName);
        }
        finally
        {
            await app.StopAsync(TestContext.CancellationToken);
        }
    }

    [TestMethod]
    public async Task Test_CreateSameUserTwice_Fails()
    {
        await using WebApplication app = WebApplicationForTesting.Create(new Dictionary<string, string?>()
        {
            { $"FeatureManagement:{UsersFeatures.CreateUsers}", "true" },
            { $"Databases:{UsersDbContext.Id}:Provider", "InMemory" },
            { $"Databases:{UsersDbContext.Id}:DatabaseName", nameof(UsersTests) },
        });
        await app.StartAsync(TestContext.CancellationToken);
        using HttpClient client = app.GetTestClient();
        try
        {
            CreateUserRequest request = new()
            {
                Username = "testuser",
                Password = "Password1!",
            };
            using JsonContent content = JsonContent.Create(request, UsersJsonContext.Default.CreateUserRequest);
            using HttpRequestMessage requestMessage1 = new()
            {
                RequestUri = new("users/create", UriKind.Relative),
                Method = HttpMethod.Post,
                Content = content,
            };
            using HttpResponseMessage responseMessage1 = await client.SendAsync(requestMessage1, TestContext.CancellationToken);
            Assert.AreEqual(HttpStatusCode.Created, responseMessage1.StatusCode);
            CreateUserResponse? response1 = await responseMessage1.Content.ReadFromJsonAsync(UsersJsonContext.Default.CreateUserResponse, TestContext.CancellationToken);
            Assert.IsNotNull(response1);
            Assert.AreEqual(request.Username, response1.UserName);
            using HttpRequestMessage requestMessage2 = new()
            {
                RequestUri = requestMessage1.RequestUri,
                Method = requestMessage1.Method,
                Content = requestMessage1.Content,
            };
            using HttpResponseMessage responseMessage2 = await client.SendAsync(requestMessage2, TestContext.CancellationToken);
            Assert.AreEqual(402, (int)responseMessage2.StatusCode);
            ErrorResponse? response2 = await responseMessage2.Content.ReadFromJsonAsync(ErrorsJsonContext.Default.ErrorResponse, TestContext.CancellationToken);
            Assert.IsNotNull(response2);
            Assert.AreEqual(KoreaderErrors.UserExists.Code, response2.Code);
            Assert.AreEqual(KoreaderErrors.UserExists.Message, response2.Message);

        }
        finally
        {
            await app.StopAsync(TestContext.CancellationToken);
        }
    }

    [TestMethod]
    public async Task Test_CreateUserWhenFeatureIsDisabled_Fails()
    {
        await using WebApplication app = WebApplicationForTesting.Create(new Dictionary<string, string?>()
        {
            { $"FeatureManagement:{UsersFeatures.CreateUsers}", "false" },
            { $"Databases:{UsersDbContext.Id}:Provider", "InMemory" },
            { $"Databases:{UsersDbContext.Id}:DatabaseName", nameof(UsersTests) },
        });
        await app.StartAsync(TestContext.CancellationToken);
        using HttpClient client = app.GetTestClient();
        try
        {
            CreateUserRequest request = new()
            {
                Username = "testuser",
                Password = "Password1!",
            };
            using HttpRequestMessage requestMessage = new()
            {
                RequestUri = new("users/create", UriKind.Relative),
                Method = HttpMethod.Post,
                Content = JsonContent.Create(request, UsersJsonContext.Default.CreateUserRequest),
            };
            using HttpResponseMessage responseMessage = await client.SendAsync(requestMessage, TestContext.CancellationToken);
            Assert.AreEqual(402, (int)responseMessage.StatusCode);
            ErrorResponse? response = await responseMessage.Content.ReadFromJsonAsync(ErrorsJsonContext.Default.ErrorResponse, TestContext.CancellationToken);
            Assert.IsNotNull(response);
            Assert.AreEqual(KoreaderErrors.UserRegistrationDisabled.Code, response.Code);
            Assert.AreEqual(KoreaderErrors.UserRegistrationDisabled.Message, response.Message);
        }
        finally
        {
            await app.StopAsync(TestContext.CancellationToken);
        }
    }

    [TestMethod]
    public async Task Test_AuthWithNoCredentials_Fails()
    {
        await using WebApplication app = WebApplicationForTesting.Create(new Dictionary<string, string?>()
        {
            { $"FeatureManagement:{UsersFeatures.CreateUsers}", "true" },
            { $"Databases:{UsersDbContext.Id}:Provider", "InMemory" },
            { $"Databases:{UsersDbContext.Id}:DatabaseName", nameof(UsersTests) },
        });
        await app.StartAsync(TestContext.CancellationToken);
        using HttpClient client = app.GetTestClient();
        try
        {
            using HttpRequestMessage requestMessage = new()
            {
                RequestUri = new("users/auth", UriKind.Relative),
                Method = HttpMethod.Get,
            };
            using HttpResponseMessage responseMessage = await client.SendAsync(requestMessage, TestContext.CancellationToken);
            Assert.AreEqual(HttpStatusCode.Unauthorized, responseMessage.StatusCode);
            ErrorResponse? response = await responseMessage.Content.ReadFromJsonAsync(ErrorsJsonContext.Default.ErrorResponse, TestContext.CancellationToken);
            Assert.IsNotNull(response);
            Assert.AreEqual(KoreaderErrors.UnauthorizedUser.Code, response.Code);
            Assert.AreEqual(KoreaderErrors.UnauthorizedUser.Message, response.Message);
        }
        finally
        {
            await app.StopAsync(TestContext.CancellationToken);
        }
    }

    [TestMethod]
    public async Task Test_AuthNonExistentUser_Fails()
    {
        await using WebApplication app = WebApplicationForTesting.Create(new Dictionary<string, string?>()
        {
            { $"FeatureManagement:{UsersFeatures.CreateUsers}", "true" },
            { $"Databases:{UsersDbContext.Id}:Provider", "InMemory" },
            { $"Databases:{UsersDbContext.Id}:DatabaseName", nameof(UsersTests) },
        });
        await app.StartAsync(TestContext.CancellationToken);
        using HttpClient client = app.GetTestClient();
        try
        {
            using HttpRequestMessage requestMessage = new()
            {
                RequestUri = new("users/auth", UriKind.Relative),
                Method = HttpMethod.Get,
                Headers = {
                    { KoreaderAuthOptions.UsernameHeader, "testuser" },
                    { KoreaderAuthOptions.PasswordHeader, "Password1!" },
                },
            };
            using HttpResponseMessage responseMessage = await client.SendAsync(requestMessage, TestContext.CancellationToken);
            Assert.AreEqual(HttpStatusCode.Unauthorized, responseMessage.StatusCode);
            ErrorResponse? response = await responseMessage.Content.ReadFromJsonAsync(ErrorsJsonContext.Default.ErrorResponse, TestContext.CancellationToken);
            Assert.IsNotNull(response);
            Assert.AreEqual(KoreaderErrors.UnauthorizedUser.Code, response.Code);
            Assert.AreEqual(KoreaderErrors.UnauthorizedUser.Message, response.Message);
        }
        finally
        {
            await app.StopAsync(TestContext.CancellationToken);
        }
    }

    [TestMethod]
    public async Task Test_AuthUserWithWrongPassword_Fails()
    {
        await using WebApplication app = WebApplicationForTesting.Create(new Dictionary<string, string?>()
        {
            { $"FeatureManagement:{UsersFeatures.CreateUsers}", "true" },
            { $"Databases:{UsersDbContext.Id}:Provider", "InMemory" },
            { $"Databases:{UsersDbContext.Id}:DatabaseName", nameof(UsersTests) },
        });
        await app.StartAsync(TestContext.CancellationToken);
        using HttpClient client = app.GetTestClient();
        try
        {
            string username = "testuser";
            string password = "Password1!";
            CreateUserRequest request1 = new()
            {
                Username = username,
                Password = password,
            };
            using HttpResponseMessage responseMessage1 = await client.PostAsJsonAsync("users/create", request1, UsersJsonContext.Default.CreateUserRequest, TestContext.CancellationToken);
            Assert.AreEqual(HttpStatusCode.Created, responseMessage1.StatusCode);
            CreateUserResponse? response1 = await responseMessage1.Content.ReadFromJsonAsync(UsersJsonContext.Default.CreateUserResponse, TestContext.CancellationToken);
            Assert.IsNotNull(response1);
            Assert.AreEqual(request1.Username, response1.UserName);
            using HttpRequestMessage requestMessage2 = new()
            {
                RequestUri = new("users/auth", UriKind.Relative),
                Method = HttpMethod.Get,
                Headers = {
                    { KoreaderAuthOptions.UsernameHeader, username },
                    { KoreaderAuthOptions.PasswordHeader, $"Not{password}" },
                },
            };
            using HttpResponseMessage responseMessage2 = await client.SendAsync(requestMessage2, TestContext.CancellationToken);
            Assert.AreEqual(HttpStatusCode.Unauthorized, responseMessage2.StatusCode);
            ErrorResponse? response2 = await responseMessage2.Content.ReadFromJsonAsync(ErrorsJsonContext.Default.ErrorResponse, TestContext.CancellationToken);
            Assert.IsNotNull(response2);
            Assert.AreEqual(KoreaderErrors.UnauthorizedUser.Code, response2.Code);
            Assert.AreEqual(KoreaderErrors.UnauthorizedUser.Message, response2.Message);
        }
        finally
        {
            await app.StopAsync(TestContext.CancellationToken);
        }
    }

    [TestMethod]
    public async Task Test_AuthUserWithCorrectAuth_Succeeds()
    {
        await using WebApplication app = WebApplicationForTesting.Create(new Dictionary<string, string?>()
        {
            { $"FeatureManagement:{UsersFeatures.CreateUsers}", "true" },
            { $"Databases:{UsersDbContext.Id}:Provider", "InMemory" },
            { $"Databases:{UsersDbContext.Id}:DatabaseName", nameof(UsersTests) },
        });
        await app.StartAsync(TestContext.CancellationToken);
        using HttpClient client = app.GetTestClient();
        try
        {
            string username = "testuser";
            string password = "Password1!";
            using HttpResponseMessage responseMessage1 = await client.PostAsJsonAsync("users/create", new CreateUserRequest()
            {
                Username = username,
                Password = password,
            }, UsersJsonContext.Default.CreateUserRequest, TestContext.CancellationToken);
            Assert.AreEqual(HttpStatusCode.Created, responseMessage1.StatusCode);
            using HttpRequestMessage requestMessage2 = new()
            {
                RequestUri = new("users/auth", UriKind.Relative),
                Method = HttpMethod.Get,
                Headers = {
                    { KoreaderAuthOptions.UsernameHeader, username },
                    { KoreaderAuthOptions.PasswordHeader, password },
                },
            };
            using HttpResponseMessage responseMessage2 = await client.SendAsync(requestMessage2, TestContext.CancellationToken);
            Assert.AreEqual(HttpStatusCode.OK, responseMessage2.StatusCode);
            AuthUserResponse? response2 = await responseMessage2.Content.ReadFromJsonAsync(UsersJsonContext.Default.AuthUserResponse, TestContext.CancellationToken);
            Assert.IsNotNull(response2);
            Assert.AreEqual(AuthUserResponse.Ok.Authorized, response2.Authorized);
        }
        finally
        {
            await app.StopAsync(TestContext.CancellationToken);
        }
    }
}
