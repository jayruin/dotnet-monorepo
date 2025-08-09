using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Routing;

namespace Apps;

public interface IWebAppInitialization : IAppInitialization
{
    void InitializeWebHost(IWebHostBuilder webHostBuilder);
    void InitializeMiddlewares(IApplicationBuilder applicationBuilder);
    void InitializeEndpoints(IEndpointRouteBuilder endpointRouteBuilder);
}
