using FileStorage;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Utils;

namespace umm.Plugins.Opds;

internal sealed class JsonFileOpdsRecipeStorage : IOpdsRecipeStorage, IDisposable
{
    private readonly IFile _file;
    private readonly SemaphoreSlim _semaphoreSlim = new(1, 1);
    private readonly Dictionary<string, OpdsRecipe> _recipes;

    public JsonFileOpdsRecipeStorage(IFile file)
    {
        _file = file;
        _recipes = ReadRecipes().ToDictionary(r => r.Id, r => r);
    }

    public void Dispose()
    {
        _semaphoreSlim.Dispose();
    }

    public Task<OpdsRecipe?> GetAsync(string id, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(_recipes.GetValueOrDefault(id));
    }

    public async Task PutAsync(OpdsRecipe recipe, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        using (await _semaphoreSlim.EnterScopeAsync(cancellationToken).ConfigureAwait(false))
        {
            _recipes[recipe.Id] = recipe;
            await WriteRecipesAsync(cancellationToken).ConfigureAwait(false);
        }
    }

    public async Task DeleteAsync(string id, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        using (await _semaphoreSlim.EnterScopeAsync(cancellationToken).ConfigureAwait(false))
        {
            if (!_recipes.ContainsKey(id)) return;
            _recipes.Remove(id);
            await WriteRecipesAsync(cancellationToken).ConfigureAwait(false);
        }
    }

    private ImmutableArray<OpdsRecipe> ReadRecipes()
    {
        if (!_file.Exists()) return [];
        using Stream stream = _file.OpenRead();
        return JsonSerializer.Deserialize(stream, OpdsRecipeJsonContext.Default.ImmutableArrayOpdsRecipe);
    }

    private async Task WriteRecipesAsync(CancellationToken cancellationToken)
    {
        Stream stream = _file.OpenWrite();
        await using (stream.ConfigureAwait(false))
        {
            await JsonSerializer.SerializeAsync(stream, _recipes, OpdsRecipeJsonContext.Default.ImmutableArrayOpdsRecipe, cancellationToken).ConfigureAwait(false);
        }
    }
}
