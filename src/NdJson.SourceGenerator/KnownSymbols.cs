using Microsoft.CodeAnalysis;

namespace NdJson.SourceGeneration
{
    internal sealed class KnownSymbols
    {
        internal INamedTypeSymbol Serializable;
        internal INamedTypeSymbol Property;
        internal INamedTypeSymbol Ignore;
        internal INamedTypeSymbol Include;
        internal INamedTypeSymbol Required;
        internal INamedTypeSymbol Converter;
        internal INamedTypeSymbol EnumString;
        internal INamedTypeSymbol EnumMember;
        internal INamedTypeSymbol DateFormat;
        internal INamedTypeSymbol ExtensionData;
        internal INamedTypeSymbol Constructor;
        internal INamedTypeSymbol Polymorphic;
        internal INamedTypeSymbol Derived;
        internal INamedTypeSymbol Defaults;
        internal INamedTypeSymbol NdjsonValue;
        internal INamedTypeSymbol ModuleInitializer;

        internal static KnownSymbols Create(Compilation compilation)
        {
            INamedTypeSymbol serializable = compilation.GetTypeByMetadataName("NdJson.NdjsonSerializableAttribute");
            if (serializable == null)
            {
                return null;
            }

            KnownSymbols symbols = new KnownSymbols();
            symbols.Serializable = serializable;
            symbols.Property = compilation.GetTypeByMetadataName("NdJson.NdjsonPropertyAttribute");
            symbols.Ignore = compilation.GetTypeByMetadataName("NdJson.NdjsonIgnoreAttribute");
            symbols.Include = compilation.GetTypeByMetadataName("NdJson.NdjsonIncludeAttribute");
            symbols.Required = compilation.GetTypeByMetadataName("NdJson.NdjsonRequiredAttribute");
            symbols.Converter = compilation.GetTypeByMetadataName("NdJson.NdjsonConverterAttribute");
            symbols.EnumString = compilation.GetTypeByMetadataName("NdJson.NdjsonEnumStringAttribute");
            symbols.EnumMember = compilation.GetTypeByMetadataName("NdJson.NdjsonEnumMemberAttribute");
            symbols.DateFormat = compilation.GetTypeByMetadataName("NdJson.NdjsonDateFormatAttribute");
            symbols.ExtensionData = compilation.GetTypeByMetadataName("NdJson.NdjsonExtensionDataAttribute");
            symbols.Constructor = compilation.GetTypeByMetadataName("NdJson.NdjsonConstructorAttribute");
            symbols.Polymorphic = compilation.GetTypeByMetadataName("NdJson.NdjsonPolymorphicAttribute");
            symbols.Derived = compilation.GetTypeByMetadataName("NdJson.NdjsonDerivedAttribute");
            symbols.Defaults = compilation.GetTypeByMetadataName("NdJson.NdjsonDefaultsAttribute");
            symbols.NdjsonValue = compilation.GetTypeByMetadataName("NdJson.NdjsonValue");
            symbols.ModuleInitializer = compilation.GetTypeByMetadataName("System.Runtime.CompilerServices.ModuleInitializerAttribute");
            return symbols;
        }
    }
}
