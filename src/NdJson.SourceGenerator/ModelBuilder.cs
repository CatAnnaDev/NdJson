using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace NdJson.SourceGeneration
{
    internal static class ModelBuilder
    {
        internal static TypeModel Build(
            INamedTypeSymbol type,
            KnownSymbols known,
            NamingPolicy defaultNaming,
            IgnoreCondition defaultIgnore,
            bool defaultIncludeFields,
            SourceProductionContext context)
        {
            TypeModel model = new TypeModel();
            model.Symbol = type;
            model.TypeName = Display.FullName(type);
            model.IsValueType = type.IsValueType;
            model.ConverterNamespace = type.ContainingNamespace == null || type.ContainingNamespace.IsGlobalNamespace
                ? string.Empty
                : type.ContainingNamespace.ToDisplayString();

            NamingPolicy naming = defaultNaming;
            IgnoreCondition ignore = defaultIgnore;
            bool includeFields = defaultIncludeFields;
            string converterName = null;

            AttributeData serializable = AttributeHelper.Find(type, known.Serializable);
            if (serializable != null)
            {
                int namingValue = AttributeHelper.GetNamedInt(serializable, "NamingPolicy", (int)NamingPolicy.Inherit);
                if (namingValue != (int)NamingPolicy.Inherit)
                {
                    naming = (NamingPolicy)namingValue;
                }

                int ignoreValue = AttributeHelper.GetNamedInt(serializable, "DefaultIgnoreCondition", (int)IgnoreCondition.Inherit);
                if (ignoreValue != (int)IgnoreCondition.Inherit)
                {
                    ignore = (IgnoreCondition)ignoreValue;
                }

                includeFields = AttributeHelper.GetNamedBool(serializable, "IncludeFields", includeFields);
                converterName = AttributeHelper.GetNamedString(serializable, "GeneratedConverterName", null);
            }

            model.ConverterName = string.IsNullOrEmpty(converterName) ? Display.ConverterName(type) : converterName;
            model.Accessibility = type.DeclaredAccessibility == Accessibility.Public ? "public" : "internal";

            AttributeData polymorphic = AttributeHelper.Find(type, known.Polymorphic);
            if (polymorphic != null)
            {
                model.IsPolymorphic = true;
                string discriminator = AttributeHelper.GetConstructorString(polymorphic, 0);
                if (string.IsNullOrEmpty(discriminator))
                {
                    discriminator = AttributeHelper.GetNamedString(polymorphic, "DiscriminatorName", "$type");
                }

                model.Discriminator = discriminator;
                model.IgnoreUnrecognized = AttributeHelper.GetNamedBool(polymorphic, "IgnoreUnrecognized", false);

                foreach (AttributeData derived in AttributeHelper.FindAll(type, known.Derived))
                {
                    INamedTypeSymbol derivedType = derived.ConstructorArguments.Length > 0 ? derived.ConstructorArguments[0].Value as INamedTypeSymbol : null;
                    if (derivedType == null)
                    {
                        continue;
                    }

                    string tag = AttributeHelper.GetConstructorString(derived, 1);
                    model.Derived.Add(new DerivedModel { Type = derivedType, Discriminator = string.IsNullOrEmpty(tag) ? derivedType.Name : tag });
                }

                if (model.Derived.Count == 0)
                {
                    context.ReportDiagnostic(Diagnostic.Create(NdjsonSourceGenerator.MissingDerived, type.Locations.FirstOrDefault(), type.ToDisplayString()));
                    return null;
                }
            }

            if (type.TypeKind == TypeKind.Interface)
            {
                if (!model.IsPolymorphic)
                {
                    return null;
                }

                return model;
            }

            CollectMembers(type, known, naming, ignore, includeFields, model, context);

            if (type.IsAbstract)
            {
                if (!model.IsPolymorphic)
                {
                    return null;
                }

                return model;
            }

            IMethodSymbol constructor = SelectConstructor(type, known, model.Members);
            if (constructor == null)
            {
                context.ReportDiagnostic(Diagnostic.Create(NdjsonSourceGenerator.NoUsableConstructor, type.Locations.FirstOrDefault(), type.ToDisplayString()));
                return null;
            }

            model.Constructor = constructor;

            for (int i = 0; i < constructor.Parameters.Length; i++)
            {
                IParameterSymbol parameter = constructor.Parameters[i];
                MemberModel match = FindMemberForParameter(model.Members, parameter.Name);
                if (match != null)
                {
                    match.ConstructorParameterIndex = i;
                    match.ConstructorParameterName = parameter.Name;
                }
            }

            bool needsInitializer = constructor.Parameters.Length > 0;
            foreach (MemberModel member in model.Members)
            {
                if (member.IsInitOnly || member.IsRequired)
                {
                    needsInitializer = true;
                }
            }

            model.UseObjectInitializer = needsInitializer;
            return model;
        }

        private static MemberModel FindMemberForParameter(List<MemberModel> members, string parameterName)
        {
            foreach (MemberModel member in members)
            {
                if (string.Equals(member.MemberName, parameterName, StringComparison.OrdinalIgnoreCase))
                {
                    return member;
                }
            }

            foreach (MemberModel member in members)
            {
                if (string.Equals(member.JsonName, parameterName, StringComparison.OrdinalIgnoreCase))
                {
                    return member;
                }
            }

            return null;
        }

        private static IMethodSymbol SelectConstructor(INamedTypeSymbol type, KnownSymbols known, List<MemberModel> members)
        {
            IMethodSymbol parameterless = null;
            IMethodSymbol widest = null;

            foreach (IMethodSymbol constructor in type.InstanceConstructors)
            {
                if (constructor.IsStatic)
                {
                    continue;
                }

                if (AttributeHelper.Find(constructor, known.Constructor) != null)
                {
                    return constructor;
                }

                if (constructor.DeclaredAccessibility != Accessibility.Public && constructor.DeclaredAccessibility != Accessibility.Internal)
                {
                    continue;
                }

                if (constructor.Parameters.Length == 0)
                {
                    parameterless = constructor;
                    continue;
                }

                if (widest == null || constructor.Parameters.Length > widest.Parameters.Length)
                {
                    widest = constructor;
                }
            }

            if (parameterless == null)
            {
                return widest;
            }

            if (widest != null && RecoversUnsettableMembers(widest, members))
            {
                return widest;
            }

            return parameterless;
        }

        private static bool RecoversUnsettableMembers(IMethodSymbol constructor, List<MemberModel> members)
        {
            foreach (MemberModel member in members)
            {
                if (member.CanWrite || member.IsInitOnly)
                {
                    continue;
                }

                foreach (IParameterSymbol parameter in constructor.Parameters)
                {
                    if (string.Equals(parameter.Name, member.MemberName, StringComparison.OrdinalIgnoreCase))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        private static void CollectMembers(
            INamedTypeSymbol type,
            KnownSymbols known,
            NamingPolicy naming,
            IgnoreCondition defaultIgnore,
            bool includeFields,
            TypeModel model,
            SourceProductionContext context)
        {
            List<List<MemberModel>> levels = new List<List<MemberModel>>();
            HashSet<string> seen = new HashSet<string>(StringComparer.Ordinal);
            INamedTypeSymbol current = type;

            while (current != null && current.SpecialType != SpecialType.System_Object)
            {
                List<MemberModel> level = new List<MemberModel>();

                foreach (ISymbol symbol in current.GetMembers())
                {
                    if (symbol.IsStatic)
                    {
                        continue;
                    }

                    IPropertySymbol property = symbol as IPropertySymbol;
                    IFieldSymbol field = symbol as IFieldSymbol;

                    if (property != null)
                    {
                        if (property.IsIndexer || property.Name == "EqualityContract" || property.GetMethod == null)
                        {
                            continue;
                        }

                        bool forced = AttributeHelper.Find(property, known.Include) != null;
                        bool accessible = property.DeclaredAccessibility == Accessibility.Public ||
                            (forced && property.DeclaredAccessibility == Accessibility.Internal);
                        if (!accessible)
                        {
                            continue;
                        }

                        if (property.GetMethod.DeclaredAccessibility != Accessibility.Public && property.GetMethod.DeclaredAccessibility != Accessibility.Internal)
                        {
                            continue;
                        }

                        if (!seen.Add(property.Name))
                        {
                            continue;
                        }

                        MemberModel member = BuildMember(property, property.Type, known, naming, defaultIgnore, context);
                        if (member == null)
                        {
                            continue;
                        }

                        member.CanRead = true;
                        member.CanWrite = property.SetMethod != null &&
                            (property.SetMethod.DeclaredAccessibility == Accessibility.Public || property.SetMethod.DeclaredAccessibility == Accessibility.Internal);
                        member.IsInitOnly = property.SetMethod != null && property.SetMethod.IsInitOnly;
                        member.IsRequired = IsRequiredMember(property) || member.IsRequiredByAttribute;

                        AddMember(model, level, member, context);
                        continue;
                    }

                    if (field != null && includeFields)
                    {
                        if (field.IsConst || field.AssociatedSymbol != null || field.Name.IndexOf('<') >= 0)
                        {
                            continue;
                        }

                        bool forcedField = AttributeHelper.Find(field, known.Include) != null;
                        if (field.DeclaredAccessibility != Accessibility.Public && !(field.DeclaredAccessibility == Accessibility.Internal && forcedField))
                        {
                            continue;
                        }

                        if (!seen.Add(field.Name))
                        {
                            continue;
                        }

                        MemberModel member = BuildMember(field, field.Type, known, naming, defaultIgnore, context);
                        if (member == null)
                        {
                            continue;
                        }

                        member.CanRead = true;
                        member.CanWrite = !field.IsReadOnly;
                        member.IsRequired = IsRequiredMember(field) || member.IsRequiredByAttribute;

                        AddMember(model, level, member, context);
                    }
                }

                levels.Add(level);
                current = current.BaseType;
            }

            List<MemberModel> ordered = new List<MemberModel>();
            for (int i = levels.Count - 1; i >= 0; i--)
            {
                ordered.AddRange(levels[i]);
            }

            List<MemberModel> sorted = ordered
                .Select((member, index) => new { member, index })
                .OrderBy(entry => entry.member.Order)
                .ThenBy(entry => entry.index)
                .Select(entry => entry.member)
                .ToList();

            for (int i = 0; i < sorted.Count; i++)
            {
                sorted[i].NameFieldIndex = i;
            }

            model.Members = sorted;
        }

        private static void AddMember(TypeModel model, List<MemberModel> level, MemberModel member, SourceProductionContext context)
        {
            if (member.Ignore == IgnoreCondition.Always)
            {
                return;
            }

            if (member.IsExtensionData)
            {
                INamedTypeSymbol dictionary = TypeHelpers.FindGenericInterface(member.Type, "System.Collections.Generic.IDictionary`2");
                if (dictionary == null || dictionary.TypeArguments[0].SpecialType != SpecialType.System_String || !member.CanWrite)
                {
                    context.ReportDiagnostic(Diagnostic.Create(NdjsonSourceGenerator.InvalidExtensionData, member.Location, member.MemberName));
                    return;
                }

                model.ExtensionData = member;
                model.ExtensionValueType = dictionary.TypeArguments[1];
                return;
            }

            level.Add(member);
        }

        private static bool IsRequiredMember(ISymbol symbol)
        {
            foreach (AttributeData attribute in symbol.GetAttributes())
            {
                if (attribute.AttributeClass != null && attribute.AttributeClass.Name == "RequiredMemberAttribute")
                {
                    return true;
                }
            }

            foreach (SyntaxReference reference in symbol.DeclaringSyntaxReferences)
            {
                MemberDeclarationSyntax declaration = reference.GetSyntax() as MemberDeclarationSyntax;
                if (declaration == null)
                {
                    continue;
                }

                foreach (Microsoft.CodeAnalysis.SyntaxToken modifier in declaration.Modifiers)
                {
                    if (modifier.Text == "required")
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        private static MemberModel BuildMember(
            ISymbol symbol,
            ITypeSymbol memberType,
            KnownSymbols known,
            NamingPolicy naming,
            IgnoreCondition defaultIgnore,
            SourceProductionContext context)
        {
            MemberModel member = new MemberModel();
            member.MemberName = symbol.Name;
            member.Type = memberType;
            member.Ignore = defaultIgnore;

            AttributeData ignoreAttribute = AttributeHelper.Find(symbol, known.Ignore);
            if (ignoreAttribute != null)
            {
                int condition = AttributeHelper.GetConstructorInt(ignoreAttribute, 0, (int)IgnoreCondition.Always);
                condition = AttributeHelper.GetNamedInt(ignoreAttribute, "Condition", condition);
                member.Ignore = (IgnoreCondition)condition;
                if (member.Ignore == IgnoreCondition.Always)
                {
                    return member;
                }

                if (member.Ignore == IgnoreCondition.Inherit)
                {
                    member.Ignore = defaultIgnore;
                }
            }

            AttributeData propertyAttribute = AttributeHelper.Find(symbol, known.Property);
            string jsonName = null;
            if (propertyAttribute != null)
            {
                jsonName = AttributeHelper.GetConstructorString(propertyAttribute, 0);
                if (string.IsNullOrEmpty(jsonName))
                {
                    jsonName = AttributeHelper.GetNamedString(propertyAttribute, "Name", null);
                }

                member.Order = AttributeHelper.GetNamedInt(propertyAttribute, "Order", 0);
                member.IsRequiredByAttribute = AttributeHelper.GetNamedBool(propertyAttribute, "Required", false);
            }

            if (AttributeHelper.Find(symbol, known.Required) != null)
            {
                member.IsRequiredByAttribute = true;
            }

            member.JsonName = string.IsNullOrEmpty(jsonName) ? Naming.Convert(symbol.Name, naming) : jsonName;

            AttributeData converterAttribute = AttributeHelper.Find(symbol, known.Converter);
            if (converterAttribute != null && converterAttribute.ConstructorArguments.Length > 0)
            {
                member.ExplicitConverter = converterAttribute.ConstructorArguments[0].Value as INamedTypeSymbol;
            }

            AttributeData dateAttribute = AttributeHelper.Find(symbol, known.DateFormat);
            if (dateAttribute != null)
            {
                member.DateFormat = (DateFormat)AttributeHelper.GetConstructorInt(dateAttribute, 0, (int)DateFormat.Inherit);
            }

            AttributeData enumAttribute = AttributeHelper.Find(symbol, known.EnumString);
            if (enumAttribute != null)
            {
                member.EnumAsString = AttributeHelper.GetConstructorBool(enumAttribute, 0, true);
                member.EnumNaming = (NamingPolicy)AttributeHelper.GetNamedInt(enumAttribute, "NamingPolicy", (int)NamingPolicy.Inherit);
            }

            if (AttributeHelper.Find(symbol, known.ExtensionData) != null)
            {
                member.IsExtensionData = true;
            }

            member.Location = symbol.Locations.FirstOrDefault();
            return member;
        }
    }

    internal static class Display
    {
        internal static readonly SymbolDisplayFormat Format = new SymbolDisplayFormat(
            globalNamespaceStyle: SymbolDisplayGlobalNamespaceStyle.Included,
            typeQualificationStyle: SymbolDisplayTypeQualificationStyle.NameAndContainingTypesAndNamespaces,
            genericsOptions: SymbolDisplayGenericsOptions.IncludeTypeParameters,
            miscellaneousOptions: SymbolDisplayMiscellaneousOptions.EscapeKeywordIdentifiers | SymbolDisplayMiscellaneousOptions.UseSpecialTypes);

        internal static string FullName(ITypeSymbol type)
        {
            return type.WithNullableAnnotation(NullableAnnotation.None).ToDisplayString(Format);
        }

        internal static string ConverterName(INamedTypeSymbol type)
        {
            StringBuilder builder = new StringBuilder();
            BuildNestedName(type, builder);
            builder.Append("NdjsonConverter");
            return builder.ToString();
        }

        internal static string HelperName(ITypeSymbol type)
        {
            StringBuilder builder = new StringBuilder();
            if (type.ContainingNamespace != null && !type.ContainingNamespace.IsGlobalNamespace)
            {
                builder.Append(type.ContainingNamespace.ToDisplayString().Replace('.', '_'));
                builder.Append('_');
            }

            INamedTypeSymbol named = type as INamedTypeSymbol;
            if (named != null)
            {
                BuildNestedName(named, builder);
            }
            else
            {
                builder.Append(type.Name);
            }

            return builder.ToString();
        }

        private static void BuildNestedName(INamedTypeSymbol type, StringBuilder builder)
        {
            if (type.ContainingType != null)
            {
                BuildNestedName(type.ContainingType, builder);
                builder.Append('_');
            }

            builder.Append(type.Name);
        }
    }
}
