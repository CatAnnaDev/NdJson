using System.Collections.Generic;
using Microsoft.CodeAnalysis;

namespace NdJson.SourceGeneration
{
    internal enum IgnoreCondition
    {
        Inherit = 0,
        Never = 1,
        Always = 2,
        WhenWritingNull = 3,
        WhenWritingDefault = 4
    }

    internal enum DateFormat
    {
        Inherit = 0,
        Iso8601 = 1,
        UnixSeconds = 2,
        UnixMilliseconds = 3,
        Ticks = 4
    }

    internal enum NamingPolicy
    {
        Inherit = 0,
        Unchanged = 1,
        CamelCase = 2,
        PascalCase = 3,
        SnakeCaseLower = 4,
        SnakeCaseUpper = 5,
        KebabCaseLower = 6,
        KebabCaseUpper = 7
    }

    internal sealed class MemberModel
    {
        internal string MemberName;
        internal string JsonName;
        internal ITypeSymbol Type;
        internal bool CanRead;
        internal bool CanWrite;
        internal bool IsInitOnly;
        internal bool IsRequired;
        internal bool IsRequiredByAttribute;
        internal int Order;
        internal IgnoreCondition Ignore;
        internal DateFormat DateFormat;
        internal bool EnumAsString;
        internal NamingPolicy EnumNaming;
        internal INamedTypeSymbol ExplicitConverter;
        internal string ConstructorParameterName;
        internal int ConstructorParameterIndex = -1;
        internal int NameFieldIndex;
        internal bool IsExtensionData;
        internal Location Location;
        internal int ConverterFieldIndex = -1;
    }

    internal sealed class DerivedModel
    {
        internal INamedTypeSymbol Type;
        internal string Discriminator;
    }

    internal sealed class TypeModel
    {
        internal INamedTypeSymbol Symbol;
        internal string TypeName;
        internal string ConverterNamespace;
        internal string ConverterName;
        internal string Accessibility;
        internal bool IsValueType;
        internal List<MemberModel> Members = new List<MemberModel>();
        internal MemberModel ExtensionData;
        internal ITypeSymbol ExtensionValueType;
        internal IMethodSymbol Constructor;
        internal bool UseObjectInitializer;
        internal bool IsPolymorphic;
        internal string Discriminator;
        internal bool IgnoreUnrecognized;
        internal List<DerivedModel> Derived = new List<DerivedModel>();
    }
}
