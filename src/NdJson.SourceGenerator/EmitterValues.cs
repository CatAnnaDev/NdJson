using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;

namespace NdJson.SourceGeneration
{
    internal enum CollectionKind
    {
        None,
        Array,
        List,
        HashSet,
        ListInterface,
        ConcreteCollection,
        Dictionary,
        DictionaryInterface
    }

    internal sealed partial class Emitter
    {
        private void EmitWriteValue(StringBuilder sb, string indent, ITypeSymbol type, string expr, MemberModel member, ref int temp)
        {
            if (member != null && member.ExplicitConverter != null)
            {
                sb.Append(indent).AppendLine("__c" + member.ConverterFieldIndex + ".Write(ref writer, " + expr + ", options);");
                return;
            }

            switch (type.SpecialType)
            {
                case SpecialType.System_String:
                    sb.Append(indent).AppendLine("writer.WriteString(" + expr + ");");
                    return;
                case SpecialType.System_Boolean:
                    sb.Append(indent).AppendLine("writer.WriteBoolean(" + expr + ");");
                    return;
                case SpecialType.System_Byte:
                case SpecialType.System_UInt16:
                case SpecialType.System_UInt32:
                case SpecialType.System_UInt64:
                    sb.Append(indent).AppendLine("writer.WriteNumber((ulong)" + expr + ");");
                    return;
                case SpecialType.System_SByte:
                case SpecialType.System_Int16:
                case SpecialType.System_Int32:
                case SpecialType.System_Int64:
                    sb.Append(indent).AppendLine("writer.WriteNumber((long)" + expr + ");");
                    return;
                case SpecialType.System_Single:
                    sb.Append(indent).AppendLine("writer.WriteNumber(" + expr + ", options.NonFiniteHandling);");
                    return;
                case SpecialType.System_Double:
                    sb.Append(indent).AppendLine("writer.WriteNumber(" + expr + ", options.NonFiniteHandling);");
                    return;
                case SpecialType.System_Decimal:
                    sb.Append(indent).AppendLine("writer.WriteNumber(" + expr + ");");
                    return;
                case SpecialType.System_Char:
                    sb.Append(indent).AppendLine("writer.WriteString(" + expr + ");");
                    return;
                case SpecialType.System_DateTime:
                    sb.Append(indent).AppendLine(Support + "WriteDateTime(ref writer, " + expr + ", options, " + DateFormatLiteral(member) + ");");
                    return;
                case SpecialType.System_Object:
                    sb.Append(indent).AppendLine("options.GetConverter<object>().Write(ref writer, " + expr + ", options);");
                    return;
            }

            if (TypeHelpers.Is(type, "System.DateTimeOffset"))
            {
                sb.Append(indent).AppendLine(Support + "WriteDateTimeOffset(ref writer, " + expr + ", options, " + DateFormatLiteral(member) + ");");
                return;
            }

            if (TypeHelpers.Is(type, "System.Guid"))
            {
                sb.Append(indent).AppendLine("writer.WriteGuid(" + expr + ");");
                return;
            }

            if (TypeHelpers.Is(type, "System.TimeSpan"))
            {
                sb.Append(indent).AppendLine("writer.WriteTimeSpan(" + expr + ");");
                return;
            }

            if (TypeHelpers.Is(type, "System.Uri"))
            {
                int id = temp++;
                sb.Append(indent).AppendLine("var __u" + id + " = " + expr + ";");
                sb.Append(indent).AppendLine("if (__u" + id + " == null) { writer.WriteNull(); } else { writer.WriteString(__u" + id + ".OriginalString); }");
                return;
            }

            if (IsByteArray(type))
            {
                int id = temp++;
                sb.Append(indent).AppendLine("var __b" + id + " = " + expr + ";");
                sb.Append(indent).AppendLine("if (__b" + id + " == null) { writer.WriteNull(); } else { writer.WriteString(global::System.Convert.ToBase64String(__b" + id + ")); }");
                return;
            }

            if (_known.NdjsonValue != null && SymbolEqualityComparer.Default.Equals(type, _known.NdjsonValue))
            {
                int id = temp++;
                sb.Append(indent).AppendLine("var __j" + id + " = " + expr + ";");
                sb.Append(indent).AppendLine("if (__j" + id + " == null) { writer.WriteNull(); } else { __j" + id + ".WriteTo(ref writer, options); }");
                return;
            }

            if (type.TypeKind == TypeKind.Enum)
            {
                string helper = GetEnumHelper((INamedTypeSymbol)type);
                sb.Append(indent).AppendLine(helper + ".Write(ref writer, " + expr + ", options, " + (member != null && member.EnumAsString ? "true" : "false") + ");");
                return;
            }

            INamedTypeSymbol nullable = type as INamedTypeSymbol;
            if (nullable != null && nullable.OriginalDefinition.SpecialType == SpecialType.System_Nullable_T)
            {
                ITypeSymbol underlying = nullable.TypeArguments[0];
                int id = temp++;
                sb.Append(indent).AppendLine("var __o" + id + " = " + expr + ";");
                sb.Append(indent).AppendLine("if (!__o" + id + ".HasValue)");
                sb.Append(indent).AppendLine("{");
                sb.Append(indent).AppendLine("    writer.WriteNull();");
                sb.Append(indent).AppendLine("}");
                sb.Append(indent).AppendLine("else");
                sb.Append(indent).AppendLine("{");
                EmitWriteValue(sb, indent + "    ", underlying, "__o" + id + ".Value", member, ref temp);
                sb.Append(indent).AppendLine("}");
                return;
            }

            ITypeSymbol element;
            ITypeSymbol dictionaryValue;
            CollectionKind kind = ClassifyCollection(type, out element, out dictionaryValue);

            if (kind == CollectionKind.Dictionary || kind == CollectionKind.DictionaryInterface)
            {
                int id = temp++;
                sb.Append(indent).AppendLine("var __d" + id + " = " + expr + ";");
                sb.Append(indent).AppendLine("if (__d" + id + " == null)");
                sb.Append(indent).AppendLine("{");
                sb.Append(indent).AppendLine("    writer.WriteNull();");
                sb.Append(indent).AppendLine("}");
                sb.Append(indent).AppendLine("else");
                sb.Append(indent).AppendLine("{");
                sb.Append(indent).AppendLine("    writer.WriteStartObject();");
                sb.Append(indent).AppendLine("    foreach (var __p" + id + " in __d" + id + ")");
                sb.Append(indent).AppendLine("    {");
                sb.Append(indent).AppendLine("        writer.WritePropertyName(__p" + id + ".Key);");
                EmitWriteValue(sb, indent + "        ", dictionaryValue, "__p" + id + ".Value", null, ref temp);
                sb.Append(indent).AppendLine("    }");
                sb.Append(indent).AppendLine();
                sb.Append(indent).AppendLine("    writer.WriteEndObject();");
                sb.Append(indent).AppendLine("}");
                return;
            }

            if (kind != CollectionKind.None)
            {
                int id = temp++;
                sb.Append(indent).AppendLine("var __a" + id + " = " + expr + ";");
                sb.Append(indent).AppendLine("if (__a" + id + " == null)");
                sb.Append(indent).AppendLine("{");
                sb.Append(indent).AppendLine("    writer.WriteNull();");
                sb.Append(indent).AppendLine("}");
                sb.Append(indent).AppendLine("else");
                sb.Append(indent).AppendLine("{");
                sb.Append(indent).AppendLine("    writer.WriteStartArray();");
                sb.Append(indent).AppendLine("    foreach (" + Display.FullName(element) + " __i" + id + " in __a" + id + ")");
                sb.Append(indent).AppendLine("    {");
                EmitWriteValue(sb, indent + "        ", element, "__i" + id, null, ref temp);
                sb.Append(indent).AppendLine("    }");
                sb.Append(indent).AppendLine();
                sb.Append(indent).AppendLine("    writer.WriteEndArray();");
                sb.Append(indent).AppendLine("}");
                return;
            }

            sb.Append(indent).AppendLine(ConverterExpression(type) + ".Write(ref writer, " + expr + ", options);");
        }

