using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace ksse.ReadingProgress;

internal sealed partial class ProgressPurgeBackgroundService : BackgroundService
{
    private const string Name = nameof(ProgressPurgeBackgroundService);

    private readonly IOptions<ProgressPurgeOptions> _options;
    private readonly TimeProvider _timeProvider;
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<ProgressPurgeBackgroundService> _logger;

    public ProgressPurgeBackgroundService(IOptions<ProgressPurgeOptions> options, TimeProvider timeProvider,
        IServiceProvider serviceProvider,
        ILogger<ProgressPurgeBackgroundService> logger)
    {
        _options = options;
        _timeProvider = timeProvider;
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (_options.Value.ScanPeriodInMinutes <= 0 || _options.Value.RetentionPeriodInDays <= 0) return;
        try
        {
            LogStarted();
            TimeSpan period = TimeSpan.FromMinutes(_options.Value.ScanPeriodInMinutes);
            using PeriodicTimer periodicTimer = new(period, _timeProvider);
            while (await periodicTimer.WaitForNextTickAsync(stoppingToken).ConfigureAwait(false))
            {
                AsyncServiceScope scope = _serviceProvider.CreateAsyncScope();
                await using ConfiguredAsyncDisposable configuredScope = scope.ConfigureAwait(false);
                await PurgeAsync(scope.ServiceProvider, stoppingToken).ConfigureAwait(false);
            }
        }
        catch (Exception exception)
        {
            LogUnexpectedError(exception);
        }
    }

    private async Task PurgeAsync(IServiceProvider serviceProvider, CancellationToken stoppingToken)
    {
        ProgressDbContext dbContext = serviceProvider.GetRequiredService<ProgressDbContext>();
        TimeSpan retention = TimeSpan.FromDays(_options.Value.RetentionPeriodInDays);
        long threshold = _timeProvider.GetUtcNow().Subtract(retention).ToUnixTimeSeconds();
        while (!stoppingToken.IsCancellationRequested)
        {
            IQueryable<ProgressDocument> query = dbContext.ProgressDocuments
                .Where(p => p.Timestamp < threshold)
                .OrderBy(p => p.Timestamp);
            if (_options.Value.BatchSize > 0)
            {
                query = query
                    .Take(_options.Value.BatchSize);
            }
            int rowsDeleted = await query
                .ExecuteDeleteAsync(stoppingToken)
                .ConfigureAwait(false);
            LogRowsDeleted(rowsDeleted);
            if (_options.Value.BatchSize <= 0 || rowsDeleted < _options.Value.BatchSize)
            {
                break;
            }
        }
    }

    [LoggerMessage(LogLevel.Information, $"[{Name}] Started")]
    private partial void LogStarted();

    [LoggerMessage(LogLevel.Error, $"[{Name}] Unexpected error")]
    private partial void LogUnexpectedError(Exception exception);

    [LoggerMessage(LogLevel.Information, $"[{Name}] Deleted {{RowsDeleted}}")]
    private partial void LogRowsDeleted(int rowsDeleted);
}
