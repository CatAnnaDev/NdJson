using System;

namespace NdJson.Serialization.Converters
{
    internal sealed class StringConverter : NdjsonConverter<string>
    {
        internal static readonly StringConverter Instance = new StringConverter();

        public override void Write(ref JsonWriter writer, in string value, NdjsonOptions options)
        {
            writer.WriteString(value);
        }

        public override string Read(ref JsonReader reader, NdjsonOptions options)
        {
            return reader.GetString();
        }
    }

    internal sealed class BooleanConverter : NdjsonConverter<bool>
    {
        internal static readonly BooleanConverter Instance = new BooleanConverter();

        public override void Write(ref JsonWriter writer, in bool value, NdjsonOptions options)
        {
            writer.WriteBoolean(value);
        }

        public override bool Read(ref JsonReader reader, NdjsonOptions options)
        {
            return reader.GetBoolean();
        }
    }

    internal sealed class ByteConverter : NdjsonConverter<byte>
    {
        internal static readonly ByteConverter Instance = new ByteConverter();

        public override void Write(ref JsonWriter writer, in byte value, NdjsonOptions options)
        {
            writer.WriteNumber((ulong)value);
        }

        public override byte Read(ref JsonReader reader, NdjsonOptions options)
        {
            return reader.GetByte();
        }
    }

    internal sealed class SByteConverter : NdjsonConverter<sbyte>
    {
        internal static readonly SByteConverter Instance = new SByteConverter();

        public override void Write(ref JsonWriter writer, in sbyte value, NdjsonOptions options)
        {
            writer.WriteNumber((long)value);
        }

        public override sbyte Read(ref JsonReader reader, NdjsonOptions options)
        {
            return reader.GetSByte();
        }
    }

    internal sealed class Int16Converter : NdjsonConverter<short>
    {
        internal static readonly Int16Converter Instance = new Int16Converter();

        public override void Write(ref JsonWriter writer, in short value, NdjsonOptions options)
        {
            writer.WriteNumber((long)value);
        }

        public override short Read(ref JsonReader reader, NdjsonOptions options)
        {
            return reader.GetInt16();
        }
    }

    internal sealed class UInt16Converter : NdjsonConverter<ushort>
    {
        internal static readonly UInt16Converter Instance = new UInt16Converter();

        public override void Write(ref JsonWriter writer, in ushort value, NdjsonOptions options)
        {
            writer.WriteNumber((ulong)value);
        }

        public override ushort Read(ref JsonReader reader, NdjsonOptions options)
        {
            return reader.GetUInt16();
        }
    }

    internal sealed class Int32Converter : NdjsonConverter<int>
    {
        internal static readonly Int32Converter Instance = new Int32Converter();

        public override void Write(ref JsonWriter writer, in int value, NdjsonOptions options)
        {
            writer.WriteNumber((long)value);
        }

        public override int Read(ref JsonReader reader, NdjsonOptions options)
        {
            return reader.GetInt32();
        }
    }

    internal sealed class UInt32Converter : NdjsonConverter<uint>
    {
        internal static readonly UInt32Converter Instance = new UInt32Converter();

        public override void Write(ref JsonWriter writer, in uint value, NdjsonOptions options)
        {
            writer.WriteNumber((ulong)value);
        }

        public override uint Read(ref JsonReader reader, NdjsonOptions options)
        {
            return reader.GetUInt32();
        }
    }

    internal sealed class Int64Converter : NdjsonConverter<long>
    {
        internal static readonly Int64Converter Instance = new Int64Converter();

        public override void Write(ref JsonWriter writer, in long value, NdjsonOptions options)
        {
            writer.WriteNumber(value);
        }

        public override long Read(ref JsonReader reader, NdjsonOptions options)
        {
            return reader.GetInt64();
        }
    }

    internal sealed class UInt64Converter : NdjsonConverter<ulong>
    {
        internal static readonly UInt64Converter Instance = new UInt64Converter();

        public override void Write(ref JsonWriter writer, in ulong value, NdjsonOptions options)
        {
            writer.WriteNumber(value);
        }

        public override ulong Read(ref JsonReader reader, NdjsonOptions options)
        {
            return reader.GetUInt64();
        }
    }

    internal sealed class SingleConverter : NdjsonConverter<float>
    {
        internal static readonly SingleConverter Instance = new SingleConverter();

        public override void Write(ref JsonWriter writer, in float value, NdjsonOptions options)
        {
            writer.WriteNumber(value, options.NonFiniteHandling);
        }

        public override float Read(ref JsonReader reader, NdjsonOptions options)
        {
            return reader.GetSingle();
        }
    }

    internal sealed class DoubleConverter : NdjsonConverter<double>
    {
        internal static readonly DoubleConverter Instance = new DoubleConverter();

        public override void Write(ref JsonWriter writer, in double value, NdjsonOptions options)
        {
            writer.WriteNumber(value, options.NonFiniteHandling);
        }

        public override double Read(ref JsonReader reader, NdjsonOptions options)
        {
            return reader.GetDouble();
        }
    }

    internal sealed class DecimalConverter : NdjsonConverter<decimal>
    {
        internal static readonly DecimalConverter Instance = new DecimalConverter();

        public override void Write(ref JsonWriter writer, in decimal value, NdjsonOptions options)
        {
            writer.WriteNumber(value);
        }

        public override decimal Read(ref JsonReader reader, NdjsonOptions options)
        {
            return reader.GetDecimal();
        }
    }

    internal sealed class CharConverter : NdjsonConverter<char>
    {
        internal static readonly CharConverter Instance = new CharConverter();

        public override void Write(ref JsonWriter writer, in char value, NdjsonOptions options)
        {
            writer.WriteString(value);
        }

