using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using SourceGenerators;
using System;
using System.Collections.Immutable;
using System.Linq;
using System.Text;

namespace umm.Auto.Plugins;

[Generator]
public sealed class PluginGenerator : IIncrementalGenerator
{
    private const string MarkerAttributeNamespace = "umm.Auto.Plugins";
    private const string MarkerAttributeName = "AutoGetPluginsAttribute";

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        MarkerAttribute.AddToContext(context, MarkerAttributeNamespace, MarkerAttributeName);

        IncrementalValuesProvider<ClassInfo?> classToGenerate = context.SyntaxProvider
            .ForAttributeWithMetadataName(
                $"{MarkerAttributeNamespace}.{MarkerAttributeName}",
                predicate: static (s, _) => s is ClassDeclarationSyntax,
                transform: static (ctx, _) => ClassInfo.FindFrom(ctx, MarkerAttributeName));

        IncrementalValueProvider<ImmutableArray<PluginClass>> cliPlugins = context.CompilationProvider
            .Select((c, _) => GetPlugins(c, "ICliPlugin"))
            .WithComparer(ImmutableArrayEqualityComparer<PluginClass>.Instance);

        IncrementalValueProvider<ImmutableArray<PluginClass>> serverPlugins = context.CompilationProvider
            .Select((c, _) => GetPlugins(c, "IServerPlugin"))
            .WithComparer(ImmutableArrayEqualityComparer<PluginClass>.Instance);

        IncrementalValuesProvider<(ClassInfo? Left, (ImmutableArray<PluginClass> Left, ImmutableArray<PluginClass> Right) Right)> combined = classToGenerate.Combine(cliPlugins.Combine(serverPlugins));

        context.RegisterSourceOutput(combined, Execute);
    }

    private static ImmutableArray<PluginClass> GetPlugins(Compilation compilation, string pluginInterfaceName)
    {
        ImmutableArray<PluginClass>.Builder builder = ImmutableArray.CreateBuilder<PluginClass>();

        INamedTypeSymbol cliPluginTypeSymbol = compilation.GetTypeByMetadataName($"umm.Plugins.Abstractions.{pluginInterfaceName}")
            ?? throw new InvalidOperationException("Could not find ICliPlugin.");
        INamedTypeSymbol requiresDynamicCodeAttributeTypeSymbol = compilation.GetTypeByMetadataName("System.Diagnostics.CodeAnalysis.RequiresDynamicCodeAttribute")
            ?? throw new InvalidOperationException("Could not find RequiresDynamicCodeAttribute.");

        foreach ((string ns, INamedTypeSymbol namedTypeSymbol) in AssemblyScanner.GetMatchingTypes(compilation, "umm.Plugins"))
        {
            if (!namedTypeSymbol.ImplementsInterface(cliPluginTypeSymbol)
                || !namedTypeSymbol.HasParameterlessConstructor()) continue;
            bool requiresDynamicCode = namedTypeSymbol.HasAttribute(requiresDynamicCodeAttributeTypeSymbol);
            builder.Add(new(ns, namedTypeSymbol.Name, requiresDynamicCode));
        }

        return builder.ToImmutable();
    }

    private static void Execute(SourceProductionContext context, (ClassInfo? Left, (ImmutableArray<PluginClass> Left, ImmutableArray<PluginClass> Right) Right) combined)
    {
        (ClassInfo? classToGenerate, (ImmutableArray<PluginClass> cliPlugins, ImmutableArray<PluginClass> serverPlugins)) = combined;
        if (classToGenerate is null) return;

        StringBuilder stringBuilder = new();
        stringBuilder.Append($$"""
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using umm.Plugins.Abstractions;

""");
        ImmutableArray<string> usingNamespaces = cliPlugins.Concat(serverPlugins)
            .Select(p => p.Namespace)
            .Distinct()
            .ToImmutableArray();
        foreach (string usingNamespace in usingNamespaces)
        {
            stringBuilder.AppendLine($"using {usingNamespace};");
        }
        stringBuilder.AppendLine($$"""

namespace {{classToGenerate.Namespace}};

internal static partial class {{classToGenerate.ClassName}}
{
""");
        stringBuilder.AppendLine("""
    public static IEnumerable<ICliPlugin> GetCliPlugins()
    {
""");
        foreach (PluginClass cliPlugin in cliPlugins.OrderBy(p => p.ClassName))
        {
            stringBuilder.AppendLine(cliPlugin.RequiresDynamicCode
                ? $$"""
        if (RuntimeFeature.IsDynamicCodeSupported)
        {
            yield return new {{cliPlugin.ClassName}}();
        }
"""
                : $"""
        yield return new {cliPlugin.ClassName}();
""");
        }
        stringBuilder.AppendLine($$"""
        yield break;
    }
""");
        stringBuilder.AppendLine("");
        stringBuilder.Append("""
    public static IEnumerable<IServerPlugin> GetServerPlugins()
    {
""");
        foreach (PluginClass serverPlugin in serverPlugins.OrderBy(p => p.ClassName))
        {
            stringBuilder.Append(serverPlugin.RequiresDynamicCode
                ? $$"""

        if (RuntimeFeature.IsDynamicCodeSupported)
        {
            yield return new {{serverPlugin.ClassName}}();
        }
"""
                : $"""

        yield return new {serverPlugin.ClassName}();
""");
        }
        stringBuilder.AppendLine("""

        yield break;
    }
""");
        stringBuilder.AppendLine("""
}
""");
        context.AddSource($"{classToGenerate.ClassName}.g.cs", stringBuilder.ToString());
    }
}