        private void EmitReadInto(StringBuilder sb, string indent, ITypeSymbol type, string target, MemberModel member, ref int temp)
        {
            if (member != null && member.ExplicitConverter != null)
            {
                sb.Append(indent).AppendLine(target + " = __c" + member.ConverterFieldIndex + ".Read(ref reader, options);");
                return;
            }

            switch (type.SpecialType)
            {
                case SpecialType.System_String:
                    sb.Append(indent).AppendLine(target + " = reader.GetString();");
                    return;
                case SpecialType.System_Boolean:
                    sb.Append(indent).AppendLine(target + " = reader.GetBoolean();");
                    return;
                case SpecialType.System_Byte:
                    sb.Append(indent).AppendLine(target + " = reader.GetByte();");
                    return;
                case SpecialType.System_SByte:
                    sb.Append(indent).AppendLine(target + " = reader.GetSByte();");
                    return;
                case SpecialType.System_Int16:
                    sb.Append(indent).AppendLine(target + " = reader.GetInt16();");
                    return;
                case SpecialType.System_UInt16:
                    sb.Append(indent).AppendLine(target + " = reader.GetUInt16();");
                    return;
                case SpecialType.System_Int32:
                    sb.Append(indent).AppendLine(target + " = reader.GetInt32();");
                    return;
                case SpecialType.System_UInt32:
                    sb.Append(indent).AppendLine(target + " = reader.GetUInt32();");
                    return;
                case SpecialType.System_Int64:
                    sb.Append(indent).AppendLine(target + " = reader.GetInt64();");
                    return;
                case SpecialType.System_UInt64:
                    sb.Append(indent).AppendLine(target + " = reader.GetUInt64();");
                    return;
                case SpecialType.System_Single:
                    sb.Append(indent).AppendLine(target + " = reader.GetSingle();");
                    return;
                case SpecialType.System_Double:
                    sb.Append(indent).AppendLine(target + " = reader.GetDouble();");
                    return;
                case SpecialType.System_Decimal:
                    sb.Append(indent).AppendLine(target + " = reader.GetDecimal();");
                    return;
                case SpecialType.System_Char:
                    sb.Append(indent).AppendLine(target + " = reader.GetChar();");
                    return;
                case SpecialType.System_DateTime:
                    sb.Append(indent).AppendLine(target + " = " + Support + "ReadDateTime(ref reader, options, " + DateFormatLiteral(member) + ");");
                    return;
                case SpecialType.System_Object:
                    sb.Append(indent).AppendLine(target + " = options.GetConverter<object>().Read(ref reader, options);");
                    return;
            }

            if (TypeHelpers.Is(type, "System.DateTimeOffset"))
            {
                sb.Append(indent).AppendLine(target + " = " + Support + "ReadDateTimeOffset(ref reader, options, " + DateFormatLiteral(member) + ");");
                return;
            }

            if (TypeHelpers.Is(type, "System.Guid"))
            {
                sb.Append(indent).AppendLine(target + " = reader.GetGuid();");
                return;
            }

            if (TypeHelpers.Is(type, "System.TimeSpan"))
            {
                sb.Append(indent).AppendLine(target + " = reader.GetTimeSpan();");
                return;
            }

            if (TypeHelpers.Is(type, "System.Uri"))
            {
                int id = temp++;
                sb.Append(indent).AppendLine("string __us" + id + " = reader.GetString();");
                sb.Append(indent).AppendLine(target + " = __us" + id + " == null ? null : new global::System.Uri(__us" + id + ", global::System.UriKind.RelativeOrAbsolute);");
                return;
            }

            if (IsByteArray(type))
            {
                sb.Append(indent).AppendLine(target + " = reader.IsNull ? null : global::System.Convert.FromBase64String(reader.GetString());");
                return;
            }

            if (_known.NdjsonValue != null && SymbolEqualityComparer.Default.Equals(type, _known.NdjsonValue))
            {
                sb.Append(indent).AppendLine(target + " = " + Ndj + "NdjsonValue.ReadCurrent(ref reader);");
                return;
            }

            if (type.TypeKind == TypeKind.Enum)
            {
                string helper = GetEnumHelper((INamedTypeSymbol)type);
                sb.Append(indent).AppendLine(target + " = " + helper + ".Read(ref reader, options);");
                return;
            }

            INamedTypeSymbol nullable = type as INamedTypeSymbol;
            if (nullable != null && nullable.OriginalDefinition.SpecialType == SpecialType.System_Nullable_T)
            {
                ITypeSymbol underlying = nullable.TypeArguments[0];
                int id = temp++;
                sb.Append(indent).AppendLine("if (reader.IsNull)");
                sb.Append(indent).AppendLine("{");
                sb.Append(indent).AppendLine("    " + target + " = null;");
                sb.Append(indent).AppendLine("}");
                sb.Append(indent).AppendLine("else");
                sb.Append(indent).AppendLine("{");
                sb.Append(indent).AppendLine("    " + Display.FullName(underlying) + " __nv" + id + " = default(" + Display.FullName(underlying) + ");");
                EmitReadInto(sb, indent + "    ", underlying, "__nv" + id, member, ref temp);
                sb.Append(indent).AppendLine("    " + target + " = __nv" + id + ";");
                sb.Append(indent).AppendLine("}");
                return;
            }

            ITypeSymbol element;
            ITypeSymbol dictionaryValue;
            CollectionKind kind = ClassifyCollection(type, out element, out dictionaryValue);

            if (kind == CollectionKind.Dictionary || kind == CollectionKind.DictionaryInterface)
            {
                int id = temp++;
                string concrete = kind == CollectionKind.Dictionary
                    ? Display.FullName(type)
                    : "global::System.Collections.Generic.Dictionary<string, " + Display.FullName(dictionaryValue) + ">";

                sb.Append(indent).AppendLine("if (reader.IsNull)");
                sb.Append(indent).AppendLine("{");
                sb.Append(indent).AppendLine("    " + target + " = null;");
                sb.Append(indent).AppendLine("}");
                sb.Append(indent).AppendLine("else");
                sb.Append(indent).AppendLine("{");
                sb.Append(indent).AppendLine("    reader.BeginObject();");
                sb.Append(indent).AppendLine("    " + concrete + " __dm" + id + " = new " + concrete + "();");
                sb.Append(indent).AppendLine("    while (reader.ReadNextProperty())");
                sb.Append(indent).AppendLine("    {");
                sb.Append(indent).AppendLine("        string __dk" + id + " = reader.GetString();");
                sb.Append(indent).AppendLine("        reader.Advance();");
                sb.Append(indent).AppendLine("        " + Display.FullName(dictionaryValue) + " __dv" + id + " = default(" + Display.FullName(dictionaryValue) + ");");
                EmitReadInto(sb, indent + "        ", dictionaryValue, "__dv" + id, null, ref temp);
                sb.Append(indent).AppendLine("        __dm" + id + "[__dk" + id + "] = __dv" + id + ";");
                sb.Append(indent).AppendLine("    }");
                sb.Append(indent).AppendLine();
                sb.Append(indent).AppendLine("    " + target + " = __dm" + id + ";");
                sb.Append(indent).AppendLine("}");
                return;
            }

            if (kind != CollectionKind.None)
            {
                int id = temp++;
                string elementName = Display.FullName(element);
                string builderType;
                string assignment;

                switch (kind)
                {
                    case CollectionKind.Array:
                        builderType = "global::System.Collections.Generic.List<" + elementName + ">";
                        assignment = "__cl" + id + ".ToArray()";
                        break;
                    case CollectionKind.HashSet:
                        builderType = Display.FullName(type);
                        assignment = "__cl" + id;
                        break;
                    case CollectionKind.ConcreteCollection:
                        builderType = Display.FullName(type);
                        assignment = "__cl" + id;
                        break;
                    default:
                        builderType = "global::System.Collections.Generic.List<" + elementName + ">";
                        assignment = "__cl" + id;
                        break;
                }

                sb.Append(indent).AppendLine("if (reader.IsNull)");
                sb.Append(indent).AppendLine("{");
                sb.Append(indent).AppendLine("    " + target + " = null;");
                sb.Append(indent).AppendLine("}");
                sb.Append(indent).AppendLine("else");
                sb.Append(indent).AppendLine("{");
                sb.Append(indent).AppendLine("    reader.BeginArray();");
                sb.Append(indent).AppendLine("    " + builderType + " __cl" + id + " = new " + builderType + "();");
                sb.Append(indent).AppendLine("    while (reader.ReadNextArrayElement())");
                sb.Append(indent).AppendLine("    {");
                sb.Append(indent).AppendLine("        " + elementName + " __ce" + id + " = default(" + elementName + ");");
                EmitReadInto(sb, indent + "        ", element, "__ce" + id, null, ref temp);
                sb.Append(indent).AppendLine("        __cl" + id + ".Add(__ce" + id + ");");
                sb.Append(indent).AppendLine("    }");
                sb.Append(indent).AppendLine();
                sb.Append(indent).AppendLine("    " + target + " = " + assignment + ";");
                sb.Append(indent).AppendLine("}");
                return;
            }

            sb.Append(indent).AppendLine(target + " = " + ConverterExpression(type) + ".Read(ref reader, options);");
        }

