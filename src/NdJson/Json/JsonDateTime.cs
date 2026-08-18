using System;
using System.Globalization;

namespace NdJson
{
    internal static class JsonDateTime
    {
        internal const int MaxLength = 33;

        internal static int WriteDateTime(Span<byte> destination, DateTime value)
        {
            int written = WriteDateTimeCore(destination, value);

            if (value.Kind == DateTimeKind.Utc)
            {
                destination[written++] = (byte)'Z';
            }
            else if (value.Kind == DateTimeKind.Local)
            {
                TimeSpan offset = TimeZoneInfo.Local.GetUtcOffset(value);
                written += WriteOffset(destination.Slice(written), offset);
            }

            return written;
        }

        internal static int WriteDateTimeOffset(Span<byte> destination, DateTimeOffset value)
        {
            int written = WriteDateTimeCore(destination, value.DateTime);
            written += WriteOffset(destination.Slice(written), value.Offset);
            return written;
        }

        private static int WriteOffset(Span<byte> destination, TimeSpan offset)
        {
            int position = 0;
            long ticks = offset.Ticks;
            if (ticks < 0)
            {
                destination[position++] = (byte)'-';
                ticks = -ticks;
            }
            else
            {
                destination[position++] = (byte)'+';
            }

            int totalMinutes = (int)(ticks / TimeSpan.TicksPerMinute);
            WriteTwoDigits(destination, position, totalMinutes / 60);
            position += 2;
            destination[position++] = (byte)':';
            WriteTwoDigits(destination, position, totalMinutes % 60);
            position += 2;
            return position;
        }

        private static int WriteDateTimeCore(Span<byte> destination, DateTime value)
        {
            int position = 0;
            WriteFourDigits(destination, position, value.Year);
            position += 4;
            destination[position++] = (byte)'-';
            WriteTwoDigits(destination, position, value.Month);
            position += 2;
            destination[position++] = (byte)'-';
            WriteTwoDigits(destination, position, value.Day);
            position += 2;
            destination[position++] = (byte)'T';
            WriteTwoDigits(destination, position, value.Hour);
            position += 2;
            destination[position++] = (byte)':';
            WriteTwoDigits(destination, position, value.Minute);
            position += 2;
            destination[position++] = (byte)':';
            WriteTwoDigits(destination, position, value.Second);
            position += 2;

            long fraction = value.Ticks % TimeSpan.TicksPerSecond;
            if (fraction != 0)
            {
                destination[position++] = (byte)'.';
                int digits = 7;
                while (fraction % 10 == 0)
                {
                    fraction /= 10;
                    digits--;
                }

                for (int i = digits - 1; i >= 0; i--)
                {
                    destination[position + i] = (byte)('0' + (int)(fraction % 10));
                    fraction /= 10;
                }

                position += digits;
            }

            return position;
        }

        private static void WriteTwoDigits(Span<byte> destination, int offset, int value)
        {
            destination[offset] = (byte)('0' + (value / 10));
            destination[offset + 1] = (byte)('0' + (value % 10));
        }

        private static void WriteFourDigits(Span<byte> destination, int offset, int value)
        {
            destination[offset] = (byte)('0' + (value / 1000));
            destination[offset + 1] = (byte)('0' + ((value / 100) % 10));
            destination[offset + 2] = (byte)('0' + ((value / 10) % 10));
            destination[offset + 3] = (byte)('0' + (value % 10));
        }

        internal static bool TryParseDateTime(ReadOnlySpan<byte> source, out DateTime value)
        {
            value = default(DateTime);
            DateTimeOffset offsetValue;
            bool hasOffset;
            DateTimeKind kind;
            if (!TryParseCore(source, out offsetValue, out hasOffset, out kind))
            {
                return TryParseFallbackDateTime(source, out value);
            }

            if (kind == DateTimeKind.Utc)
            {
                value = DateTime.SpecifyKind(offsetValue.UtcDateTime, DateTimeKind.Utc);
                return true;
            }

            if (hasOffset)
            {
                value = offsetValue.LocalDateTime;
                return true;
            }

            value = DateTime.SpecifyKind(offsetValue.DateTime, DateTimeKind.Unspecified);
            return true;
        }

