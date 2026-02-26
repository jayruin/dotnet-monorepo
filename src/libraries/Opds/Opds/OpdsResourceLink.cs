namespace Opds;

public sealed class OpdsResourceLink
{
    public required string Href { get; init; }
    public required string Type { get; init; }
    public string? Title { get; init; }
}
