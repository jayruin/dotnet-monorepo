using Microsoft.Extensions.Logging;
using umm.Storages.Blob;
using umm.Storages.Metadata;
using umm.Storages.Tags;

namespace umm.Vendors.Common;

public sealed class MediaVendorContext
{
    public required string VendorId { get; init; }
    public required IMetadataStorage MetadataStorage { get; init; }
    public required IBlobStorage BlobStorage { get; init; }
    public required ITagsStorage TagsStorage { get; init; }
    public required ILogger Logger { get; init; }
}
