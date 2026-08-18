using Microsoft.CodeAnalysis;

namespace NdJson.SourceGeneration
{
    internal static class TypeHelpers
    {
        private static readonly SymbolDisplayFormat NameOnly = new SymbolDisplayFormat(
            globalNamespaceStyle: SymbolDisplayGlobalNamespaceStyle.Omitted,
            typeQualificationStyle: SymbolDisplayTypeQualificationStyle.NameAndContainingTypesAndNamespaces,
            genericsOptions: SymbolDisplayGenericsOptions.None);

        internal static string OpenName(ITypeSymbol type)
        {
            return type.OriginalDefinition.ToDisplayString(NameOnly);
        }

        internal static bool Is(ITypeSymbol type, string fullName, int arity)
        {
            INamedTypeSymbol named = type as INamedTypeSymbol;
            if (named == null || named.Arity != arity)
            {
                return false;
            }

            return named.OriginalDefinition.ToDisplayString(NameOnly) == fullName;
        }

        internal static bool Is(ITypeSymbol type, string fullName)
        {
            return type != null && type.OriginalDefinition.ToDisplayString(NameOnly) == fullName && (type as INamedTypeSymbol) != null;
        }

        internal static INamedTypeSymbol FindGenericInterface(ITypeSymbol type, string fullName, int arity)
        {
            if (Is(type, fullName, arity))
            {
                return (INamedTypeSymbol)type;
            }

            foreach (INamedTypeSymbol candidate in type.AllInterfaces)
            {
                if (Is(candidate, fullName, arity))
                {
                    return candidate;
                }
            }

            return null;
        }

        internal static INamedTypeSymbol FindGenericInterface(ITypeSymbol type, string metadataName)
        {
            int tick = metadataName.IndexOf('`');
            if (tick < 0)
            {
                return FindGenericInterface(type, metadataName, 0);
            }

            string name = metadataName.Substring(0, tick);
            int arity = int.Parse(metadataName.Substring(tick + 1));
            return FindGenericInterface(type, name, arity);
        }
    }
}
