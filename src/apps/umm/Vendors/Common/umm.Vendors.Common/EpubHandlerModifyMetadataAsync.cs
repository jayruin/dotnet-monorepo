using Epubs;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace umm.Vendors.Common;

public delegate Task<IReadOnlyCollection<MetadataPropertyChange>> EpubHandlerModifyMetadataAsync(
    EpubContainer container, string contentId, IEpubMetadata epubMetadata, CancellationToken cancellationToken);
