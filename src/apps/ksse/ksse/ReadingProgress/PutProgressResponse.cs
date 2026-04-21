namespace ksse.ReadingProgress;

internal sealed class PutProgressResponse
{
    public required string Document { get; init; }
    public required long Timestamp { get; init; }
}
