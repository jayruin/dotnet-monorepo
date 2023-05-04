using FileStorage;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace PdfEdit;

public static class Loader
{
    public static Recipe LoadRecipe(IFile jsonFile, IDirectory rootDirectory)
    {
        using Stream stream = jsonFile.OpenRead();
        RecipeJson recipeJson = JsonSerializer.Deserialize(stream, JsonContext.Default.RecipeJson) ?? throw new JsonException();
        return new Recipe
        {
            Pdfs = recipeJson.Pdfs.Select(rootDirectory.GetDirectory).ToImmutableArray(),
            Passwords = recipeJson.Passwords.Select(rootDirectory.GetFile).ToImmutableArray(),
            Titles = recipeJson.Titles.Select(rootDirectory.GetFile).ToImmutableArray(),
            Tocs = recipeJson.Tocs.Select(rootDirectory.GetDirectory).ToImmutableArray(),
            Filters = recipeJson.Filters.Select(f => new Filter
            {
                Width = f.Width,
                Height = f.Height,
                Ids = f.Ids.ToImmutableArray(),
            }).ToImmutableArray(),
            Groups = recipeJson.Groups.Select(g => new Group
            {
                Text = g.Text,
                Cover = rootDirectory.GetFile(g.Cover),
                Content = g.Content.ToImmutableArray(),
            }).ToImmutableArray(),
        };
    }

    public static IImmutableDictionary<string, string> LoadMapping(ImmutableArray<IFile> files)
    {
        var builder = ImmutableDictionary.CreateBuilder<string, string>();
        foreach (IFile file in files)
        {
            using Stream stream = file.OpenRead();
            Dictionary<string, string> data = JsonSerializer.Deserialize(stream, JsonContext.Default.DictionaryStringString)
                ?? throw new JsonException();
            foreach ((string id, string value) in data)
            {
                builder[id] = value;
            }
        }
        return builder.ToImmutable();
    }

    public static IImmutableDictionary<string, ImmutableArray<TocNode>> LoadTocs(ImmutableArray<IDirectory> directories)
    {
        var builder = ImmutableDictionary.CreateBuilder<string, ImmutableArray<TocNode>>();
        foreach (IDirectory directory in directories)
        {
            foreach (IFile file in directory.EnumerateFiles())
            {
                try
                {
                    using Stream stream = file.OpenRead();
                    List<TocNodeJson> toc = JsonSerializer.Deserialize(stream, JsonContext.Default.ListTocNodeJson) ?? throw new JsonException();
                    builder[file.Stem] = toc.Select(n => n.ToImmutable()).ToImmutableArray();
                }
                catch (Exception ex) when (ex is FileStorageException || ex is JsonException) { }
            }
        }
        return builder.ToImmutable();
    }

    public static IImmutableDictionary<string, IFile> LoadPdfs(ImmutableArray<IDirectory> directories)
    {
        var builder = ImmutableDictionary.CreateBuilder<string, IFile>();
        foreach (IDirectory directory in directories)
        {
            foreach (IFile file in directory.EnumerateFiles())
            {
                if (file.Extension == ".pdf")
                {
                    builder[file.Stem] = file;
                }
            }
        }
        return builder.ToImmutable();
    }
}
