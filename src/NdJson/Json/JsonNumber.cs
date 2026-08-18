using System;
using System.Globalization;
using System.Runtime.CompilerServices;

namespace NdJson
{
    internal static class JsonNumber
    {
        internal const int MaxInt64Length = 20;
        internal const int MaxDoubleLength = 32;

        private static readonly byte[] Digits2 = CreateDigits2();

        private static readonly double[] Pow10 =
        {
            1e0, 1e1, 1e2, 1e3, 1e4, 1e5, 1e6, 1e7, 1e8, 1e9, 1e10, 1e11,
            1e12, 1e13, 1e14, 1e15, 1e16, 1e17, 1e18, 1e19, 1e20, 1e21, 1e22
        };

        private static byte[] CreateDigits2()
        {
            byte[] table = new byte[200];
            for (int i = 0; i < 100; i++)
            {
                table[i * 2] = (byte)('0' + (i / 10));
                table[(i * 2) + 1] = (byte)('0' + (i % 10));
            }

            return table;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static int CountDigits(ulong value)
        {
            int digits = 1;
            if (value >= 10000000000000000UL)
            {
                value /= 10000000000000000UL;
                digits += 16;
            }

            if (value >= 100000000UL)
            {
                value /= 100000000UL;
                digits += 8;
            }

            if (value >= 10000UL)
            {
                value /= 10000UL;
                digits += 4;
            }

            if (value >= 100UL)
            {
                value /= 100UL;
                digits += 2;
            }

            if (value >= 10UL)
            {
                digits++;
            }

            return digits;
        }

        internal static int WriteUInt64(Span<byte> destination, ulong value)
        {
            int digits = CountDigits(value);
            int index = digits;
            byte[] table = Digits2;

            while (value >= 100)
            {
                ulong quotient = value / 100;
                uint offset = (uint)(value - (quotient * 100)) * 2;
                value = quotient;
                destination[--index] = table[offset + 1];
                destination[--index] = table[offset];
            }

            if (value < 10)
            {
                destination[--index] = (byte)('0' + (int)value);
            }
            else
            {
                uint offset = (uint)value * 2;
                destination[--index] = table[offset + 1];
                destination[--index] = table[offset];
            }

            return digits;
        }

        internal static int WriteInt64(Span<byte> destination, long value)
        {
            if (value >= 0)
            {
                return WriteUInt64(destination, (ulong)value);
            }

            destination[0] = JsonConstants.Minus;
            ulong magnitude = value == long.MinValue ? 9223372036854775808UL : (ulong)(-value);
            return 1 + WriteUInt64(destination.Slice(1), magnitude);
        }

        internal static int WriteDouble(Span<byte> destination, double value)
        {
#if NET8_0_OR_GREATER
            int written;
            if (value.TryFormat(destination, out written, "R", CultureInfo.InvariantCulture))
            {
                return written;
            }

            return -1;
#elif NETSTANDARD2_1
            Span<char> chars = stackalloc char[MaxDoubleLength];
            int charsWritten;
            if (!value.TryFormat(chars, out charsWritten, "R".AsSpan(), CultureInfo.InvariantCulture))
            {
                return -1;
            }

            if (charsWritten > destination.Length)
            {
                return -1;
            }

            for (int i = 0; i < charsWritten; i++)
            {
                destination[i] = (byte)chars[i];
            }

            return charsWritten;
#else
            string text = value.ToString("R", CultureInfo.InvariantCulture);
            if (text.Length > destination.Length)
            {
                return -1;
            }

            for (int i = 0; i < text.Length; i++)
            {
                destination[i] = (byte)text[i];
            }

            return text.Length;
#endif
        }

        internal static int WriteSingle(Span<byte> destination, float value)
        {
#if NET8_0_OR_GREATER
            int written;
            if (value.TryFormat(destination, out written, "R", CultureInfo.InvariantCulture))
            {
                return written;
            }

            return -1;
#elif NETSTANDARD2_1
            Span<char> chars = stackalloc char[MaxDoubleLength];
            int charsWritten;
            if (!value.TryFormat(chars, out charsWritten, "R".AsSpan(), CultureInfo.InvariantCulture))
            {
                return -1;
            }

            if (charsWritten > destination.Length)
            {
                return -1;
            }

            for (int i = 0; i < charsWritten; i++)
            {
                destination[i] = (byte)chars[i];
            }

            return charsWritten;
#else
            string text = value.ToString("R", CultureInfo.InvariantCulture);
            if (text.Length > destination.Length)
            {
                return -1;
            }

            for (int i = 0; i < text.Length; i++)
            {
                destination[i] = (byte)text[i];
            }

            return text.Length;
#endif
        }

        internal static int WriteDecimal(Span<byte> destination, decimal value)
        {
            string text = value.ToString(CultureInfo.InvariantCulture);
            if (text.Length > destination.Length)
            {
                return -1;
            }

            for (int i = 0; i < text.Length; i++)
            {
                destination[i] = (byte)text[i];
            }

            return text.Length;
        }

        internal static bool TryParseUInt64(ReadOnlySpan<byte> source, out ulong value)
        {
            value = 0;
            if (source.Length == 0 || source.Length > 20)
            {
                return false;
            }

            ulong accumulator = 0;
            for (int i = 0; i < source.Length; i++)
            {
                uint digit = (uint)(source[i] - JsonConstants.Zero);
                if (digit > 9)
                {
                    return false;
                }

                if (accumulator > 1844674407370955161UL)
                {
                    return false;
                }

                accumulator *= 10;
                if (accumulator > ulong.MaxValue - digit)
                {
                    return false;
                }

                accumulator += digit;
            }

            value = accumulator;
            return true;
        }

        internal static bool TryParseInt64(ReadOnlySpan<byte> source, out long value)
        {
            value = 0;
            if (source.Length == 0)
            {
                return false;
            }

            bool negative = source[0] == JsonConstants.Minus;
            if (negative || source[0] == JsonConstants.Plus)
            {
                source = source.Slice(1);
            }

            ulong magnitude;
            if (!TryParseUInt64(source, out magnitude))
            {
                return false;
            }

            if (negative)
            {
                if (magnitude > 9223372036854775808UL)
                {
                    return false;
                }

                value = magnitude == 9223372036854775808UL ? long.MinValue : -(long)magnitude;
                return true;
            }

            if (magnitude > long.MaxValue)
            {
                return false;
            }

            value = (long)magnitude;
            return true;
        }

        internal static bool TryParseDouble(ReadOnlySpan<byte> source, out double value)
        {
            value = 0;
            int length = source.Length;
            if (length == 0)
            {
                return false;
            }

            int index = 0;
            bool negative = false;
            byte first = source[0];
            if (first == JsonConstants.Minus)
            {
                negative = true;
                index = 1;
            }
            else if (first == JsonConstants.Plus)
            {
                index = 1;
            }

            ulong mantissa = 0;
            int significantDigits = 0;
            int decimalExponent = 0;
            bool anyDigits = false;
            bool overflowed = false;

            while (index < length)
            {
                uint digit = (uint)(source[index] - JsonConstants.Zero);
                if (digit > 9)
                {
                    break;
                }

                anyDigits = true;
                if (significantDigits < 19)
                {
                    if (mantissa != 0 || digit != 0)
                    {
                        mantissa = (mantissa * 10) + digit;
                        significantDigits++;
                    }
                }
                else
                {
                    decimalExponent++;
                    overflowed = true;
                }

                index++;
            }

            if (index < length && source[index] == JsonConstants.Period)
            {
                index++;
                while (index < length)
                {
                    uint digit = (uint)(source[index] - JsonConstants.Zero);
                    if (digit > 9)
                    {
                        break;
                    }

                    anyDigits = true;
                    if (significantDigits < 19)
                    {
                        if (mantissa != 0 || digit != 0)
                        {
                            mantissa = (mantissa * 10) + digit;
                            significantDigits++;
                            decimalExponent--;
                        }
                        else
                        {
                            decimalExponent--;
                        }
                    }
                    else
                    {
                        overflowed = true;
                    }

                    index++;
                }
            }

            if (!anyDigits)
            {
                return false;
            }

            if (index < length && (source[index] == (byte)'e' || source[index] == (byte)'E'))
            {
                index++;
                if (index >= length)
                {
                    return false;
                }

                bool exponentNegative = false;
                if (source[index] == JsonConstants.Minus)
                {
                    exponentNegative = true;
                    index++;
                }
                else if (source[index] == JsonConstants.Plus)
                {
                    index++;
                }

                int exponent = 0;
                bool anyExponentDigits = false;
                while (index < length)
                {
                    uint digit = (uint)(source[index] - JsonConstants.Zero);
                    if (digit > 9)
                    {
                        return false;
                    }

                    anyExponentDigits = true;
                    if (exponent < 100000)
                    {
                        exponent = (exponent * 10) + (int)digit;
                    }

                    index++;
                }

                if (!anyExponentDigits)
                {
                    return false;
                }

                decimalExponent += exponentNegative ? -exponent : exponent;
            }

            if (index != length)
            {
                return false;
            }

            if (!overflowed && significantDigits <= 15 && decimalExponent >= -22 && decimalExponent <= 22)
            {
                double result = mantissa;
                result = decimalExponent >= 0 ? result * Pow10[decimalExponent] : result / Pow10[-decimalExponent];
                value = negative ? -result : result;
                return true;
            }

            return ParseDoubleFallback(source, out value);
        }

        private static bool ParseDoubleFallback(ReadOnlySpan<byte> source, out double value)
        {
            string text = JsonEscaping.GetString(source);
            return double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out value);
        }

        internal static bool TryParseDecimal(ReadOnlySpan<byte> source, out decimal value)
        {
            string text = JsonEscaping.GetString(source);
            return decimal.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out value);
        }
    }
}
