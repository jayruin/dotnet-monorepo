using Microsoft.Extensions.Logging;

namespace Logging;

public sealed class LoggingOptions
{
    public bool Verbose { get; set; }

    public LogLevel Level { get; set; } = LogLevel.Information;

    public bool Console { get; set; }

    public string? File { get; set; }
}
