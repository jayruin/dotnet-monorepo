using System.Threading;
using System.Threading.Tasks;

namespace umm.Plugins.Opds;

internal sealed class NullOpdsRecipeStorage : IOpdsRecipeStorage
{
    public Task<OpdsRecipe?> GetAsync(string id, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult<OpdsRecipe?>(null);
    }

    public Task PutAsync(OpdsRecipe recipe, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.CompletedTask;
    }

    public Task DeleteAsync(string id, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.CompletedTask;
    }
}
