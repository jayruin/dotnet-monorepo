using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Apps;

public interface IAppInitialization
{
    void InitializeConfigurationSources(IConfigurationBuilder configurationBuilder, IConfiguration configuration);
    void InitializeServices(IServiceCollection services, IConfiguration configuration);
}
