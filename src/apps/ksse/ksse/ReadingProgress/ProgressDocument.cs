using System;

namespace ksse.ReadingProgress;

internal sealed class ProgressDocument
{
    public required string User { get; init; }
    public required string Hash { get; init; }
    public required string Progress { get; init; }
    public required double Percentage { get; init; }
    public required string Device { get; init; }
    public required string DeviceId { get; init; }
    public required DateTimeOffset Timestamp { get; init; }
}
