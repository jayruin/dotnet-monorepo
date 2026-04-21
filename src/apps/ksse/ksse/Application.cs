using Apps;
using ksse.ReadingProgress;
using ksse.Users;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.CommandLine;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace ksse;

internal static class Application
{
    public static RootCommand CreateRootCommand()
    {
        RootCommand rootCommand = new("Koreader Sync Server Extended")
        {
            CreateServeCommand(),
            CreateDeleteDatabasesCommand(),
            CreateCreateDatabasesCommand(),
            CreateCreateUserCommand(),
        };
        return rootCommand;
    }

    private static Command CreateServeCommand()
    {
        Option<IEnumerable<string>> urlsOption = new("--urls")
        {
            DefaultValueFactory = _ => [],
        };
        Command command = new("serve")
        {
            urlsOption,
        };
        command.SetAction(parseResult => HandleServeCommandAsync(
            parseResult.GetRequiredValue(urlsOption)));
        return command;
    }

    private static Task HandleServeCommandAsync(IEnumerable<string> urls)
    {
        IWebAppInitialization initialization = Initialization.Combine(
            [
                Initialization.CreateWebAppInitialization(
                    initializeServices: Initializations.InitializeServices,
                    initializeMiddlewares: Initializations.InitializeMiddlewares,
                    initializeEndpoints: Initializations.InitializeEndpoints)
            ]);
        // TODO cancellation token
        return CliEndpoint.RunWebApplicationAsync(initialization, urls);
    }

    private static Command CreateDeleteDatabasesCommand()
    {
        Argument<IEnumerable<string>> databasesArgument = CreateDatabasesArgument();
        Command command = new("delete-databases", "Delete databases")
        {
            databasesArgument,
        };
        command.SetAction((parseResult, cancellationToken) => CliEndpoint.ExecuteAsync(
            sp => HandleDeleteDatabasesCommandAsync(sp,
                parseResult.GetRequiredValue(databasesArgument),
                cancellationToken),
            initializeServices: Initializations.InitializeCoreServices));
        return command;
    }

    private static async Task HandleDeleteDatabasesCommandAsync(IServiceProvider serviceProvider,
        IEnumerable<string> databases,
        CancellationToken cancellationToken)
    {
        HashSet<string> databasesSet = new(databases, StringComparer.OrdinalIgnoreCase);
        if (databasesSet.Contains(UsersDbContext.Id))
        {
            await using UsersDbContext dbContext = serviceProvider.GetRequiredService<UsersDbContext>();
            await DeleteDbContextDatabaseAsync(dbContext, cancellationToken);
        }
        if (databasesSet.Contains(ProgressDbContext.Id))
        {
            await using ProgressDbContext dbContext = serviceProvider.GetRequiredService<ProgressDbContext>();
            await DeleteDbContextDatabaseAsync(dbContext, cancellationToken);
        }
    }

    private static Command CreateCreateDatabasesCommand()
    {
        Argument<IEnumerable<string>> databasesArgument = CreateDatabasesArgument();
        Command command = new("create-databases", "Create databases")
        {
            databasesArgument,
        };
        command.SetAction((parseResult, cancellationToken) => CliEndpoint.ExecuteAsync(
            sp => HandleCreateDatabasesCommandAsync(sp,
                parseResult.GetRequiredValue(databasesArgument),
                cancellationToken),
            initializeServices: Initializations.InitializeCoreServices));
        return command;
    }

    private static async Task HandleCreateDatabasesCommandAsync(IServiceProvider serviceProvider,
        IEnumerable<string> databases,
        CancellationToken cancellationToken)
    {
        HashSet<string> databasesSet = new(databases, StringComparer.OrdinalIgnoreCase);
        if (databasesSet.Contains(UsersDbContext.Id))
        {
            await using UsersDbContext dbContext = serviceProvider.GetRequiredService<UsersDbContext>();
            await CreateDbContextDatabaseAsync(dbContext, cancellationToken);
        }
        if (databasesSet.Contains(ProgressDbContext.Id))
        {
            await using ProgressDbContext dbContext = serviceProvider.GetRequiredService<ProgressDbContext>();
            await CreateDbContextDatabaseAsync(dbContext, cancellationToken);
        }
    }

    private static async Task DeleteDbContextDatabaseAsync(DbContext dbContext, CancellationToken cancellationToken)
    {
        await dbContext.Database.EnsureDeletedAsync(cancellationToken);
    }

    private static async Task CreateDbContextDatabaseAsync(DbContext dbContext, CancellationToken cancellationToken)
    {
        IDatabaseCreator databaseCreator = dbContext.GetService<IDatabaseCreator>();
        if (databaseCreator is IRelationalDatabaseCreator relationalDatabaseCreator)
        {
            if (await relationalDatabaseCreator.CanConnectAsync(cancellationToken) && await relationalDatabaseCreator.HasTablesAsync(cancellationToken))
            {
                await relationalDatabaseCreator.CreateTablesAsync(cancellationToken);
            }
            else
            {
                await databaseCreator.EnsureCreatedAsync(cancellationToken);
            }
        }
        else
        {
            await databaseCreator.EnsureCreatedAsync(cancellationToken);
        }
    }

    private static Argument<IEnumerable<string>> CreateDatabasesArgument()
    {
        Argument<IEnumerable<string>> databasesArgument = new("databases");
        databasesArgument.AcceptOnlyFromAmong(UsersDbContext.Id, ProgressDbContext.Id);
        return databasesArgument;
    }

    private static Command CreateCreateUserCommand()
    {
        Argument<string> userNameArgument = new("userName");
        Argument<string> passwordArgument = new("password");
        Command command = new("create-user", "Create User")
        {
            userNameArgument,
            passwordArgument,
        };
        command.SetAction((parseResult, cancellationToken) => CliEndpoint.ExecuteAsync(
            sp => HandleCreateUserCommandAsync(sp,
                parseResult.GetRequiredValue(userNameArgument),
                parseResult.GetRequiredValue(passwordArgument),
                cancellationToken),
            initializeServices: Initializations.InitializeCoreServices));
        return command;
    }

    private static async Task HandleCreateUserCommandAsync(IServiceProvider serviceProvider,
        string userName, string password,
        CancellationToken cancellationToken)
    {
        using UserManager<IdentityUser> userManager = serviceProvider.GetRequiredService<UserManager<IdentityUser>>();
        IdentityUser user = new()
        {
            UserName = userName,
        };
        cancellationToken.ThrowIfCancellationRequested();
        IdentityResult identityResult = await userManager.CreateAsync(user, password);
        if (!identityResult.Succeeded)
        {
            throw new InvalidOperationException(string.Join(',', identityResult.Errors.Select(e => $"{e.Code}:{e.Description}")));
        }
    }
}
