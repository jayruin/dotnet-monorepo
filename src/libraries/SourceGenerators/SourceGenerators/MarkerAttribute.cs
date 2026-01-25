using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using System.Text;

namespace SourceGenerators;

public static class MarkerAttribute
{
    public static void AddToContext(IncrementalGeneratorInitializationContext context,
        string attributeNamespace, string attributeName)
    {
        context
            .RegisterPostInitializationOutput(ctx =>
            {
                ctx.AddEmbeddedAttributeDefinition();
                ctx.AddSource(
                    $"{attributeName}.g.cs",
                    SourceText.From(GetSourceText(attributeNamespace, attributeName), new UTF8Encoding()));
            });
    }

    private static string GetSourceText(string attributeNamespace, string attributeName) => $$"""
namespace {{attributeNamespace}};

[Microsoft.CodeAnalysis.Embedded]
[System.AttributeUsage(System.AttributeTargets.Class)]
internal sealed class {{attributeName}} : System.Attribute
{
}
""";
}
