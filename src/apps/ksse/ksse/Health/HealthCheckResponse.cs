namespace ksse.Health;

internal sealed class HealthCheckResponse
{
    public required string State { get; init; }

    public static HealthCheckResponse Ok => new()
    {
        State = "OK",
    };
}
