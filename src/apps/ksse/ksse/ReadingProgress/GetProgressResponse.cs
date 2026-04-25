namespace ksse.ReadingProgress;

internal sealed class GetProgressResponse
{
    public required string Document { get; init; }
    public required string Progress { get; init; }
    public required double Percentage { get; init; }
    public required string Device { get; init; }
    public required string DeviceId { get; init; }
    public required long Timestamp { get; init; }

    public static GetProgressResponse FromProgressDocument(ProgressDocument progressDocument) => new()
    {
        Document = progressDocument.Hash,
        Progress = progressDocument.Progress,
        Percentage = progressDocument.Percentage,
        Device = progressDocument.Device,
        DeviceId = progressDocument.DeviceId,
        Timestamp = progressDocument.Timestamp.ToUnixTimeSeconds(),
    };
}