        internal static bool TryParseDateTimeOffset(ReadOnlySpan<byte> source, out DateTimeOffset value)
        {
            value = default(DateTimeOffset);
            bool hasOffset;
            DateTimeKind kind;
            if (!TryParseCore(source, out value, out hasOffset, out kind))
            {
                return TryParseFallbackDateTimeOffset(source, out value);
            }

            return true;
        }

        private static bool TryParseCore(ReadOnlySpan<byte> source, out DateTimeOffset value, out bool hasOffset, out DateTimeKind kind)
        {
            value = default(DateTimeOffset);
            hasOffset = false;
            kind = DateTimeKind.Unspecified;

            if (source.Length < 19)
            {
                return false;
            }

            int year;
            int month;
            int day;
            int hour;
            int minute;
            int second;

            if (!TryReadDigits(source, 0, 4, out year) ||
                source[4] != '-' ||
                !TryReadDigits(source, 5, 2, out month) ||
                source[7] != '-' ||
                !TryReadDigits(source, 8, 2, out day) ||
                (source[10] != 'T' && source[10] != ' ') ||
                !TryReadDigits(source, 11, 2, out hour) ||
                source[13] != ':' ||
                !TryReadDigits(source, 14, 2, out minute) ||
                source[16] != ':' ||
                !TryReadDigits(source, 17, 2, out second))
            {
                return false;
            }

            int position = 19;
            long fractionTicks = 0;

            if (position < source.Length && source[position] == '.')
            {
                position++;
                int digits = 0;
                while (position < source.Length && source[position] >= '0' && source[position] <= '9')
                {
                    if (digits < 7)
                    {
                        fractionTicks = (fractionTicks * 10) + (source[position] - '0');
                        digits++;
                    }

                    position++;
                }

                if (digits == 0)
                {
                    return false;
                }

                while (digits < 7)
                {
                    fractionTicks *= 10;
                    digits++;
                }
            }

            TimeSpan offset = TimeSpan.Zero;

            if (position < source.Length)
            {
                byte suffix = source[position];
                if (suffix == 'Z' || suffix == 'z')
                {
                    position++;
                    hasOffset = true;
                    kind = DateTimeKind.Utc;
                }
                else if (suffix == '+' || suffix == '-')
                {
                    int offsetHours;
                    int offsetMinutes = 0;
                    if (position + 3 > source.Length || !TryReadDigits(source, position + 1, 2, out offsetHours))
                    {
                        return false;
                    }

                    int next = position + 3;
                    if (next < source.Length && source[next] == ':')
                    {
                        if (!TryReadDigits(source, next + 1, 2, out offsetMinutes))
                        {
                            return false;
                        }

                        next += 3;
                    }
                    else if (next + 2 <= source.Length && source[next] >= '0' && source[next] <= '9')
                    {
                        if (!TryReadDigits(source, next, 2, out offsetMinutes))
                        {
                            return false;
                        }

                        next += 2;
                    }

                    offset = new TimeSpan(offsetHours, offsetMinutes, 0);
                    if (suffix == '-')
                    {
                        offset = -offset;
                    }

                    position = next;
                    hasOffset = true;
                    kind = offset == TimeSpan.Zero ? DateTimeKind.Utc : DateTimeKind.Local;
                }
            }

            if (position != source.Length)
            {
                return false;
            }

            if (year < 1 || month < 1 || month > 12 || day < 1 || day > DateTime.DaysInMonth(year, month) ||
                hour > 23 || minute > 59 || second > 59)
            {
                return false;
            }

            DateTime local = new DateTime(year, month, day, hour, minute, second, DateTimeKind.Unspecified);
            local = local.AddTicks(fractionTicks);
            value = new DateTimeOffset(local, offset);
            return true;
        }

