using Microsoft.AspNetCore.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Serve;

public interface IStaticFileServer
{
    Task<IResult> HandleBrowseAsync(string subPath, CancellationToken cancellationToken);
    Task<IResult> HandleDownloadAsync(string subPath, CancellationToken cancellationToken);
}
