using System;
using System.Text;

namespace NdJson
{
    internal static class JsonEscaping
    {
        internal static readonly bool[] NeedsEscape = CreateEscapeTable();

        private static readonly char[] HexDigits = { '0', '1', '2', '3', '4', '5', '6', '7', '8', '9', 'A', 'B', 'C', 'D', 'E', 'F' };

        private static bool[] CreateEscapeTable()
        {
            bool[] table = new bool[256];
            for (int i = 0; i < 0x20; i++)
            {
                table[i] = true;
            }

            table[JsonConstants.Quote] = true;
            table[JsonConstants.BackSlash] = true;
            return table;
        }

        internal static byte ToHexDigit(int value)
        {
            return (byte)HexDigits[value & 0xF];
        }

        internal static int Unescape(ReadOnlySpan<byte> source, Span<byte> destination)
        {
            int written = 0;
            int index = 0;

            while (index < source.Length)
            {
                byte current = source[index];
                if (current != JsonConstants.BackSlash)
                {
                    int next = source.Slice(index).IndexOf(JsonConstants.BackSlash);
                    int length = next < 0 ? source.Length - index : next;
                    source.Slice(index, length).CopyTo(destination.Slice(written));
                    written += length;
                    index += length;
                    continue;
                }

                index++;
                if (index >= source.Length)
                {
                    ThrowInvalidEscape();
                }

                byte escaped = source[index++];
                switch (escaped)
                {
                    case (byte)'"':
                        destination[written++] = JsonConstants.Quote;
                        break;
                    case (byte)'\\':
                        destination[written++] = JsonConstants.BackSlash;
                        break;
                    case (byte)'/':
                        destination[written++] = JsonConstants.Slash;
                        break;
                    case (byte)'b':
                        destination[written++] = 0x08;
                        break;
                    case (byte)'f':
                        destination[written++] = 0x0C;
                        break;
                    case (byte)'n':
                        destination[written++] = 0x0A;
                        break;
                    case (byte)'r':
                        destination[written++] = 0x0D;
                        break;
                    case (byte)'t':
                        destination[written++] = 0x09;
                        break;
                    case (byte)'u':
                        {
                            if (index + 4 > source.Length)
                            {
                                ThrowInvalidEscape();
                            }

                            int codePoint = ReadHex4(source.Slice(index));
                            index += 4;

                            if (codePoint >= 0xD800 && codePoint <= 0xDBFF)
                            {
                                if (index + 6 <= source.Length && source[index] == JsonConstants.BackSlash && source[index + 1] == (byte)'u')
                                {
                                    int low = ReadHex4(source.Slice(index + 2));
                                    if (low >= 0xDC00 && low <= 0xDFFF)
                                    {
                                        index += 6;
                                        codePoint = 0x10000 + ((codePoint - 0xD800) << 10) + (low - 0xDC00);
                                    }
                                    else
                                    {
                                        codePoint = 0xFFFD;
                                    }
                                }
                                else
                                {
                                    codePoint = 0xFFFD;
                                }
                            }
                            else if (codePoint >= 0xDC00 && codePoint <= 0xDFFF)
                            {
                                codePoint = 0xFFFD;
                            }

                            written += WriteCodePoint(codePoint, destination.Slice(written));
                            break;
                        }
                    default:
                        ThrowInvalidEscape();
                        break;
                }
            }

            return written;
        }

        internal static int WriteCodePoint(int codePoint, Span<byte> destination)
        {
            if (codePoint < 0x80)
            {
                destination[0] = (byte)codePoint;
                return 1;
            }

            if (codePoint < 0x800)
            {
                destination[0] = (byte)(0xC0 | (codePoint >> 6));
                destination[1] = (byte)(0x80 | (codePoint & 0x3F));
                return 2;
            }

            if (codePoint < 0x10000)
            {
                destination[0] = (byte)(0xE0 | (codePoint >> 12));
                destination[1] = (byte)(0x80 | ((codePoint >> 6) & 0x3F));
                destination[2] = (byte)(0x80 | (codePoint & 0x3F));
                return 3;
            }

            destination[0] = (byte)(0xF0 | (codePoint >> 18));
            destination[1] = (byte)(0x80 | ((codePoint >> 12) & 0x3F));
            destination[2] = (byte)(0x80 | ((codePoint >> 6) & 0x3F));
            destination[3] = (byte)(0x80 | (codePoint & 0x3F));
            return 4;
        }

        private static int ReadHex4(ReadOnlySpan<byte> source)
        {
            int value = 0;
            for (int i = 0; i < 4; i++)
            {
                int digit = HexValue(source[i]);
                if (digit < 0)
                {
                    ThrowInvalidEscape();
                }

                value = (value << 4) | digit;
            }

            return value;
        }

        private static int HexValue(byte c)
        {
            if (c >= '0' && c <= '9')
            {
                return c - '0';
            }

            if (c >= 'a' && c <= 'f')
            {
                return c - 'a' + 10;
            }

            if (c >= 'A' && c <= 'F')
            {
                return c - 'A' + 10;
            }

            return -1;
        }

        internal static string GetString(ReadOnlySpan<byte> utf8)
        {
            if (utf8.Length == 0)
            {
                return string.Empty;
            }

#if NETSTANDARD2_0
            return Encoding.UTF8.GetString(utf8.ToArray());
#else
            return Encoding.UTF8.GetString(utf8);
#endif
        }

        internal static byte[] Encode(string value)
        {
            return Encoding.UTF8.GetBytes(value);
        }

        internal static byte[] EncodePropertyName(string name)
        {
            byte[] raw = Encoding.UTF8.GetBytes(name);
            int extra = 0;
            for (int i = 0; i < raw.Length; i++)
            {
                if (NeedsEscape[raw[i]])
                {
                    extra += 5;
                }
            }

            byte[] result = new byte[raw.Length + extra + 3];
            int position = 0;
            result[position++] = JsonConstants.Quote;

            for (int i = 0; i < raw.Length; i++)
            {
                byte c = raw[i];
                if (!NeedsEscape[c])
                {
                    result[position++] = c;
                    continue;
                }

                result[position++] = JsonConstants.BackSlash;
                switch (c)
                {
                    case JsonConstants.Quote:
                        result[position++] = JsonConstants.Quote;
                        break;
                    case JsonConstants.BackSlash:
                        result[position++] = JsonConstants.BackSlash;
                        break;
                    case 0x08:
                        result[position++] = (byte)'b';
                        break;
                    case 0x0C:
                        result[position++] = (byte)'f';
                        break;
                    case 0x0A:
                        result[position++] = (byte)'n';
                        break;
                    case 0x0D:
                        result[position++] = (byte)'r';
                        break;
                    case 0x09:
                        result[position++] = (byte)'t';
                        break;
                    default:
                        result[position++] = (byte)'u';
                        result[position++] = (byte)'0';
                        result[position++] = (byte)'0';
                        result[position++] = ToHexDigit(c >> 4);
                        result[position++] = ToHexDigit(c);
                        break;
                }
            }

            result[position++] = JsonConstants.Quote;
            result[position++] = JsonConstants.Colon;

            if (position == result.Length)
            {
                return result;
            }

            byte[] trimmed = new byte[position];
            Buffer.BlockCopy(result, 0, trimmed, 0, position);
            return trimmed;
        }

        private static void ThrowInvalidEscape()
        {
            throw new NdjsonException("Sequence d'echappement JSON invalide.");
        }
    }
}
