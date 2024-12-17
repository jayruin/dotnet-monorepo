namespace EpubProj;

internal sealed class MutableSeries
{
    public required string Name { get; set; }
    public required string Index { get; set; }

    public IEpubProjectSeries ToImmutable() => new EpubProjectSeries()
    {
        Name = Name,
        Index = Index,
    };
}
