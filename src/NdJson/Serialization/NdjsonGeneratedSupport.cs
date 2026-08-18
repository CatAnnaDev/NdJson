using System;
using System.Collections.Generic;

namespace NdJson.Serialization
{
    public static class NdjsonGeneratedSupport
    {
        public static byte[] EncodeName(string name)
        {
            return JsonEscaping.EncodePropertyName(name);
        }

        public static byte[] EncodeUtf8(string value)
        {
            return JsonEscaping.Encode(value);
        }

        public static byte[] EncodeQuoted(string value)
        {
            byte[] encoded = JsonEscaping.Encode(value);
            byte[] result = new byte[encoded.Length + 2];
            result[0] = JsonConstants.Quote;
            Buffer.BlockCopy(encoded, 0, result, 1, encoded.Length);
            result[result.Length - 1] = JsonConstants.Quote;
            return result;
        }

        public static bool NameEquals(ReadOnlySpan<byte> name, byte[] candidate)
        {
            return name.SequenceEqual(new ReadOnlySpan<byte>(candidate));
        }

        public static bool NameEqualsIgnoreCase(ReadOnlySpan<byte> name, byte[] candidate)
        {
            if (name.Length != candidate.Length)
            {
                return false;
            }

            for (int i = 0; i < name.Length; i++)
            {
                byte left = name[i];
                byte right = candidate[i];
                if (left == right)
                {
                    continue;
                }

                if (left >= 'A' && left <= 'Z')
                {
                    left = (byte)(left + 32);
                }

                if (right >= 'A' && right <= 'Z')
                {
                    right = (byte)(right + 32);
                }

                if (left != right)
                {
                    return false;
                }
            }

            return true;
        }

        public static void WriteDateTime(ref JsonWriter writer, DateTime value, NdjsonOptions options, NdjsonDateFormat format)
        {
            NdjsonDateFormat effective = format == NdjsonDateFormat.Inherit ? options.DateFormat : format;
            switch (effective)
            {
                case NdjsonDateFormat.UnixSeconds:
                    writer.WriteNumber((long)JsonDateTime.ToUnixSeconds(value));
                    return;
                case NdjsonDateFormat.UnixMilliseconds:
                    writer.WriteNumber((long)JsonDateTime.ToUnixMilliseconds(value));
                    return;
                case NdjsonDateFormat.Ticks:
                    writer.WriteNumber(value.Ticks);
                    return;
                default:
                    writer.WriteDateTime(value);
                    return;
            }
        }

        public static DateTime ReadDateTime(ref JsonReader reader, NdjsonOptions options, NdjsonDateFormat format)
        {
            NdjsonDateFormat effective = format == NdjsonDateFormat.Inherit ? options.DateFormat : format;
            if (reader.TokenType == JsonTokenType.Number)
            {
                switch (effective)
                {
                    case NdjsonDateFormat.UnixSeconds:
                        return JsonDateTime.FromUnixSeconds(reader.GetDouble());
                    case NdjsonDateFormat.Ticks:
                        return new DateTime(reader.GetInt64(), DateTimeKind.Utc);
                    default:
                        return JsonDateTime.FromUnixMilliseconds(reader.GetDouble());
                }
            }

            return reader.GetDateTime();
        }

        public static void WriteDateTimeOffset(ref JsonWriter writer, DateTimeOffset value, NdjsonOptions options, NdjsonDateFormat format)
        {
            NdjsonDateFormat effective = format == NdjsonDateFormat.Inherit ? options.DateFormat : format;
            switch (effective)
            {
                case NdjsonDateFormat.UnixSeconds:
                    writer.WriteNumber(value.ToUnixTimeSeconds());
                    return;
                case NdjsonDateFormat.UnixMilliseconds:
                    writer.WriteNumber(value.ToUnixTimeMilliseconds());
                    return;
                case NdjsonDateFormat.Ticks:
                    writer.WriteNumber(value.UtcTicks);
                    return;
                default:
                    writer.WriteDateTimeOffset(value);
                    return;
            }
        }

        public static DateTimeOffset ReadDateTimeOffset(ref JsonReader reader, NdjsonOptions options, NdjsonDateFormat format)
        {
            NdjsonDateFormat effective = format == NdjsonDateFormat.Inherit ? options.DateFormat : format;
            if (reader.TokenType == JsonTokenType.Number)
            {
                switch (effective)
                {
                    case NdjsonDateFormat.UnixSeconds:
                        return DateTimeOffset.FromUnixTimeSeconds(reader.GetInt64());
                    case NdjsonDateFormat.Ticks:
                        return new DateTimeOffset(reader.GetInt64(), TimeSpan.Zero);
                    default:
                        return DateTimeOffset.FromUnixTimeMilliseconds(reader.GetInt64());
                }
            }

            return reader.GetDateTimeOffset();
        }

        public static void ThrowMissingRequired(string memberName, string typeName)
        {
            throw new NdjsonException("Propriete requise absente : '" + memberName + "' sur " + typeName + ".");
        }

        public static void ThrowUnknownEnum(string value, string enumName)
        {
            throw new NdjsonException("Valeur '" + value + "' inconnue pour l'enumeration " + enumName + ".");
        }

        public static void ThrowUnknownDiscriminator(string value, string typeName)
        {
            throw new NdjsonException("Discriminateur '" + value + "' inconnu pour " + typeName + ".");
        }

        public static void ThrowMissingDiscriminator(string typeName)
        {
            throw new NdjsonException("Discriminateur absent : impossible de determiner le type derive de " + typeName + ".");
        }

        public static Dictionary<string, NdjsonValue> NewExtensionData()
        {
            return new Dictionary<string, NdjsonValue>(StringComparer.Ordinal);
        }
    }
}
