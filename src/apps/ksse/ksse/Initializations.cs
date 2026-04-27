using ksse.Auth;
using ksse.Errors;
using ksse.Health;
using ksse.ReadingProgress;
using ksse.Users;
using Logging;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.FeatureManagement;
using System;
using System.Threading.Tasks;

namespace ksse;

internal static class Initializations
{
    public static void InitializeCoreServices(IServiceCollection services, IConfiguration configuration)
    {
        IConfiguration databasesConfiguration = configuration.GetSection("databases");

        IConfiguration usersDatabaseConfiguration = databasesConfiguration.GetSection(UsersDbContext.Id);
        AddDatabase<UsersDbContext>(services, usersDatabaseConfiguration);

        IConfiguration progressDatabaseConfiguration = databasesConfiguration.GetSection(ProgressDbContext.Id);
        AddDatabase<ProgressDbContext>(services, progressDatabaseConfiguration);

        services
            .AddIdentityCore<IdentityUser>()
            .AddRoles<IdentityRole>()
            .AddEntityFrameworkStores<UsersDbContext>();

        services.Configure<IdentityOptions>(options =>
        {
            configuration
                .GetSection(nameof(IdentityOptions))
                .GetSection(nameof(IdentityOptions.Password))
                .Bind(options.Password);
        });

        services.AddScoped<IProgressManager, ProgressManager>();
    }

    public static void InitializeServices(IServiceCollection services, IConfiguration configuration)
    {
        InitializeCoreServices(services, configuration);

        services.ConfigureHttpJsonOptions(options =>
        {
            options.SerializerOptions.TypeInfoResolverChain.Insert(0, UsersJsonContext.Default);
            options.SerializerOptions.TypeInfoResolverChain.Insert(0, ProgressJsonContext.Default);
            options.SerializerOptions.TypeInfoResolverChain.Insert(0, HealthChecksJsonContext.Default);
            options.SerializerOptions.TypeInfoResolverChain.Insert(0, ErrorsJsonContext.Default);
            options.SerializerOptions.PropertyNamingPolicy = ErrorsJsonContext.Default.Options.PropertyNamingPolicy;
        });

        services.AddConfiguredLogging(configuration);

        services
            .AddAuthentication()
            .AddScheme<KoreaderAuthOptions, KoreaderAuthHandler>(KoreaderAuthOptions.DefaultScheme, null);

        services
            .AddAuthorizationBuilder()
            .SetDefaultPolicy(new AuthorizationPolicyBuilder()
                .AddAuthenticationSchemes(KoreaderAuthOptions.DefaultScheme)
                .RequireAuthenticatedUser()
                .Build());

        services.AddFeatureManagement();

        services
            .AddHealthChecks()
            .AddDbContextCheck<UsersDbContext>()
            .AddDbContextCheck<ProgressDbContext>();
    }

    public static void InitializeMiddlewares(IApplicationBuilder applicationBuilder)
    {
        applicationBuilder.UseStatusCodePages(context =>
        {
            if (context.HttpContext.Response.StatusCode != StatusCodes.Status401Unauthorized) return Task.CompletedTask;
            return context.HttpContext.Response.WriteAsJsonAsync(KoreaderErrors.UnauthorizedUser, ErrorsJsonContext.Default.ErrorResponse);
        });
        applicationBuilder.UseAuthentication();
        applicationBuilder.UseAuthorization();
    }

    public static void InitializeEndpoints(IEndpointRouteBuilder endpointRouteBuilder)
    {
        endpointRouteBuilder.MapUsersEndpoints();
        endpointRouteBuilder.MapProgressEndpoints();
        endpointRouteBuilder.MapHealthCheckEndpoints();
        endpointRouteBuilder.MapGet("", () => TypedResults.Ok());
    }

    private static void AddDatabase<TContext>(IServiceCollection services, IConfiguration configuration)
        where TContext : DbContext
    {
        string? databaseProvider = configuration["provider"] ?? string.Empty;
        if (databaseProvider.Equals(SqliteOptions.Id, StringComparison.OrdinalIgnoreCase))
        {
            SqliteOptions? sqliteOptions = configuration.Get<SqliteOptions>();
            services.AddDbContext<TContext>(options =>
            {
                options.UseSqlite(sqliteOptions?.ConnectionString);
            });
        }
        else if (databaseProvider.Equals(PostgreSqlOptions.Id, StringComparison.OrdinalIgnoreCase))
        {
            PostgreSqlOptions? postgreSqlOptions = configuration.Get<PostgreSqlOptions>();
            services.AddDbContext<TContext>(options =>
            {
                options.UseNpgsql(postgreSqlOptions?.ConnectionString);
            });
        }
        else if (databaseProvider.Equals(SqlServerOptions.Id, StringComparison.OrdinalIgnoreCase))
        {
            SqlServerOptions? sqlServerOptions = configuration.Get<SqlServerOptions>();
            services.AddDbContext<TContext>(options =>
            {
                options.UseSqlServer(sqlServerOptions?.ConnectionString);
            });
        }
        else
        {
            InMemoryOptions? inMemoryOptions = configuration.Get<InMemoryOptions>();
            services.AddDbContext<TContext>(options =>
            {
                options.UseInMemoryDatabase(inMemoryOptions?.DatabaseName ?? typeof(TContext).Name);
            });
        }
    }

    internal sealed class SqliteOptions
    {
        public const string Id = "sqlite";
        public string? ConnectionString { get; set; }
    }

    internal sealed class PostgreSqlOptions
    {
        public const string Id = "postgresql";
        public string? ConnectionString { get; set; }
    }

    internal sealed class SqlServerOptions
    {
        public const string Id = "sqlserver";
        public string? ConnectionString { get; set; }
    }

    internal sealed class InMemoryOptions
    {
        public const string Id = "inmemory";
        public string? DatabaseName { get; set; }
    }
}
