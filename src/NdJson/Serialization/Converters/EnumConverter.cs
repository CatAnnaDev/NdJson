using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq.Expressions;
using System.Reflection;

namespace NdJson.Serialization.Converters
{
    internal static class DynamicCodeSupport
    {
        internal static readonly bool IsSupported = Probe();

        private static bool Probe()
        {
            try
            {
                ParameterExpression parameter = Expression.Parameter(typeof(int), "value");
                Func<int, int> compiled = Expression.Lambda<Func<int, int>>(Expression.Add(parameter, Expression.Constant(1)), parameter).Compile();
                return compiled(1) == 2;
            }
            catch (Exception)
            {
                return false;
            }
        }
    }

    internal sealed class EnumConverter<T> : NdjsonConverter<T> where T : struct
    {
        private readonly bool _forceString;
        private readonly bool _isFlags;
        private readonly long[] _values;
        private readonly byte[][] _encodedNames;
        private readonly string[] _names;
        private readonly Dictionary<string, long> _byName;
        private readonly Func<T, long> _toInt64;
        private readonly Func<long, T> _fromInt64;

        public EnumConverter(bool forceString, NdjsonNamingPolicy namingPolicy)
        {
            Type enumType = typeof(T);
            _forceString = forceString;
            _isFlags = enumType.GetTypeInfo().GetCustomAttribute<FlagsAttribute>() != null;

            string[] rawNames = Enum.GetNames(enumType);
            Array rawValues = Enum.GetValues(enumType);
            Type underlying = Enum.GetUnderlyingType(enumType);
            bool unsigned = underlying == typeof(ulong);

            _names = new string[rawNames.Length];
            _values = new long[rawNames.Length];
            _encodedNames = new byte[rawNames.Length][];
            _byName = new Dictionary<string, long>(rawNames.Length * 2, StringComparer.Ordinal);

            for (int i = 0; i < rawNames.Length; i++)
            {
                object rawValue = rawValues.GetValue(i);
                long numeric = unsigned ? unchecked((long)Convert.ToUInt64(rawValue, CultureInfo.InvariantCulture)) : Convert.ToInt64(rawValue, CultureInfo.InvariantCulture);
                string name = ResolveName(enumType, rawNames[i], namingPolicy);

                _names[i] = name;
                _values[i] = numeric;
                _encodedNames[i] = EncodeQuoted(name);
                _byName[name] = numeric;
                if (!_byName.ContainsKey(rawNames[i]))
                {
                    _byName[rawNames[i]] = numeric;
                }
            }

            _toInt64 = CreateToInt64(underlying);
            _fromInt64 = CreateFromInt64(underlying);
        }

        private static string ResolveName(Type enumType, string declaredName, NdjsonNamingPolicy namingPolicy)
        {
            FieldInfo field = enumType.GetField(declaredName, BindingFlags.Public | BindingFlags.Static);
            if (field != null)
            {
                NdjsonEnumMemberAttribute member = field.GetCustomAttribute<NdjsonEnumMemberAttribute>();
                if (member != null && !string.IsNullOrEmpty(member.Name))
                {
                    return member.Name;
                }
            }

            return NdjsonNaming.Convert(declaredName, namingPolicy);
        }

        private static byte[] EncodeQuoted(string name)
        {
            byte[] encoded = JsonEscaping.Encode(name);
            byte[] result = new byte[encoded.Length + 2];
            result[0] = JsonConstants.Quote;
            Buffer.BlockCopy(encoded, 0, result, 1, encoded.Length);
            result[result.Length - 1] = JsonConstants.Quote;
            return result;
        }

        private static Func<T, long> CreateToInt64(Type underlying)
        {
            if (DynamicCodeSupport.IsSupported)
            {
                try
                {
                    ParameterExpression parameter = Expression.Parameter(typeof(T), "value");
                    Expression body = Expression.Convert(Expression.Convert(parameter, underlying), typeof(long));
                    return Expression.Lambda<Func<T, long>>(body, parameter).Compile();
                }
                catch (Exception)
                {
                }
            }

            if (underlying == typeof(ulong))
            {
                return value => unchecked((long)Convert.ToUInt64(value, CultureInfo.InvariantCulture));
            }

            return value => Convert.ToInt64(value, CultureInfo.InvariantCulture);
        }

        private static Func<long, T> CreateFromInt64(Type underlying)
        {
            if (DynamicCodeSupport.IsSupported)
            {
                try
                {
                    ParameterExpression parameter = Expression.Parameter(typeof(long), "value");
                    Expression body = Expression.Convert(Expression.Convert(parameter, underlying), typeof(T));
                    return Expression.Lambda<Func<long, T>>(body, parameter).Compile();
                }
                catch (Exception)
                {
                }
            }

            return value => (T)Enum.ToObject(typeof(T), value);
        }

        public override void Write(ref JsonWriter writer, in T value, NdjsonOptions options)
        {
            long numeric = _toInt64(value);

            if (!_forceString && !options.WriteEnumsAsStrings)
            {
                writer.WriteNumber(numeric);
                return;
            }

            long[] values = _values;
            for (int i = 0; i < values.Length; i++)
            {
                if (values[i] == numeric)
                {
                    writer.WritePreEncodedString(_encodedNames[i]);
                    return;
                }
            }

            if (_isFlags)
            {
                writer.WriteString(FormatFlags(numeric));
                return;
            }

            writer.WriteNumber(numeric);
        }

        public override T Read(ref JsonReader reader, NdjsonOptions options)
        {
            if (reader.TokenType == JsonTokenType.Number)
            {
                return _fromInt64(reader.GetInt64());
            }

            if (reader.TokenType == JsonTokenType.Null)
            {
                return default(T);
            }

            string text = reader.GetString();
            long numeric;
            if (_byName.TryGetValue(text, out numeric))
            {
                return _fromInt64(numeric);
            }

            if (_isFlags && text.IndexOf(',') >= 0)
            {
                long combined = 0;
                string[] parts = text.Split(',');
                for (int i = 0; i < parts.Length; i++)
                {
                    string part = parts[i].Trim();
                    long partValue;
                    if (_byName.TryGetValue(part, out partValue))
                    {
                        combined |= partValue;
                        continue;
                    }

                    T parsedPart;
                    if (Enum.TryParse<T>(part, true, out parsedPart))
                    {
                        combined |= _toInt64(parsedPart);
                        continue;
                    }

                    ThrowUnknown(part);
                }

                return _fromInt64(combined);
            }

            T parsed;
            if (Enum.TryParse<T>(text, true, out parsed))
            {
                return parsed;
            }

            long asNumber;
            if (long.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out asNumber))
            {
                return _fromInt64(asNumber);
            }

            ThrowUnknown(text);
            return default(T);
        }

        private string FormatFlags(long numeric)
        {
            System.Text.StringBuilder builder = new System.Text.StringBuilder();
            long remaining = numeric;

            for (int i = 0; i < _values.Length; i++)
            {
                long candidate = _values[i];
                if (candidate == 0)
                {
                    continue;
                }

                if ((remaining & candidate) == candidate)
                {
                    if (builder.Length > 0)
                    {
                        builder.Append(", ");
                    }

                    builder.Append(_names[i]);
                    remaining &= ~candidate;
                }
            }

            if (remaining != 0 || builder.Length == 0)
            {
                return numeric.ToString(CultureInfo.InvariantCulture);
            }

            return builder.ToString();
        }

        private static void ThrowUnknown(string text)
        {
            throw new NdjsonException("Valeur '" + text + "' inconnue pour l'enumeration " + typeof(T).Name + ".");
        }
    }
}
