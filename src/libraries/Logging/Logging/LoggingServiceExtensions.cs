using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using OpenTelemetry.Exporter;
using OpenTelemetry.Logs;
using Serilog;
using Serilog.Extensions.Logging;
using System;
using System.Text;
using Utils;

namespace Logging;

public static class LoggingServiceExtensions
{
    public static IServiceCollection AddConfiguredLogging(this IServiceCollection serviceCollection, IConfiguration configuration)
    {
        IConfiguration loggingConfiguration = configuration.GetSection("logger");
        LoggingOptions? loggingOptions = loggingConfiguration.Get<LoggingOptions>();
        if (loggingOptions is null) return serviceCollection;
        string otlpLoggingOptionsName = "otlp-logging";
        IConfigurationSection otlpLoggingConfigurationSection = loggingConfiguration.GetSection("otlp");
        bool addOtlp = otlpLoggingConfigurationSection.Exists();
        if (addOtlp)
        {
            serviceCollection.Configure<OtlpExporterOptions>(otlpLoggingOptionsName, otlpLoggingConfigurationSection);
        }
        serviceCollection.AddLogging(builder =>
        {
            builder.SetMinimumLevel(loggingOptions.Level);
            if (!loggingOptions.Verbose)
            {
                builder
                    .AddFilter("Microsoft", LogLevel.None)
                    .AddFilter("System", LogLevel.None);
            }
            if (addOtlp)
            {
                builder.AddOpenTelemetry(otel => otel.AddOtlpExporter(otlpLoggingOptionsName, configure: null));
            }
            if (loggingOptions.Console)
            {
                Console.OutputEncoding = new UTF8Encoding();
                builder.AddSimpleConsole(options =>
                {
                    options.UseUtcTimestamp = true;
                    options.SingleLine = true;
                    options.TimestampFormat = $"{DateTimeFormatting.Iso8601} ";
                    options.IncludeScopes = true;
                });
            }
        });
        if (!string.IsNullOrWhiteSpace(loggingOptions.File))
        {
            serviceCollection.AddSerilog((loggerConfiguration) =>
            {
                const string outputTemplate = "[{Timestamp:u}] [{Level}] [{EventId}] {Message:lj}{NewLine}{Exception}";
                if (!loggingOptions.Verbose)
                {
                    loggerConfiguration
                        .MinimumLevel.Override("Microsoft", LevelConvert.ToSerilogLevel(LogLevel.None))
                        .MinimumLevel.Override("System", LevelConvert.ToSerilogLevel(LogLevel.None));
                }
                loggerConfiguration
                    .MinimumLevel.Is(LevelConvert.ToSerilogLevel(loggingOptions.Level))
                    .WriteTo.File(loggingOptions.File, outputTemplate: outputTemplate, shared: true)
                    .Enrich.FromLogContext();
            }, writeToProviders: true);
        }
        return serviceCollection;
    }
}
