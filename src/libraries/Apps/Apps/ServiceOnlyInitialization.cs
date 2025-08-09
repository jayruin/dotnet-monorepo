using Microsoft.Extensions.DependencyInjection;
using System;

namespace Apps;

internal sealed class ServiceOnlyInitialization : IServiceOnlyInitialization
{
    private readonly Action<IServiceCollection> _initializeServices;

    public ServiceOnlyInitialization(
        Action<IServiceCollection> initializeServices)
    {
        _initializeServices = initializeServices;
    }

    public void InitializeServices(IServiceCollection services)
    {
        _initializeServices(services);
    }
}
