namespace umm.Library;

public closed class SingleTargetSearchTerm : SearchTerm
{
    public required SearchTerm Target { get; init; }
}
