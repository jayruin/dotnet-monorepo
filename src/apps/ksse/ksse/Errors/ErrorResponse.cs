namespace ksse.Errors;

internal sealed class ErrorResponse
{
    public required int Code { get; init; }
    public required string Message { get; init; }
}
