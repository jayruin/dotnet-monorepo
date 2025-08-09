using Microsoft.Extensions.DependencyInjection;
using System;

namespace Apps;

internal sealed class DelegateServiceOnlyInitialization : IServiceOnlyInitialization
{
    private readonly Action<IServiceCollection> _initializeServices;

    public DelegateServiceOnlyInitialization(
        Action<IServiceCollection>? initializeServices)
    {
        initializeServices ??= (_) => { };
        _initializeServices = initializeServices;
    }

    public void InitializeServices(IServiceCollection services)
    {
        _initializeServices(services);
    }
}
