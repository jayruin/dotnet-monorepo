using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;

namespace umm.Auto.Vendors;

[Generator]
public class MediaVendorInitializationGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        // TODO embedded attribute
        context
            //.AddEmbeddedAttributeDefinition()
            .RegisterPostInitializationOutput(ctx => ctx.AddSource(
                "AutoAddMediaVendorsAttribute.g.cs",
                SourceText.From(AutoAddMediaVendors, new UTF8Encoding())));

        IncrementalValuesProvider<ClassToGenerate?> classToGenerate = context.SyntaxProvider
            .ForAttributeWithMetadataName(
                "umm.Auto.Vendors.AutoAddMediaVendorsAttribute",
                predicate: static (s, _) => s is ClassDeclarationSyntax,
                transform: static (ctx, _) => GetClassToGenerate(ctx));

        IncrementalValueProvider<ImmutableArray<InitializationMethod>> initializationMethods = context.CompilationProvider
            .Select((c, _) => GetInitializationMethods(c))
            .WithComparer(ImmutableArrayEqualityComparer<InitializationMethod>.Instance);

        IncrementalValuesProvider<(ClassToGenerate? Left, ImmutableArray<InitializationMethod> Right)> combined = classToGenerate.Combine(initializationMethods);

        context.RegisterSourceOutput(combined, Execute);
    }

    private const string AutoAddMediaVendors = """
namespace umm.Auto.Vendors;

// TODO embedded attribute
// [Microsoft.CodeAnalysis.Embedded]
[System.AttributeUsage(System.AttributeTargets.Class)]
internal sealed class AutoAddMediaVendorsAttribute : System.Attribute
{
}
""";

    private static ClassToGenerate? GetClassToGenerate(GeneratorAttributeSyntaxContext context)
    {
        foreach (AttributeData attributeData in context.Attributes)
        {
            if (attributeData.AttributeClass?.Name == "AutoAddMediaVendorsAttribute"
                && context.TargetNode is ClassDeclarationSyntax classDeclarationSyntax)
            {
                bool isInternal = false;
                bool isStatic = false;
                bool isPartial = false;
                foreach (SyntaxToken modifier in classDeclarationSyntax.Modifiers)
                {
                    if (modifier.IsKind(SyntaxKind.InternalKeyword))
                    {
                        isInternal = true;
                    }
                    if (modifier.IsKind(SyntaxKind.StaticKeyword))
                    {
                        isStatic = true;
                    }
                    if (modifier.IsKind(SyntaxKind.PartialKeyword))
                    {
                        isPartial = true;
                    }
                }
                if (isInternal && isStatic && isPartial)
                {
                    return new(GetNamespace(classDeclarationSyntax), classDeclarationSyntax.Identifier.ToString());
                }
            }
        }
        return null;
    }

    private static string GetNamespace(BaseTypeDeclarationSyntax baseTypeDeclarationSyntax)
    {
        string ns = "";
        SyntaxNode? parent = baseTypeDeclarationSyntax.Parent;
        while (parent != null && parent is not BaseNamespaceDeclarationSyntax)
        {
            parent = parent.Parent;
        }
        if (parent is BaseNamespaceDeclarationSyntax baseNamespaceDeclarationSyntax)
        {
            ns = baseNamespaceDeclarationSyntax.Name.ToString();
        }
        return ns;
    }

    private static ImmutableArray<InitializationMethod> GetInitializationMethods(Compilation compilation)
    {
        ImmutableArray<InitializationMethod>.Builder builder = ImmutableArray.CreateBuilder<InitializationMethod>();

        INamedTypeSymbol? serviceCollectionTypeSymbol = compilation.GetTypeByMetadataName("Microsoft.Extensions.DependencyInjection.IServiceCollection");
        INamedTypeSymbol? configurationTypeSymbol = compilation.GetTypeByMetadataName("Microsoft.Extensions.Configuration.IConfiguration");
        foreach (IAssemblySymbol assemblySymbol in compilation.SourceModule.ReferencedAssemblySymbols)
        {
            foreach ((INamespaceSymbol namespaceSymbol, string ns) in GetMatchingNamespaces(assemblySymbol, "umm.Vendors"))
            {
                foreach (INamedTypeSymbol namedTypeSymbol in namespaceSymbol.GetTypeMembers())
                {
                    if (!namedTypeSymbol.IsStatic
                        || !namedTypeSymbol.Name.EndsWith("ServiceCollectionExtensions")) continue;
                    foreach (ISymbol symbol in namedTypeSymbol.GetMembers())
                    {
                        if (symbol is not IMethodSymbol methodSymbol
                            || methodSymbol.DeclaredAccessibility != Accessibility.Public
                            || !methodSymbol.IsExtensionMethod
                            || methodSymbol.Parameters.Length != 2
                            || !SymbolEqualityComparer.Default.Equals(methodSymbol.Parameters[0].Type, serviceCollectionTypeSymbol)
                            || !SymbolEqualityComparer.Default.Equals(methodSymbol.Parameters[1].Type, configurationTypeSymbol)) continue;
                        builder.Add(new(ns, namedTypeSymbol.Name, methodSymbol.Name));
                    }
                }
            }
        }
        return builder.ToImmutable();
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

    private static void Execute(SourceProductionContext context, (ClassToGenerate? Left, ImmutableArray<InitializationMethod> Right) combined)
    {
        (ClassToGenerate? classToGenerate, ImmutableArray<InitializationMethod> initializationMethods) = combined;
        if (classToGenerate is null) return;

        StringBuilder stringBuilder = new();
        stringBuilder.Append($$"""
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

""");
        foreach (InitializationMethod initializationMethod in initializationMethods.OrderBy(i => i.Namespace))
        {
            stringBuilder.AppendLine($"using {initializationMethod.Namespace};");
        }
        stringBuilder.Append($$"""

namespace {{classToGenerate.Namespace}};

internal static partial class {{classToGenerate.ClassName}}
{
    public static IServiceCollection AddMediaVendors(this IServiceCollection serviceCollection, IConfiguration configuration)
    {
        return serviceCollection
""");
        foreach (InitializationMethod initializationMethod in initializationMethods.OrderBy(i => i.MethodName))
        {
            stringBuilder.Append($"""

            .{initializationMethod.MethodName}(configuration)
""");
        }
        stringBuilder.Append(";");
        stringBuilder.Append($$"""

    }
}

""");
        context.AddSource($"{classToGenerate.ClassName}.g.cs", stringBuilder.ToString());
    }
}
