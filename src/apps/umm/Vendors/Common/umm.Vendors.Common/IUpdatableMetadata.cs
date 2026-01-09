using System.Collections.Generic;

namespace umm.Vendors.Common;

public interface IUpdatableMetadata<TSelf>
    where TSelf : IUpdatableMetadata<TSelf>
{
    string ContentId { get; }
    string? LastUpdated { get; }
    IReadOnlyCollection<MetadataPropertyChange> GetChanges(TSelf other);
}
