namespace umm.Vendors.Common;

public static class MediaIdentifierExtensions
{
    extension<TSelf>(IMediaIdentifier<TSelf> mediaIdentifier)
        where TSelf : IMediaIdentifier<TSelf>
    {
        public static TSelf? Parse(string? s) => TSelf.TryParse(s, out TSelf? result) ? result : default;
    }
}