        private static string DateFormatLiteral(MemberModel member)
        {
            DateFormat format = member == null ? DateFormat.Inherit : member.DateFormat;
            switch (format)
            {
                case DateFormat.Iso8601:
                    return Ndj + "NdjsonDateFormat.Iso8601";
                case DateFormat.UnixSeconds:
                    return Ndj + "NdjsonDateFormat.UnixSeconds";
                case DateFormat.UnixMilliseconds:
                    return Ndj + "NdjsonDateFormat.UnixMilliseconds";
                case DateFormat.Ticks:
                    return Ndj + "NdjsonDateFormat.Ticks";
                default:
                    return Ndj + "NdjsonDateFormat.Inherit";
            }
        }

        private static bool IsByteArray(ITypeSymbol type)
        {
            IArrayTypeSymbol array = type as IArrayTypeSymbol;
            return array != null && array.Rank == 1 && array.ElementType.SpecialType == SpecialType.System_Byte;
        }

        private CollectionKind ClassifyCollection(ITypeSymbol type, out ITypeSymbol element, out ITypeSymbol dictionaryValue)
        {
            element = null;
            dictionaryValue = null;

            IArrayTypeSymbol array = type as IArrayTypeSymbol;
            if (array != null)
            {
                if (array.Rank != 1)
                {
                    return CollectionKind.None;
                }

                element = array.ElementType;
                return CollectionKind.Array;
            }

            INamedTypeSymbol named = type as INamedTypeSymbol;
            if (named == null)
            {
                return CollectionKind.None;
            }

            if (TypeHelpers.Is(named, "System.Collections.Generic.Dictionary", 2) && named.TypeArguments[0].SpecialType == SpecialType.System_String)
            {
                dictionaryValue = named.TypeArguments[1];
                return CollectionKind.Dictionary;
            }

            if (named.TypeKind == TypeKind.Interface)
            {
                INamedTypeSymbol dictionaryInterface = TypeHelpers.FindGenericInterface(named, "System.Collections.Generic.IDictionary", 2)
                    ?? TypeHelpers.FindGenericInterface(named, "System.Collections.Generic.IReadOnlyDictionary", 2);
                if (dictionaryInterface != null)
                {
                    if (dictionaryInterface.TypeArguments[0].SpecialType != SpecialType.System_String)
                    {
                        return CollectionKind.None;
                    }

                    dictionaryValue = dictionaryInterface.TypeArguments[1];
                    return CollectionKind.DictionaryInterface;
                }

                INamedTypeSymbol enumerableInterface = TypeHelpers.FindGenericInterface(named, "System.Collections.Generic.IEnumerable", 1);
                if (enumerableInterface != null)
                {
                    element = enumerableInterface.TypeArguments[0];
                    return CollectionKind.ListInterface;
                }

                return CollectionKind.None;
            }

            if (TypeHelpers.Is(named, "System.Collections.Generic.List", 1))
            {
                element = named.TypeArguments[0];
                return CollectionKind.List;
            }

            if (TypeHelpers.Is(named, "System.Collections.Generic.HashSet", 1))
            {
                element = named.TypeArguments[0];
                return CollectionKind.HashSet;
            }

            if (TypeHelpers.FindGenericInterface(named, "System.Collections.Generic.IDictionary", 2) != null)
            {
                return CollectionKind.None;
            }

            INamedTypeSymbol collection = TypeHelpers.FindGenericInterface(named, "System.Collections.Generic.ICollection", 1);
            if (collection != null && HasPublicParameterlessConstructor(named))
            {
                element = collection.TypeArguments[0];
                return CollectionKind.ConcreteCollection;
            }

            return CollectionKind.None;
        }

