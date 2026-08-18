using System;
using System.Buffers;
using System.Runtime.CompilerServices;

namespace NdJson
{
    public ref struct JsonWriter
    {
        private byte[] _buffer;
        private int _position;
        private bool _rented;
        private bool _needSeparator;
        private int _depth;

        public JsonWriter(byte[] buffer)
            : this(buffer, 0, false)
        {
        }

        public JsonWriter(byte[] buffer, int position, bool bufferIsRented)
        {
            if (buffer == null)
            {
                throw new ArgumentNullException(nameof(buffer));
            }

            _buffer = buffer;
            _position = position;
            _rented = bufferIsRented;
            _needSeparator = false;
            _depth = 0;
        }

        public static JsonWriter Create(int initialCapacity)
        {
            return new JsonWriter(ArrayPool<byte>.Shared.Rent(initialCapacity < 256 ? 256 : initialCapacity), 0, true);
        }

        public byte[] Buffer
        {
            get { return _buffer; }
        }

        public int BytesWritten
        {
            get { return _position; }
        }

        public bool BufferIsRented
        {
            get { return _rented; }
        }

        public int Depth
        {
            get { return _depth; }
        }

        public void ResetTo(int position)
        {
            _position = position;
            _needSeparator = false;
            _depth = 0;
        }

        public ReadOnlySpan<byte> WrittenSpan
        {
            get { return new ReadOnlySpan<byte>(_buffer, 0, _position); }
        }

        public byte[] ToArray()
        {
            byte[] result = new byte[_position];
            System.Buffer.BlockCopy(_buffer, 0, result, 0, _position);
            return result;
        }

        public void Release()
        {
            if (_rented && _buffer != null)
            {
                ArrayPool<byte>.Shared.Return(_buffer);
                _buffer = null;
                _rented = false;
                _position = 0;
            }
        }

        public void WriteStartObject()
        {
            WriteSeparator();
            EnsureRoom(1);
            _buffer[_position++] = JsonConstants.OpenBrace;
            _needSeparator = false;
            _depth++;
        }

        public void WriteEndObject()
        {
            EnsureRoom(1);
            _buffer[_position++] = JsonConstants.CloseBrace;
            _needSeparator = true;
            _depth--;
        }

        public void WriteStartArray()
        {
            WriteSeparator();
            EnsureRoom(1);
            _buffer[_position++] = JsonConstants.OpenBracket;
            _needSeparator = false;
            _depth++;
        }

        public void WriteEndArray()
        {
            EnsureRoom(1);
            _buffer[_position++] = JsonConstants.CloseBracket;
            _needSeparator = true;
            _depth--;
        }

        public void WritePropertyName(byte[] encodedNameWithColon)
        {
            WriteSeparator();
            int length = encodedNameWithColon.Length;
            EnsureRoom(length);
            System.Buffer.BlockCopy(encodedNameWithColon, 0, _buffer, _position, length);
            _position += length;
            _needSeparator = false;
        }

        public void WritePropertyName(string name)
        {
            WriteSeparator();
            WriteStringCore(name);
            EnsureRoom(1);
            _buffer[_position++] = JsonConstants.Colon;
            _needSeparator = false;
        }

        public void WriteNull()
        {
            WriteSeparator();
            EnsureRoom(4);
            _buffer[_position] = (byte)'n';
            _buffer[_position + 1] = (byte)'u';
            _buffer[_position + 2] = (byte)'l';
            _buffer[_position + 3] = (byte)'l';
            _position += 4;
            _needSeparator = true;
        }

        public void WriteBoolean(bool value)
        {
            WriteSeparator();
            if (value)
            {
                EnsureRoom(4);
                _buffer[_position] = (byte)'t';
                _buffer[_position + 1] = (byte)'r';
                _buffer[_position + 2] = (byte)'u';
                _buffer[_position + 3] = (byte)'e';
                _position += 4;
            }
            else
            {
                EnsureRoom(5);
                _buffer[_position] = (byte)'f';
                _buffer[_position + 1] = (byte)'a';
                _buffer[_position + 2] = (byte)'l';
                _buffer[_position + 3] = (byte)'s';
                _buffer[_position + 4] = (byte)'e';
                _position += 5;
            }

            _needSeparator = true;
        }

        public void WriteNumber(long value)
        {
            WriteSeparator();
            EnsureRoom(JsonNumber.MaxInt64Length);
            _position += JsonNumber.WriteInt64(new Span<byte>(_buffer, _position, JsonNumber.MaxInt64Length), value);
            _needSeparator = true;
        }

        public void WriteNumber(ulong value)
        {
            WriteSeparator();
            EnsureRoom(JsonNumber.MaxInt64Length);
            _position += JsonNumber.WriteUInt64(new Span<byte>(_buffer, _position, JsonNumber.MaxInt64Length), value);
            _needSeparator = true;
        }

        public void WriteNumber(int value)
        {
            WriteNumber((long)value);
        }

        public void WriteNumber(uint value)
        {
            WriteNumber((ulong)value);
        }

        public void WriteNumber(double value)
        {
            WriteNumber(value, NdjsonNonFiniteHandling.Throw);
        }

        public void WriteNumber(double value, NdjsonNonFiniteHandling handling)
        {
            if (double.IsNaN(value) || double.IsInfinity(value))
            {
                WriteNonFinite(value, handling);
                return;
            }

            WriteSeparator();
            EnsureRoom(JsonNumber.MaxDoubleLength);
            int written = JsonNumber.WriteDouble(new Span<byte>(_buffer, _position, JsonNumber.MaxDoubleLength), value);
            if (written < 0)
            {
                ThrowFormatFailure();
            }

            _position += written;
            _needSeparator = true;
        }

        public void WriteNumber(float value)
        {
            WriteNumber(value, NdjsonNonFiniteHandling.Throw);
        }

        public void WriteNumber(float value, NdjsonNonFiniteHandling handling)
        {
            if (float.IsNaN(value) || float.IsInfinity(value))
            {
                WriteNonFinite(value, handling);
                return;
            }

            WriteSeparator();
            EnsureRoom(JsonNumber.MaxDoubleLength);
            int written = JsonNumber.WriteSingle(new Span<byte>(_buffer, _position, JsonNumber.MaxDoubleLength), value);
            if (written < 0)
            {
                ThrowFormatFailure();
            }

            _position += written;
            _needSeparator = true;
        }

        public void WriteNumber(decimal value)
        {
            WriteSeparator();
            EnsureRoom(48);
            int written = JsonNumber.WriteDecimal(new Span<byte>(_buffer, _position, 48), value);
            if (written < 0)
            {
                ThrowFormatFailure();
            }

            _position += written;
            _needSeparator = true;
        }

        private void WriteNonFinite(double value, NdjsonNonFiniteHandling handling)
        {
            switch (handling)
            {
                case NdjsonNonFiniteHandling.WriteNull:
                    WriteNull();
                    return;
                case NdjsonNonFiniteHandling.WriteString:
                    {
                        byte[] literal;
                        if (double.IsNaN(value))
                        {
                            literal = JsonConstants.NaNLiteral;
                        }
                        else if (value > 0)
                        {
                            literal = JsonConstants.PositiveInfinityLiteral;
                        }
                        else
                        {
                            literal = JsonConstants.NegativeInfinityLiteral;
                        }

                        WriteSeparator();
                        EnsureRoom(literal.Length);
                        System.Buffer.BlockCopy(literal, 0, _buffer, _position, literal.Length);
                        _position += literal.Length;
                        _needSeparator = true;
                        return;
                    }
                default:
                    throw new NdjsonException("Impossible d'ecrire la valeur flottante non finie " + value.ToString(System.Globalization.CultureInfo.InvariantCulture) + " en JSON. Utilisez NdjsonOptions.NonFiniteHandling.");
            }
        }

        public void WriteString(string value)
        {
            if (value == null)
            {
                WriteNull();
                return;
            }

            WriteSeparator();
            WriteStringCore(value);
            _needSeparator = true;
        }

        public void WriteString(char value)
        {
            WriteSeparator();
            EnsureRoom(8);
            _buffer[_position++] = JsonConstants.Quote;
            AppendChar(value);
            EnsureRoom(1);
            _buffer[_position++] = JsonConstants.Quote;
            _needSeparator = true;
        }

        public void WriteStringUtf8(ReadOnlySpan<byte> utf8Value)
        {
            WriteSeparator();
            EnsureRoom(utf8Value.Length + 2);
            _buffer[_position++] = JsonConstants.Quote;

            bool[] table = JsonEscaping.NeedsEscape;
            int start = 0;
            for (int i = 0; i < utf8Value.Length; i++)
            {
                byte current = utf8Value[i];
                if (!table[current])
                {
                    continue;
                }

                int length = i - start;
                if (length > 0)
                {
                    EnsureRoom(length);
                    utf8Value.Slice(start, length).CopyTo(new Span<byte>(_buffer, _position, length));
                    _position += length;
                }

                AppendEscapedAscii(current);
                start = i + 1;
            }

            int tail = utf8Value.Length - start;
            if (tail > 0)
            {
                EnsureRoom(tail);
                utf8Value.Slice(start, tail).CopyTo(new Span<byte>(_buffer, _position, tail));
                _position += tail;
            }

            EnsureRoom(1);
            _buffer[_position++] = JsonConstants.Quote;
            _needSeparator = true;
        }

        public void WriteRawValue(ReadOnlySpan<byte> utf8Json)
        {
            WriteSeparator();
            EnsureRoom(utf8Json.Length);
            utf8Json.CopyTo(new Span<byte>(_buffer, _position, utf8Json.Length));
            _position += utf8Json.Length;
            _needSeparator = true;
        }

        public void WritePreEncodedString(byte[] quotedUtf8)
        {
            WriteSeparator();
            EnsureRoom(quotedUtf8.Length);
            System.Buffer.BlockCopy(quotedUtf8, 0, _buffer, _position, quotedUtf8.Length);
            _position += quotedUtf8.Length;
            _needSeparator = true;
        }

        public void WriteDateTime(DateTime value)
        {
            WriteSeparator();
            EnsureRoom(JsonDateTime.MaxLength + 2);
            _buffer[_position++] = JsonConstants.Quote;
            _position += JsonDateTime.WriteDateTime(new Span<byte>(_buffer, _position, JsonDateTime.MaxLength), value);
            _buffer[_position++] = JsonConstants.Quote;
            _needSeparator = true;
        }

        public void WriteDateTimeOffset(DateTimeOffset value)
        {
            WriteSeparator();
            EnsureRoom(JsonDateTime.MaxLength + 2);
            _buffer[_position++] = JsonConstants.Quote;
            _position += JsonDateTime.WriteDateTimeOffset(new Span<byte>(_buffer, _position, JsonDateTime.MaxLength), value);
            _buffer[_position++] = JsonConstants.Quote;
            _needSeparator = true;
        }

        public void WriteGuid(Guid value)
        {
            WriteSeparator();
            EnsureRoom(JsonGuidHelper.Length + 2);
            _buffer[_position++] = JsonConstants.Quote;
            _position += JsonGuidHelper.Write(new Span<byte>(_buffer, _position, JsonGuidHelper.Length), value);
            _buffer[_position++] = JsonConstants.Quote;
            _needSeparator = true;
        }

        public void WriteTimeSpan(TimeSpan value)
        {
            WriteString(value.ToString("c", System.Globalization.CultureInfo.InvariantCulture));
        }

        public void WriteNewLine()
        {
            EnsureRoom(1);
            _buffer[_position++] = JsonConstants.LineFeed;
            _needSeparator = false;
            _depth = 0;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void WriteSeparator()
        {
            if (_needSeparator)
            {
                EnsureRoom(1);
                _buffer[_position++] = JsonConstants.Comma;
            }
        }

        private void WriteStringCore(string value)
        {
            long required = ((long)value.Length * 3) + 2;
            if (required > int.MaxValue)
            {
                throw new NdjsonException("Chaine trop longue pour etre encodee en JSON.");
            }

            EnsureRoom((int)required);
            _buffer[_position++] = JsonConstants.Quote;

            bool[] table = JsonEscaping.NeedsEscape;
            for (int i = 0; i < value.Length; i++)
            {
                char current = value[i];
                if (current < 0x80)
                {
                    if (table[current])
                    {
                        AppendEscapedAscii((byte)current);
                        continue;
                    }

                    _buffer[_position++] = (byte)current;
                    continue;
                }

                if (current < 0x800)
                {
                    _buffer[_position++] = (byte)(0xC0 | (current >> 6));
                    _buffer[_position++] = (byte)(0x80 | (current & 0x3F));
                    continue;
                }

                if (current >= 0xD800 && current <= 0xDBFF && i + 1 < value.Length)
                {
                    char low = value[i + 1];
                    if (low >= 0xDC00 && low <= 0xDFFF)
                    {
                        int codePoint = 0x10000 + ((current - 0xD800) << 10) + (low - 0xDC00);
                        _buffer[_position++] = (byte)(0xF0 | (codePoint >> 18));
                        _buffer[_position++] = (byte)(0x80 | ((codePoint >> 12) & 0x3F));
                        _buffer[_position++] = (byte)(0x80 | ((codePoint >> 6) & 0x3F));
                        _buffer[_position++] = (byte)(0x80 | (codePoint & 0x3F));
                        i++;
                        continue;
                    }
                }

                int scalar = (current >= 0xD800 && current <= 0xDFFF) ? 0xFFFD : current;
                _buffer[_position++] = (byte)(0xE0 | (scalar >> 12));
                _buffer[_position++] = (byte)(0x80 | ((scalar >> 6) & 0x3F));
                _buffer[_position++] = (byte)(0x80 | (scalar & 0x3F));
            }

            EnsureRoom(1);
            _buffer[_position++] = JsonConstants.Quote;
        }

        private void AppendChar(char value)
        {
            if (value < 0x80)
            {
                if (JsonEscaping.NeedsEscape[value])
                {
                    AppendEscapedAscii((byte)value);
                    return;
                }

                EnsureRoom(1);
                _buffer[_position++] = (byte)value;
                return;
            }

            EnsureRoom(3);
            int scalar = (value >= 0xD800 && value <= 0xDFFF) ? 0xFFFD : value;
            if (scalar < 0x800)
            {
                _buffer[_position++] = (byte)(0xC0 | (scalar >> 6));
                _buffer[_position++] = (byte)(0x80 | (scalar & 0x3F));
                return;
            }

            _buffer[_position++] = (byte)(0xE0 | (scalar >> 12));
            _buffer[_position++] = (byte)(0x80 | ((scalar >> 6) & 0x3F));
            _buffer[_position++] = (byte)(0x80 | (scalar & 0x3F));
        }

        private void AppendEscapedAscii(byte value)
        {
            EnsureRoom(6);
            _buffer[_position++] = JsonConstants.BackSlash;
            switch (value)
            {
                case JsonConstants.Quote:
                    _buffer[_position++] = JsonConstants.Quote;
                    return;
                case JsonConstants.BackSlash:
                    _buffer[_position++] = JsonConstants.BackSlash;
                    return;
                case 0x08:
                    _buffer[_position++] = (byte)'b';
                    return;
                case 0x0C:
                    _buffer[_position++] = (byte)'f';
                    return;
                case 0x0A:
                    _buffer[_position++] = (byte)'n';
                    return;
                case 0x0D:
                    _buffer[_position++] = (byte)'r';
                    return;
                case 0x09:
                    _buffer[_position++] = (byte)'t';
                    return;
                default:
                    _buffer[_position++] = (byte)'u';
                    _buffer[_position++] = (byte)'0';
                    _buffer[_position++] = (byte)'0';
                    _buffer[_position++] = JsonEscaping.ToHexDigit(value >> 4);
                    _buffer[_position++] = JsonEscaping.ToHexDigit(value);
                    return;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void EnsureRoom(int needed)
        {
            if (_position + needed > _buffer.Length)
            {
                Grow(needed);
            }
        }

        private void Grow(int needed)
        {
            long target = (long)_position + needed;
            long size = _buffer.Length == 0 ? 256 : _buffer.Length;
            while (size < target)
            {
                size *= 2;
            }

            if (size > int.MaxValue)
            {
                throw new NdjsonException("Le tampon JSON depasse la taille maximale d'un tableau.");
            }

            byte[] bigger = ArrayPool<byte>.Shared.Rent((int)size);
            System.Buffer.BlockCopy(_buffer, 0, bigger, 0, _position);
            if (_rented)
            {
                ArrayPool<byte>.Shared.Return(_buffer);
            }

            _buffer = bigger;
            _rented = true;
        }

        private static void ThrowFormatFailure()
        {
            throw new NdjsonException("Echec du formatage d'une valeur numerique.");
        }
    }
}
