using System.Collections.Immutable;
using umm.Library;

namespace umm.Vendors.Common;

public interface ISearchableMetadata
{
    ImmutableArray<MetadataSearchField> GetSearchFields();
}
