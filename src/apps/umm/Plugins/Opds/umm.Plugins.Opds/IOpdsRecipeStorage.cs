using System.Threading;
using System.Threading.Tasks;

namespace umm.Plugins.Opds;

internal interface IOpdsRecipeStorage
{
    Task<OpdsRecipe?> GetAsync(string id, CancellationToken cancellationToken = default);
    Task PutAsync(OpdsRecipe recipe, CancellationToken cancellationToken = default);
    Task DeleteAsync(string id, CancellationToken cancellationToken = default);
}
