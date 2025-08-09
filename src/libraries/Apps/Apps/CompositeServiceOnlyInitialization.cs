using Microsoft.Extensions.DependencyInjection;
using System.Collections.Generic;
using System.Collections.Immutable;

namespace Apps;

internal sealed class CompositeServiceOnlyInitialization : IServiceOnlyInitialization
{
    private readonly ImmutableArray<IServiceOnlyInitialization> _initializations;

    public CompositeServiceOnlyInitialization(IEnumerable<IServiceOnlyInitialization> initializations)
    {
        _initializations = [.. initializations];
    }

    public void InitializeServices(IServiceCollection services)
    {
        foreach (IServiceOnlyInitialization initialization in _initializations)
        {
            initialization.InitializeServices(services);
        }
    }
}
