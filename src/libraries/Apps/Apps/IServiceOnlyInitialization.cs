using Microsoft.Extensions.DependencyInjection;

namespace Apps;

public interface IServiceOnlyInitialization
{
    void InitializeServices(IServiceCollection services);
}
