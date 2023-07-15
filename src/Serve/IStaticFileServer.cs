using Microsoft.AspNetCore.Http;
using System.Threading.Tasks;

namespace Serve;

public interface IStaticFileServer
{
    Task<IResult> HandleBrowseAsync(string subPath);

    Task<IResult> HandleDownloadAsync(string subPath);
}