using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace SourceGenerators;

public sealed record ClassInfo(string Namespace, string ClassName)
{
    public static ClassInfo? FindFrom(GeneratorAttributeSyntaxContext context, string attributeName)
    {
        if (context.TargetNode is not ClassDeclarationSyntax classDeclarationSyntax) return null;
        foreach (AttributeData attributeData in context.Attributes)
        {
            if ((attributeData.AttributeClass?.Name) != attributeName)
            {
                continue;
            }
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
}
