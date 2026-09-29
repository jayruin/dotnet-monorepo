namespace ksse.Errors;

internal sealed class Error
{
    public required ErrorResponse Response { get; init; }
    public required int HttpStatusCode { get; init; }
}
