using Microsoft.CodeAnalysis;
using System.Collections.Generic;
using System.Collections.Immutable;

namespace SourceGenerators;

public static class AssemblyScanner
{
    public static IEnumerable<(string, INamedTypeSymbol)> GetMatchingTypes(Compilation compilation, string prefix)
    {
        foreach (IAssemblySymbol assemblySymbol in compilation.SourceModule.ReferencedAssemblySymbols)
        {
            foreach ((INamespaceSymbol namespaceSymbol, string ns) in GetMatchingNamespaces(assemblySymbol, prefix))
            {
                foreach (INamedTypeSymbol namedTypeSymbol in namespaceSymbol.GetTypeMembers())
                {
                    yield return (ns, namedTypeSymbol);
                }
            }
        }
    }

    private static IEnumerable<(INamespaceSymbol, string)> GetMatchingNamespaces(IAssemblySymbol assemblySymbol, string prefix)
    {
        ImmutableArray<string> prefixParts = [.. prefix.Split('.')];
        string currentPrefix = assemblySymbol.GlobalNamespace.Name;
        foreach (INamespaceSymbol namespaceSymbol in assemblySymbol.GlobalNamespace.GetNamespaceMembers())
        {
            foreach ((INamespaceSymbol, string) output in GetMatchingNamespaces(namespaceSymbol, currentPrefix, prefixParts))
            {
                yield return output;
            }
        }
    }

    private static IEnumerable<(INamespaceSymbol, string)> GetMatchingNamespaces(INamespaceSymbol namespaceSymbol, string currentPrefix, ImmutableArray<string> prefixParts)
    {
        if (prefixParts.Length > 0 && namespaceSymbol.Name != prefixParts[0]) yield break;
        ImmutableArray<string> childPrefixParts = prefixParts.Length > 0 ? prefixParts[1..] : prefixParts;
        string currentChildPrefix = string.IsNullOrWhiteSpace(currentPrefix)
            ? namespaceSymbol.Name
            : string.Join(".", currentPrefix, namespaceSymbol.Name);
        if (childPrefixParts.Length == 0)
        {
            yield return (namespaceSymbol, currentChildPrefix);
        }
        foreach (INamespaceSymbol childNamespaceSymbol in namespaceSymbol.GetNamespaceMembers())
        {
            foreach ((INamespaceSymbol, string) output in GetMatchingNamespaces(childNamespaceSymbol, currentChildPrefix, childPrefixParts))
            {
                yield return output;
            }
        }
    }
}
