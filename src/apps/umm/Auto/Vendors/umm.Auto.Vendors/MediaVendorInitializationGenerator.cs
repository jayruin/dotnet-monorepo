using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using SourceGenerators;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;

namespace umm.Auto.Vendors;

[Generator]
public class MediaVendorInitializationGenerator : IIncrementalGenerator
{
    private const string MarkerAttributeNamespace = "umm.Auto.Vendors";
    private const string MarkerAttributeName = "AutoAddMediaVendorsAttribute";

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        MarkerAttribute.AddToContext(context, MarkerAttributeNamespace, MarkerAttributeName);

        IncrementalValuesProvider<ClassInfo?> classToGenerate = context.SyntaxProvider
            .ForAttributeWithMetadataName(
                $"{MarkerAttributeNamespace}.{MarkerAttributeName}",
                predicate: static (s, _) => s is ClassDeclarationSyntax,
                transform: static (ctx, _) => ClassInfo.FindFrom(ctx, MarkerAttributeName));

        IncrementalValueProvider<ImmutableArray<InitializationMethod>> initializationMethods = context.CompilationProvider
            .Select((c, _) => GetInitializationMethods(c))
            .WithComparer(ImmutableArrayEqualityComparer<InitializationMethod>.Instance);

        IncrementalValuesProvider<(ClassInfo? Left, ImmutableArray<InitializationMethod> Right)> combined = classToGenerate.Combine(initializationMethods);

        context.RegisterSourceOutput(combined, Execute);
    }

    private static ImmutableArray<InitializationMethod> GetInitializationMethods(Compilation compilation)
    {
        ImmutableArray<InitializationMethod>.Builder builder = ImmutableArray.CreateBuilder<InitializationMethod>();

        INamedTypeSymbol? serviceCollectionTypeSymbol = compilation.GetTypeByMetadataName("Microsoft.Extensions.DependencyInjection.IServiceCollection");
        INamedTypeSymbol? configurationTypeSymbol = compilation.GetTypeByMetadataName("Microsoft.Extensions.Configuration.IConfiguration");

        foreach ((string ns, INamedTypeSymbol namedTypeSymbol) in AssemblyScanner.GetMatchingTypes(compilation, "umm.Vendors"))
        {
            if (!namedTypeSymbol.IsStatic || !namedTypeSymbol.Name.EndsWith("ServiceCollectionExtensions")) continue;
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

        return builder.ToImmutable();
    }

    private static void Execute(SourceProductionContext context, (ClassInfo? Left, ImmutableArray<InitializationMethod> Right) combined)
    {
        (ClassInfo? classToGenerate, ImmutableArray<InitializationMethod> initializationMethods) = combined;
        if (classToGenerate is null) return;

        StringBuilder stringBuilder = new();
        stringBuilder.Append("""
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
        stringBuilder.AppendLine("""

    }
}
""");
        context.AddSource($"{classToGenerate.ClassName}.g.cs", stringBuilder.ToString());
    }
}