        private static bool TryReadDigits(ReadOnlySpan<byte> source, int offset, int count, out int value)
        {
            value = 0;
            if (offset + count > source.Length)
            {
                return false;
            }

            int result = 0;
            for (int i = 0; i < count; i++)
            {
                uint digit = (uint)(source[offset + i] - JsonConstants.Zero);
                if (digit > 9)
                {
                    return false;
                }

                result = (result * 10) + (int)digit;
            }

            value = result;
            return true;
        }

        private static bool TryParseFallbackDateTime(ReadOnlySpan<byte> source, out DateTime value)
        {
            string text = JsonEscaping.GetString(source);
            return DateTime.TryParse(text, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out value);
        }

        private static bool TryParseFallbackDateTimeOffset(ReadOnlySpan<byte> source, out DateTimeOffset value)
        {
            string text = JsonEscaping.GetString(source);
            return DateTimeOffset.TryParse(text, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out value);
        }

        internal static DateTime FromUnixSeconds(double seconds)
        {
            return new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc).AddTicks((long)(seconds * TimeSpan.TicksPerSecond));
        }

        internal static DateTime FromUnixMilliseconds(double milliseconds)
        {
            return new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc).AddTicks((long)(milliseconds * TimeSpan.TicksPerMillisecond));
        }

        internal static double ToUnixSeconds(DateTime value)
        {
            DateTime utc = value.Kind == DateTimeKind.Unspecified ? DateTime.SpecifyKind(value, DateTimeKind.Utc) : value.ToUniversalTime();
            return (utc - new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc)).TotalSeconds;
        }

        internal static double ToUnixMilliseconds(DateTime value)
        {
            DateTime utc = value.Kind == DateTimeKind.Unspecified ? DateTime.SpecifyKind(value, DateTimeKind.Utc) : value.ToUniversalTime();
            return (utc - new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc)).TotalMilliseconds;
        }
    }

    internal static class JsonGuidHelper
    {
        internal const int Length = 36;

        private static readonly byte[] HexLower = { (byte)'0', (byte)'1', (byte)'2', (byte)'3', (byte)'4', (byte)'5', (byte)'6', (byte)'7', (byte)'8', (byte)'9', (byte)'a', (byte)'b', (byte)'c', (byte)'d', (byte)'e', (byte)'f' };

        internal static int Write(Span<byte> destination, Guid value)
        {
            byte[] bytes = value.ToByteArray();
            int position = 0;

            position = WriteHex(destination, position, bytes[3]);
            position = WriteHex(destination, position, bytes[2]);
            position = WriteHex(destination, position, bytes[1]);
            position = WriteHex(destination, position, bytes[0]);
            destination[position++] = (byte)'-';
            position = WriteHex(destination, position, bytes[5]);
            position = WriteHex(destination, position, bytes[4]);
            destination[position++] = (byte)'-';
            position = WriteHex(destination, position, bytes[7]);
            position = WriteHex(destination, position, bytes[6]);
            destination[position++] = (byte)'-';
            position = WriteHex(destination, position, bytes[8]);
            position = WriteHex(destination, position, bytes[9]);
            destination[position++] = (byte)'-';
            for (int i = 10; i < 16; i++)
            {
                position = WriteHex(destination, position, bytes[i]);
            }

            return position;
        }

        private static int WriteHex(Span<byte> destination, int position, byte value)
        {
            destination[position] = HexLower[value >> 4];
            destination[position + 1] = HexLower[value & 0xF];
            return position + 2;
        }

        internal static bool TryParse(ReadOnlySpan<byte> source, out Guid value)
        {
            if (source.Length == Length)
            {
                Span<char> chars = stackalloc char[Length];
                for (int i = 0; i < Length; i++)
                {
                    byte c = source[i];
                    if (c > 127)
                    {
                        value = default(Guid);
                        return false;
                    }

                    chars[i] = (char)c;
                }

#if NETSTANDARD2_0
                return Guid.TryParse(new string(chars.ToArray()), out value);
#else
                return Guid.TryParse(chars, out value);
#endif
            }

            return Guid.TryParse(JsonEscaping.GetString(source), out value);
        }
    }
}