        public override char Read(ref JsonReader reader, NdjsonOptions options)
        {
            return reader.GetChar();
        }
    }

    internal sealed class DateTimeConverter : NdjsonConverter<DateTime>
    {
        internal static readonly DateTimeConverter Instance = new DateTimeConverter();

        private readonly NdjsonDateFormat _format;

        internal DateTimeConverter()
        {
            _format = NdjsonDateFormat.Inherit;
        }

        internal DateTimeConverter(NdjsonDateFormat format)
        {
            _format = format;
        }

        private NdjsonDateFormat Format(NdjsonOptions options)
        {
            return _format == NdjsonDateFormat.Inherit ? options.DateFormat : _format;
        }

        public override void Write(ref JsonWriter writer, in DateTime value, NdjsonOptions options)
        {
            switch (Format(options))
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

        public override DateTime Read(ref JsonReader reader, NdjsonOptions options)
        {
            if (reader.TokenType == JsonTokenType.Number)
            {
                switch (Format(options))
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
    }

    internal sealed class DateTimeOffsetConverter : NdjsonConverter<DateTimeOffset>
    {
        internal static readonly DateTimeOffsetConverter Instance = new DateTimeOffsetConverter();

        private readonly NdjsonDateFormat _format;

        internal DateTimeOffsetConverter()
        {
            _format = NdjsonDateFormat.Inherit;
        }

        internal DateTimeOffsetConverter(NdjsonDateFormat format)
        {
            _format = format;
        }

        private NdjsonDateFormat Format(NdjsonOptions options)
        {
            return _format == NdjsonDateFormat.Inherit ? options.DateFormat : _format;
        }

        public override void Write(ref JsonWriter writer, in DateTimeOffset value, NdjsonOptions options)
        {
            switch (Format(options))
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

        public override DateTimeOffset Read(ref JsonReader reader, NdjsonOptions options)
        {
            if (reader.TokenType == JsonTokenType.Number)
            {
                switch (Format(options))
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
    }

    internal sealed class TimeSpanConverter : NdjsonConverter<TimeSpan>
    {
        internal static readonly TimeSpanConverter Instance = new TimeSpanConverter();

        public override void Write(ref JsonWriter writer, in TimeSpan value, NdjsonOptions options)
        {
            writer.WriteTimeSpan(value);
        }

        public override TimeSpan Read(ref JsonReader reader, NdjsonOptions options)
        {
            return reader.GetTimeSpan();
        }
    }

    internal sealed class GuidConverter : NdjsonConverter<Guid>
    {
        internal static readonly GuidConverter Instance = new GuidConverter();

        public override void Write(ref JsonWriter writer, in Guid value, NdjsonOptions options)
        {
            writer.WriteGuid(value);
        }

        public override Guid Read(ref JsonReader reader, NdjsonOptions options)
        {
            return reader.GetGuid();
        }
    }

    internal sealed class UriConverter : NdjsonConverter<Uri>
    {
        internal static readonly UriConverter Instance = new UriConverter();

        public override void Write(ref JsonWriter writer, in Uri value, NdjsonOptions options)
        {
            if (value == null)
            {
                writer.WriteNull();
                return;
            }

            writer.WriteString(value.OriginalString);
        }

        public override Uri Read(ref JsonReader reader, NdjsonOptions options)
        {
            string text = reader.GetString();
            return text == null ? null : new Uri(text, UriKind.RelativeOrAbsolute);
        }
    }

    internal sealed class ByteArrayConverter : NdjsonConverter<byte[]>
    {
        internal static readonly ByteArrayConverter Instance = new ByteArrayConverter();

        public override void Write(ref JsonWriter writer, in byte[] value, NdjsonOptions options)
        {
            if (value == null)
            {
                writer.WriteNull();
                return;
            }

            writer.WriteString(Convert.ToBase64String(value));
        }

        public override byte[] Read(ref JsonReader reader, NdjsonOptions options)
        {
            if (reader.TokenType == JsonTokenType.Null)
            {
                return null;
            }

            if (reader.TokenType == JsonTokenType.StartArray)
            {
                System.Collections.Generic.List<byte> bytes = new System.Collections.Generic.List<byte>();
                while (reader.ReadNextArrayElement())
                {
                    bytes.Add(reader.GetByte());
                }

                return bytes.ToArray();
            }

            return Convert.FromBase64String(reader.GetString());
        }
    }

    internal sealed class NdjsonValueConverter : NdjsonConverter<NdjsonValue>
    {
        internal static readonly NdjsonValueConverter Instance = new NdjsonValueConverter();

        public override void Write(ref JsonWriter writer, in NdjsonValue value, NdjsonOptions options)
        {
            if (value == null)
            {
                writer.WriteNull();
                return;
            }

            value.WriteTo(ref writer, options);
        }

        public override NdjsonValue Read(ref JsonReader reader, NdjsonOptions options)
        {
            return NdjsonValue.ReadCurrent(ref reader);
        }
    }

    internal sealed class ObjectConverter : NdjsonConverter<object>
    {
        internal static readonly ObjectConverter Instance = new ObjectConverter();

        public override void Write(ref JsonWriter writer, in object value, NdjsonOptions options)
        {
            if (value == null)
            {
                writer.WriteNull();
                return;
            }

            Type runtimeType = value.GetType();
            if (runtimeType == typeof(object))
            {
                writer.WriteStartObject();
                writer.WriteEndObject();
                return;
            }

            NdjsonConverter converter = options.GetConverter(runtimeType);
            converter.WriteObject(ref writer, value, options);
        }

        public override object Read(ref JsonReader reader, NdjsonOptions options)
        {
            return NdjsonValue.ReadCurrent(ref reader).ToClrObject();
        }
    }
}
