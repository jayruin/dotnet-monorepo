namespace Images;

public sealed record ImageGridOptions(
    int? Rows = null,
    int? Columns = null,
    Color? BackgroundColor = null,
    bool Expand = false);
