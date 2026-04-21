namespace ksse.ReadingProgress;

internal sealed class PutProgressRequest
{
    public required string Document { get; init; }
    public required string Progress { get; init; }
    public required double Percentage { get; init; }
    public required string Device { get; init; }
    public required string DeviceId { get; init; }
}