        private static bool HasPublicParameterlessConstructor(INamedTypeSymbol type)
        {
            if (type.IsAbstract)
            {
                return false;
            }

            foreach (IMethodSymbol constructor in type.InstanceConstructors)
            {
                if (constructor.Parameters.Length == 0 && constructor.DeclaredAccessibility == Accessibility.Public)
                {
                    return true;
                }
            }

            return false;
        }

        private string ConverterExpression(ITypeSymbol type)
        {
            INamedTypeSymbol named = type as INamedTypeSymbol;
            if (named != null)
            {
                string generated;
                if (_converterNames.TryGetValue(named, out generated))
                {
                    return generated + ".Instance";
                }

                string external = FindExternalConverter(named);
                if (external != null)
                {
                    return external + ".Instance";
                }
            }

            return "options.GetConverter<" + Display.FullName(type) + ">()";
        }

        private string FindExternalConverter(INamedTypeSymbol type)
        {
            if (SymbolEqualityComparer.Default.Equals(type.ContainingAssembly, _compilation.Assembly))
            {
                return null;
            }

            if (AttributeHelper.Find(type, _known.Serializable) == null)
            {
                return null;
            }

            string ns = type.ContainingNamespace == null || type.ContainingNamespace.IsGlobalNamespace
                ? string.Empty
                : type.ContainingNamespace.ToDisplayString();
            string name = Display.ConverterName(type);
            string metadataName = ns.Length == 0 ? name : ns + "." + name;

            INamedTypeSymbol converter = _compilation.GetTypeByMetadataName(metadataName);
            if (converter == null || converter.DeclaredAccessibility != Accessibility.Public)
            {
                return null;
            }

            return "global::" + metadataName;
        }
    }
}
