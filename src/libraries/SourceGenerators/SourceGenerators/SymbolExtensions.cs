using Microsoft.CodeAnalysis;

namespace SourceGenerators;

public static class SymbolExtensions
{
    extension(INamedTypeSymbol namedTypeSymbol)
    {
        public bool ImplementsInterface(INamedTypeSymbol interfaceType)
        {
            foreach (INamedTypeSymbol implementedInterfaceType in namedTypeSymbol.AllInterfaces)
            {
                if (SymbolEqualityComparer.Default.Equals(interfaceType, implementedInterfaceType)) return true;
            }
            return false;
        }

        public bool HasParameterlessConstructor()
        {
            foreach (IMethodSymbol constructor in namedTypeSymbol.InstanceConstructors)
            {
                if (constructor.Parameters.Length == 0) return true;
            }
            return false;
        }

        public bool HasAttribute(INamedTypeSymbol attributeType)
        {
            foreach (AttributeData attributeData in namedTypeSymbol.GetAttributes())
            {
                if (SymbolEqualityComparer.Default.Equals(attributeType, attributeData.AttributeClass)) return true;
            }
            return false;
        }
    }
}
