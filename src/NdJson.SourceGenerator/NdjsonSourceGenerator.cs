using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace NdJson.SourceGeneration
{
    [Generator]
    public sealed class NdjsonSourceGenerator : IIncrementalGenerator
    {
        internal static readonly DiagnosticDescriptor GenericTypeNotSupported = new DiagnosticDescriptor(
            "NDJSON001",
            "Type generique non pris en charge",
            "Le type generique '{0}' ne peut pas etre traite par le generateur NDJSON ; le repli par reflexion sera utilise.",
            "NdJson",
            DiagnosticSeverity.Warning,
            true);

        internal static readonly DiagnosticDescriptor NoUsableConstructor = new DiagnosticDescriptor(
            "NDJSON002",
            "Aucun constructeur utilisable",
            "Le type '{0}' n'expose aucun constructeur accessible utilisable pour la deserialisation NDJSON.",
            "NdJson",
            DiagnosticSeverity.Warning,
            true);

        internal static readonly DiagnosticDescriptor InvalidExtensionData = new DiagnosticDescriptor(
            "NDJSON003",
            "Membre [NdjsonExtensionData] invalide",
            "Le membre '{0}' doit etre de type IDictionary<string, NdjsonValue> ou IDictionary<string, object>.",
            "NdJson",
            DiagnosticSeverity.Warning,
            true);

        internal static readonly DiagnosticDescriptor MissingDerived = new DiagnosticDescriptor(
            "NDJSON004",
            "Type polymorphe sans type derive",
            "Le type '{0}' porte [NdjsonPolymorphic] mais aucun [NdjsonDerived] n'est declare.",
            "NdJson",
            DiagnosticSeverity.Warning,
            true);

        public void Initialize(IncrementalGeneratorInitializationContext context)
        {
            IncrementalValueProvider<ImmutableArray<INamedTypeSymbol>> candidates = context.SyntaxProvider
                .CreateSyntaxProvider(
                    static (node, _) => node is TypeDeclarationSyntax declaration && declaration.AttributeLists.Count > 0,
                    static (ctx, _) => ctx.SemanticModel.GetDeclaredSymbol((TypeDeclarationSyntax)ctx.Node))
                .Where(static symbol => symbol != null)
                .Collect();

            IncrementalValueProvider<(Compilation, ImmutableArray<INamedTypeSymbol>)> combined = context.CompilationProvider.Combine(candidates);

            context.RegisterSourceOutput(combined, static (spc, pair) => Execute(pair.Item1, pair.Item2, spc));
        }

        private static void Execute(Compilation compilation, ImmutableArray<INamedTypeSymbol> candidates, SourceProductionContext context)
        {
            KnownSymbols known = KnownSymbols.Create(compilation);
            if (known == null)
            {
                return;
            }

            NamingPolicy defaultNaming = NamingPolicy.Unchanged;
            IgnoreCondition defaultIgnore = IgnoreCondition.Never;
            bool defaultIncludeFields = true;

            if (known.Defaults != null)
            {
                foreach (AttributeData attribute in compilation.Assembly.GetAttributes())
                {
                    if (!SymbolEqualityComparer.Default.Equals(attribute.AttributeClass, known.Defaults))
                    {
                        continue;
                    }

                    defaultNaming = (NamingPolicy)AttributeHelper.GetNamedInt(attribute, "NamingPolicy", (int)NamingPolicy.Unchanged);
                    defaultIgnore = (IgnoreCondition)AttributeHelper.GetNamedInt(attribute, "DefaultIgnoreCondition", (int)IgnoreCondition.Never);
                    defaultIncludeFields = AttributeHelper.GetNamedBool(attribute, "IncludeFields", true);
                }
            }

            if (defaultNaming == NamingPolicy.Inherit)
            {
                defaultNaming = NamingPolicy.Unchanged;
            }

            if (defaultIgnore == IgnoreCondition.Inherit)
            {
                defaultIgnore = IgnoreCondition.Never;
            }

            HashSet<INamedTypeSymbol> targets = new HashSet<INamedTypeSymbol>(SymbolEqualityComparer.Default);

            foreach (INamedTypeSymbol candidate in candidates)
            {
                if (AttributeHelper.Find(candidate, known.Serializable) != null)
                {
                    targets.Add(candidate);
                }
            }

            foreach (AttributeData attribute in compilation.Assembly.GetAttributes())
            {
                if (!SymbolEqualityComparer.Default.Equals(attribute.AttributeClass, known.Serializable))
                {
                    continue;
                }

                if (attribute.ConstructorArguments.Length == 1)
                {
                    INamedTypeSymbol declared = attribute.ConstructorArguments[0].Value as INamedTypeSymbol;
                    if (declared != null)
                    {
                        targets.Add(declared);
                    }
                }
            }

            List<INamedTypeSymbol> pending = targets.ToList();
            foreach (INamedTypeSymbol target in pending)
            {
                foreach (AttributeData attribute in target.GetAttributes())
                {
                    if (!SymbolEqualityComparer.Default.Equals(attribute.AttributeClass, known.Derived))
                    {
                        continue;
                    }

                    if (attribute.ConstructorArguments.Length >= 1)
                    {
                        INamedTypeSymbol derived = attribute.ConstructorArguments[0].Value as INamedTypeSymbol;
                        if (derived != null)
                        {
                            targets.Add(derived);
                        }
                    }
                }
            }

            List<TypeModel> models = new List<TypeModel>();
            Dictionary<INamedTypeSymbol, string> converterNames = new Dictionary<INamedTypeSymbol, string>(SymbolEqualityComparer.Default);

            foreach (INamedTypeSymbol target in targets)
            {
                if (target.IsGenericType || target.IsUnboundGenericType)
                {
                    context.ReportDiagnostic(Diagnostic.Create(GenericTypeNotSupported, target.Locations.FirstOrDefault(), target.ToDisplayString()));
                    continue;
                }

                if (target.TypeKind != TypeKind.Class && target.TypeKind != TypeKind.Struct && target.TypeKind != TypeKind.Interface)
                {
                    continue;
                }

                TypeModel model = ModelBuilder.Build(target, known, defaultNaming, defaultIgnore, defaultIncludeFields, context);
                if (model == null)
                {
                    continue;
                }

                models.Add(model);
                converterNames[target] = model.ConverterNamespace.Length == 0
                    ? "global::" + model.ConverterName
                    : "global::" + model.ConverterNamespace + "." + model.ConverterName;
            }

            if (models.Count == 0)
            {
                return;
            }

            Emitter emitter = new Emitter(compilation, known, converterNames);

            foreach (TypeModel model in models)
            {
                string source = emitter.EmitConverter(model);
                context.AddSource(SanitizeFileName(model.ConverterNamespace, model.ConverterName) + ".g.cs", SourceText.From(source, Encoding.UTF8));
            }

            string enums = emitter.EmitEnumHelpers();
            if (enums != null)
            {
                context.AddSource("NdjsonEnumHelpers.g.cs", SourceText.From(enums, Encoding.UTF8));
            }

            bool moduleInitializerAvailable = known.ModuleInitializer != null &&
                compilation is CSharpCompilation csharp &&
                csharp.LanguageVersion >= LanguageVersion.CSharp9;

            string registry = emitter.EmitRegistry(models, compilation.AssemblyName, moduleInitializerAvailable);
            context.AddSource("NdjsonGeneratedRegistry.g.cs", SourceText.From(registry, Encoding.UTF8));
        }

        private static string SanitizeFileName(string ns, string name)
        {
            string full = ns.Length == 0 ? name : ns + "." + name;
            StringBuilder builder = new StringBuilder(full.Length);
            foreach (char c in full)
            {
                builder.Append(char.IsLetterOrDigit(c) || c == '_' || c == '.' ? c : '_');
            }

            return builder.ToString();
        }
    }

    internal static class AttributeHelper
    {
        internal static AttributeData Find(ISymbol symbol, INamedTypeSymbol attributeType)
        {
            if (attributeType == null)
            {
                return null;
            }

            foreach (AttributeData attribute in symbol.GetAttributes())
            {
                if (SymbolEqualityComparer.Default.Equals(attribute.AttributeClass, attributeType))
                {
                    return attribute;
                }
            }

            return null;
        }

        internal static IEnumerable<AttributeData> FindAll(ISymbol symbol, INamedTypeSymbol attributeType)
        {
            if (attributeType == null)
            {
                yield break;
            }

            foreach (AttributeData attribute in symbol.GetAttributes())
            {
                if (SymbolEqualityComparer.Default.Equals(attribute.AttributeClass, attributeType))
                {
                    yield return attribute;
                }
            }
        }

        internal static int GetNamedInt(AttributeData attribute, string name, int fallback)
        {
            foreach (KeyValuePair<string, TypedConstant> argument in attribute.NamedArguments)
            {
                if (argument.Key == name && argument.Value.Value is int value)
                {
                    return value;
                }
            }

            return fallback;
        }

        internal static bool GetNamedBool(AttributeData attribute, string name, bool fallback)
        {
            foreach (KeyValuePair<string, TypedConstant> argument in attribute.NamedArguments)
            {
                if (argument.Key == name && argument.Value.Value is bool value)
                {
                    return value;
                }
            }

            return fallback;
        }

        internal static string GetNamedString(AttributeData attribute, string name, string fallback)
        {
            foreach (KeyValuePair<string, TypedConstant> argument in attribute.NamedArguments)
            {
                if (argument.Key == name && argument.Value.Value is string value)
                {
                    return value;
                }
            }

            return fallback;
        }

        internal static string GetConstructorString(AttributeData attribute, int index)
        {
            if (attribute.ConstructorArguments.Length > index)
            {
                return attribute.ConstructorArguments[index].Value as string;
            }

            return null;
        }

        internal static int GetConstructorInt(AttributeData attribute, int index, int fallback)
        {
            if (attribute.ConstructorArguments.Length > index && attribute.ConstructorArguments[index].Value is int value)
            {
                return value;
            }

            return fallback;
        }

        internal static bool GetConstructorBool(AttributeData attribute, int index, bool fallback)
        {
            if (attribute.ConstructorArguments.Length > index && attribute.ConstructorArguments[index].Value is bool value)
            {
                return value;
            }

            return fallback;
        }
    }
}
