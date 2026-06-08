using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace ksse.Health;

internal sealed partial class DatabaseHeartbeatBackgroundService<TContext> : BackgroundService
    where TContext : DbContext
{
    private static readonly string Name = $"{nameof(DatabaseHeartbeatBackgroundService<>)}<{typeof(TContext).Name}>";

    private readonly IOptions<DatabaseHeartbeatOptions> _options;
    private readonly TimeProvider _timeProvider;
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<DatabaseHeartbeatBackgroundService<TContext>> _logger;

    public DatabaseHeartbeatBackgroundService(IOptions<DatabaseHeartbeatOptions> options, TimeProvider timeProvider,
        IServiceProvider serviceProvider,
        ILogger<DatabaseHeartbeatBackgroundService<TContext>> logger)
    {
        _options = options;
        _timeProvider = timeProvider;
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (_options.Value.TimePeriodInMinutes <= 0) return;
        try
        {
            LogStarted();
            TimeSpan period = TimeSpan.FromMinutes(_options.Value.TimePeriodInMinutes);
            using PeriodicTimer periodicTimer = new(period, _timeProvider);
            while (await periodicTimer.WaitForNextTickAsync(stoppingToken).ConfigureAwait(false))
            {
                await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);
                AsyncServiceScope scope = _serviceProvider.CreateAsyncScope();
                await using ConfiguredAsyncDisposable configuredScope = scope.ConfigureAwait(false);
                await CheckDatabaseAsync(scope.ServiceProvider, stoppingToken).ConfigureAwait(false);
            }
        }
        catch (Exception exception)
        {
            LogUnexpectedError(exception);
        }
    }

    private async Task CheckDatabaseAsync(IServiceProvider serviceProvider, CancellationToken cancellationToken)
    {
        TContext dbContext = serviceProvider.GetRequiredService<TContext>();
        await using ConfiguredAsyncDisposable configuredDbContext = dbContext.ConfigureAwait(false);
        bool canConnect = await dbContext.Database.CanConnectAsync(cancellationToken).ConfigureAwait(false);
        LogCanConnect(canConnect);
    }

    [LoggerMessage(LogLevel.Information, "[{Name}] Started")]
    private partial void LogStarted(string name);
    private void LogStarted() => LogStarted(Name);

    [LoggerMessage(LogLevel.Information, "[{Name}] CanConnect={CanConnect}")]
    private partial void LogCanConnect(string name, bool canConnect);
    private void LogCanConnect(bool canConnect) => LogCanConnect(Name, canConnect);

    [LoggerMessage(LogLevel.Error, "[{Name}] Unexpected error")]
    private partial void LogUnexpectedError(string name, Exception exception);
    private void LogUnexpectedError(Exception exception) => LogUnexpectedError(Name, exception);
}
