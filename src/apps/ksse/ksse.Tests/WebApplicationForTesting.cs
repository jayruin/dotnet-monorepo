using ksse.ReadingProgress;
using ksse.Users;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System.Collections.Generic;

namespace ksse.Tests;

internal static class WebApplicationForTesting
{
    public static WebApplication Create(IReadOnlyDictionary<string, string?>? inMemoryConfiguration = null)
    {
        WebApplicationBuilder builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        if (inMemoryConfiguration is not null)
        {
            builder.Configuration.AddInMemoryCollection(inMemoryConfiguration);
        }
        Initializations.InitializeServices(builder.Services, builder.Configuration);
        WebApplication app = builder.Build();
        Initializations.InitializeMiddlewares(app);
        Initializations.InitializeEndpoints(app);
        app.Services.GetRequiredService<UsersDbContext>().Database.EnsureDeleted();
        app.Services.GetRequiredService<ProgressDbContext>().Database.EnsureDeleted();
        return app;
    }
}
