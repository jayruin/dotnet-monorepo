namespace ksse.ReadingProgress;

internal sealed class ProgressPurgeOptions
{
    public long ScanPeriodInMinutes { get; set; }
    public long RetentionPeriodInDays { get; set; }
    public int BatchSize { get; set; }
}
