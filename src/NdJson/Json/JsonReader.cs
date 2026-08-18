using System;
using System.Runtime.CompilerServices;

namespace NdJson
{
    public ref struct JsonReader
    {
        private readonly ReadOnlySpan<byte> _buffer;
        private readonly int _maxDepth;
        private int _position;
        private int _valueStart;
        private int _valueLength;
        private int _tokenStart;
        private int _depth;
        private ulong _containerStack;
        private JsonTokenType _tokenType;
        private bool _valueHasEscapes;
        private bool _completed;
        private byte[] _scratch;

        public JsonReader(ReadOnlySpan<byte> utf8Json)
            : this(utf8Json, 64)
        {
        }

        public JsonReader(ReadOnlySpan<byte> utf8Json, int maxDepth)
        {
            _buffer = utf8Json;
            _maxDepth = maxDepth < 1 ? 1 : (maxDepth > 64 ? 64 : maxDepth);
            _position = 0;
            _valueStart = 0;
            _valueLength = 0;
            _tokenStart = 0;
            _depth = 0;
            _containerStack = 0;
            _tokenType = JsonTokenType.None;
            _valueHasEscapes = false;
            _completed = false;
            _scratch = null;
        }

        public JsonTokenType TokenType
        {
            get { return _tokenType; }
        }

        public int Depth
        {
            get { return _depth; }
        }

        public int Position
        {
            get { return _position; }
        }

        public int TokenStartPosition
        {
            get { return _tokenStart; }
        }

        public ReadOnlySpan<byte> RawBuffer
        {
            get { return _buffer; }
        }

        public bool ValueHasEscapes
        {
            get { return _valueHasEscapes; }
        }

        public ReadOnlySpan<byte> ValueSpan
        {
            get { return _buffer.Slice(_valueStart, _valueLength); }
        }

        public bool Read()
        {
            if (_completed)
            {
                _tokenType = JsonTokenType.None;
                return false;
            }

            SkipWhitespace();

            if (_position >= _buffer.Length)
            {
                if (_depth != 0)
                {
                    ThrowUnexpectedEnd();
                }

                _tokenType = JsonTokenType.None;
                _completed = true;
                return false;
            }

            byte current = _buffer[_position];

            switch (_tokenType)
            {
                case JsonTokenType.None:
                    return ReadValueToken(current);

                case JsonTokenType.PropertyName:
                    return ReadValueToken(current);

                case JsonTokenType.StartObject:
                    if (current == JsonConstants.CloseBrace)
                    {
                        return EndContainer(true);
                    }

                    return ReadPropertyNameToken(current);

                case JsonTokenType.StartArray:
                    if (current == JsonConstants.CloseBracket)
                    {
                        return EndContainer(false);
                    }

                    return ReadValueToken(current);

                default:
                    break;
            }

            if (_depth == 0)
            {
                ThrowUnexpectedCharacter(current);
            }

            if (current == JsonConstants.Comma)
            {
                _position++;
                SkipWhitespace();
                if (_position >= _buffer.Length)
                {
                    ThrowUnexpectedEnd();
                }

                current = _buffer[_position];
                if (IsInObject())
                {
                    return ReadPropertyNameToken(current);
                }

                return ReadValueToken(current);
            }

            if (current == JsonConstants.CloseBrace)
            {
                if (!IsInObject())
                {
                    ThrowUnexpectedCharacter(current);
                }

                return EndContainer(true);
            }

            if (current == JsonConstants.CloseBracket)
            {
                if (IsInObject())
                {
                    ThrowUnexpectedCharacter(current);
                }

                return EndContainer(false);
            }

            ThrowUnexpectedCharacter(current);
            return false;
        }

        public void SkipValue()
        {
            if (!Read())
            {
                ThrowUnexpectedEnd();
            }

            SkipChildren();
        }

        public void SkipChildren()
        {
            if (_tokenType != JsonTokenType.StartObject && _tokenType != JsonTokenType.StartArray)
            {
                return;
            }

            int target = _depth - 1;
            while (Read())
            {
                if ((_tokenType == JsonTokenType.EndObject || _tokenType == JsonTokenType.EndArray) && _depth == target)
                {
                    return;
                }
            }

            ThrowUnexpectedEnd();
        }

        public void Advance()
        {
            if (!Read())
            {
                ThrowUnexpectedEnd();
            }
        }

        public bool BeginObject()
        {
            if (_tokenType == JsonTokenType.Null)
            {
                return false;
            }

            if (_tokenType != JsonTokenType.StartObject)
            {
                ThrowExpected("un objet");
            }

            return true;
        }

        public bool BeginArray()
        {
            if (_tokenType == JsonTokenType.Null)
            {
                return false;
            }

            if (_tokenType != JsonTokenType.StartArray)
            {
                ThrowExpected("un tableau");
            }

            return true;
        }

        public bool IsNull
        {
            get { return _tokenType == JsonTokenType.Null; }
        }

        public bool ReadNextProperty()
        {
            if (!Read())
            {
                ThrowUnexpectedEnd();
            }

            if (_tokenType == JsonTokenType.EndObject)
            {
                return false;
            }

            if (_tokenType != JsonTokenType.PropertyName)
            {
                ThrowExpected("un nom de propriete");
            }

            return true;
        }

        public bool ReadNextArrayElement()
        {
            if (!Read())
            {
                ThrowUnexpectedEnd();
            }

            return _tokenType != JsonTokenType.EndArray;
        }

        public bool PropertyEquals(ReadOnlySpan<byte> utf8Name)
        {
            if (!_valueHasEscapes)
            {
                return ValueSpanEquals(ValueSpan, utf8Name);
            }

            return ValueSpanEquals(UnescapeToScratch(), utf8Name);
        }

        public bool PropertyEqualsIgnoreCase(ReadOnlySpan<byte> utf8Name)
        {
            ReadOnlySpan<byte> value = _valueHasEscapes ? UnescapeToScratch() : ValueSpan;
            if (value.Length != utf8Name.Length)
            {
                return false;
            }

            for (int i = 0; i < value.Length; i++)
            {
                byte a = value[i];
                byte b = utf8Name[i];
                if (a == b)
                {
                    continue;
                }

                if (a >= 'A' && a <= 'Z')
                {
                    a = (byte)(a + 32);
                }

                if (b >= 'A' && b <= 'Z')
                {
                    b = (byte)(b + 32);
                }

                if (a != b)
                {
                    return false;
                }
            }

            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool ValueSpanEquals(ReadOnlySpan<byte> left, ReadOnlySpan<byte> right)
        {
            return left.SequenceEqual(right);
        }

        public string GetString()
        {
            if (_tokenType == JsonTokenType.Null)
            {
                return null;
            }

            if (_tokenType != JsonTokenType.String && _tokenType != JsonTokenType.PropertyName)
            {
                ThrowExpected("une chaine");
            }

            if (!_valueHasEscapes)
            {
                return JsonEscaping.GetString(ValueSpan);
            }

            return JsonEscaping.GetString(UnescapeToScratch());
        }

        public ReadOnlySpan<byte> PropertyNameSpan
        {
            get { return _valueHasEscapes ? UnescapeToScratch() : ValueSpan; }
        }

        public ReadOnlySpan<byte> GetUnescapedSpan()
        {
            if (!_valueHasEscapes)
            {
                return ValueSpan;
            }

            return UnescapeToScratch();
        }

        public bool GetBoolean()
        {
            if (_tokenType == JsonTokenType.True)
            {
                return true;
            }

            if (_tokenType == JsonTokenType.False)
            {
                return false;
            }

            ThrowExpected("un booleen");
            return false;
        }

        public long GetInt64()
        {
            EnsureNumber();
            long value;
            if (JsonNumber.TryParseInt64(ValueSpan, out value))
            {
                return value;
            }

            double asDouble;
            if (JsonNumber.TryParseDouble(ValueSpan, out asDouble) && asDouble >= -9223372036854775808.0 && asDouble <= 9223372036854775807.0)
            {
                return (long)asDouble;
            }

            ThrowInvalidNumber("Int64");
            return 0;
        }

        public ulong GetUInt64()
        {
            EnsureNumber();
            ulong value;
            if (JsonNumber.TryParseUInt64(ValueSpan, out value))
            {
                return value;
            }

            ThrowInvalidNumber("UInt64");
            return 0;
        }

        public int GetInt32()
        {
            long value = GetInt64();
            if (value < int.MinValue || value > int.MaxValue)
            {
                ThrowInvalidNumber("Int32");
            }

            return (int)value;
        }

        public uint GetUInt32()
        {
            ulong value = GetUInt64();
            if (value > uint.MaxValue)
            {
                ThrowInvalidNumber("UInt32");
            }

            return (uint)value;
        }

        public short GetInt16()
        {
            long value = GetInt64();
            if (value < short.MinValue || value > short.MaxValue)
            {
                ThrowInvalidNumber("Int16");
            }

            return (short)value;
        }

        public ushort GetUInt16()
        {
            ulong value = GetUInt64();
            if (value > ushort.MaxValue)
            {
                ThrowInvalidNumber("UInt16");
            }

            return (ushort)value;
        }

        public sbyte GetSByte()
        {
            long value = GetInt64();
            if (value < sbyte.MinValue || value > sbyte.MaxValue)
            {
                ThrowInvalidNumber("SByte");
            }

            return (sbyte)value;
        }

        public byte GetByte()
        {
            ulong value = GetUInt64();
            if (value > byte.MaxValue)
            {
                ThrowInvalidNumber("Byte");
            }

            return (byte)value;
        }

        public double GetDouble()
        {
            if (_tokenType == JsonTokenType.String)
            {
                return GetNonFiniteFromString();
            }

            EnsureNumber();
            double value;
            if (JsonNumber.TryParseDouble(ValueSpan, out value))
            {
                return value;
            }

            ThrowInvalidNumber("Double");
            return 0;
        }

        public float GetSingle()
        {
            return (float)GetDouble();
        }

        public decimal GetDecimal()
        {
            EnsureNumber();
            decimal value;
            if (JsonNumber.TryParseDecimal(ValueSpan, out value))
            {
                return value;
            }

            ThrowInvalidNumber("Decimal");
            return 0;
        }

        public DateTime GetDateTime()
        {
            if (_tokenType == JsonTokenType.Number)
            {
                return JsonDateTime.FromUnixMilliseconds(GetDouble());
            }

            EnsureString();
            DateTime value;
            if (JsonDateTime.TryParseDateTime(GetUnescapedSpan(), out value))
            {
                return value;
            }

            ThrowInvalidNumber("DateTime");
            return default(DateTime);
        }

        public DateTimeOffset GetDateTimeOffset()
        {
            EnsureString();
            DateTimeOffset value;
            if (JsonDateTime.TryParseDateTimeOffset(GetUnescapedSpan(), out value))
            {
                return value;
            }

            ThrowInvalidNumber("DateTimeOffset");
            return default(DateTimeOffset);
        }

        public Guid GetGuid()
        {
            EnsureString();
            Guid value;
            if (JsonGuidHelper.TryParse(GetUnescapedSpan(), out value))
            {
                return value;
            }

            ThrowInvalidNumber("Guid");
            return default(Guid);
        }

        public TimeSpan GetTimeSpan()
        {
            if (_tokenType == JsonTokenType.Number)
            {
                return TimeSpan.FromTicks((long)GetDouble());
            }

            EnsureString();
            TimeSpan value;
            if (TimeSpan.TryParse(GetString(), System.Globalization.CultureInfo.InvariantCulture, out value))
            {
                return value;
            }

            ThrowInvalidNumber("TimeSpan");
            return default(TimeSpan);
        }

        public char GetChar()
        {
            string text = GetString();
            if (text == null || text.Length != 1)
            {
                ThrowExpected("une chaine d'un seul caractere");
            }

            return text[0];
        }

        private double GetNonFiniteFromString()
        {
            ReadOnlySpan<byte> span = GetUnescapedSpan();
            if (span.Length == 3 && span[0] == 'N' && span[1] == 'a' && span[2] == 'N')
            {
                return double.NaN;
            }

            if (span.Length == 8 && span[0] == 'I')
            {
                return double.PositiveInfinity;
            }

            if (span.Length == 9 && span[0] == '-' && span[1] == 'I')
            {
                return double.NegativeInfinity;
            }

            double parsed;
            if (JsonNumber.TryParseDouble(span, out parsed))
            {
                return parsed;
            }

            ThrowInvalidNumber("Double");
            return 0;
        }

        private ReadOnlySpan<byte> UnescapeToScratch()
        {
            ReadOnlySpan<byte> raw = ValueSpan;
            if (_scratch == null || _scratch.Length < raw.Length)
            {
                _scratch = new byte[raw.Length < 64 ? 64 : raw.Length];
            }

            int written = JsonEscaping.Unescape(raw, _scratch);
            return new ReadOnlySpan<byte>(_scratch, 0, written);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void SkipWhitespace()
        {
            int position = _position;
            ReadOnlySpan<byte> buffer = _buffer;
            while (position < buffer.Length)
            {
                byte current = buffer[position];
                if (current != JsonConstants.Space && current != JsonConstants.Tab && current != JsonConstants.CarriageReturn && current != JsonConstants.LineFeed)
                {
                    break;
                }

                position++;
            }

            _position = position;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool IsInObject()
        {
            return (_containerStack & (1UL << (_depth - 1))) != 0;
        }

        private bool EndContainer(bool isObject)
        {
            _tokenStart = _position;
            _position++;
            _depth--;
            _tokenType = isObject ? JsonTokenType.EndObject : JsonTokenType.EndArray;
            _valueStart = _position;
            _valueLength = 0;
            _valueHasEscapes = false;

            if (_depth == 0)
            {
                CompleteRoot();
            }

            return true;
        }

        private bool ReadPropertyNameToken(byte current)
        {
            _tokenStart = _position;
            if (current != JsonConstants.Quote)
            {
                ThrowUnexpectedCharacter(current);
            }

            ScanString();
            _tokenType = JsonTokenType.PropertyName;

            SkipWhitespace();
            if (_position >= _buffer.Length || _buffer[_position] != JsonConstants.Colon)
            {
                ThrowExpected("':'");
            }

            _position++;
            return true;
        }

        private bool ReadValueToken(byte current)
        {
            _tokenStart = _position;
            switch (current)
            {
                case JsonConstants.Quote:
                    ScanString();
                    _tokenType = JsonTokenType.String;
                    if (_depth == 0)
                    {
                        CompleteRoot();
                    }

                    return true;

                case JsonConstants.OpenBrace:
                    PushContainer(true);
                    _tokenType = JsonTokenType.StartObject;
                    return true;

                case JsonConstants.OpenBracket:
                    PushContainer(false);
                    _tokenType = JsonTokenType.StartArray;
                    return true;

                case (byte)'t':
                    ExpectLiteral(JsonConstants.TrueLiteral);
                    _tokenType = JsonTokenType.True;
                    if (_depth == 0)
                    {
                        CompleteRoot();
                    }

                    return true;

                case (byte)'f':
                    ExpectLiteral(JsonConstants.FalseLiteral);
                    _tokenType = JsonTokenType.False;
                    if (_depth == 0)
                    {
                        CompleteRoot();
                    }

                    return true;

                case (byte)'n':
                    ExpectLiteral(JsonConstants.NullLiteral);
                    _tokenType = JsonTokenType.Null;
                    if (_depth == 0)
                    {
                        CompleteRoot();
                    }

                    return true;

                default:
                    if (current == JsonConstants.Minus || (current >= JsonConstants.Zero && current <= JsonConstants.Nine))
                    {
                        ScanNumber();
                        _tokenType = JsonTokenType.Number;
                        if (_depth == 0)
                        {
                            CompleteRoot();
                        }

                        return true;
                    }

                    ThrowUnexpectedCharacter(current);
                    return false;
            }
        }

        private void PushContainer(bool isObject)
        {
            if (_depth >= _maxDepth)
            {
                throw new NdjsonException("Profondeur JSON maximale depassee (" + _maxDepth.ToString(System.Globalization.CultureInfo.InvariantCulture) + ").");
            }

            if (isObject)
            {
                _containerStack |= 1UL << _depth;
            }
            else
            {
                _containerStack &= ~(1UL << _depth);
            }

            _depth++;
            _position++;
            _valueStart = _position;
            _valueLength = 0;
            _valueHasEscapes = false;
        }

        private void CompleteRoot()
        {
            _completed = true;
            int position = _position;
            ReadOnlySpan<byte> buffer = _buffer;
            while (position < buffer.Length)
            {
                byte current = buffer[position];
                if (current != JsonConstants.Space && current != JsonConstants.Tab && current != JsonConstants.CarriageReturn && current != JsonConstants.LineFeed)
                {
                    throw new NdjsonException("Contenu inattendu apres la valeur JSON racine a l'octet " + position.ToString(System.Globalization.CultureInfo.InvariantCulture) + ".");
                }

                position++;
            }
        }

        private void ScanString()
        {
            int start = _position + 1;
            int index = start;
            bool hasEscapes = false;
            ReadOnlySpan<byte> buffer = _buffer;

            while (true)
            {
                if (index >= buffer.Length)
                {
                    ThrowUnexpectedEnd();
                }

                int found = buffer.Slice(index).IndexOfAny(JsonConstants.Quote, JsonConstants.BackSlash);
                if (found < 0)
                {
                    ThrowUnexpectedEnd();
                }

                index += found;
                if (buffer[index] == JsonConstants.Quote)
                {
                    break;
                }

                hasEscapes = true;
                index += 2;
            }

            _valueStart = start;
            _valueLength = index - start;
            _valueHasEscapes = hasEscapes;
            _position = index + 1;
        }

        private void ScanNumber()
        {
            int start = _position;
            int index = start;
            ReadOnlySpan<byte> buffer = _buffer;
            bool[] table = JsonConstants.IsNumberPart;

            while (index < buffer.Length && table[buffer[index]])
            {
                index++;
            }

            _valueStart = start;
            _valueLength = index - start;
            _valueHasEscapes = false;
            _position = index;
        }

        private void ExpectLiteral(byte[] literal)
        {
            if (_position + literal.Length > _buffer.Length)
            {
                ThrowUnexpectedEnd();
            }

            for (int i = 1; i < literal.Length; i++)
            {
                if (_buffer[_position + i] != literal[i])
                {
                    ThrowUnexpectedCharacter(_buffer[_position + i]);
                }
            }

            _valueStart = _position;
            _valueLength = literal.Length;
            _valueHasEscapes = false;
            _position += literal.Length;
        }

        private void EnsureNumber()
        {
            if (_tokenType != JsonTokenType.Number)
            {
                ThrowExpected("un nombre");
            }
        }

        private void EnsureString()
        {
            if (_tokenType != JsonTokenType.String)
            {
                ThrowExpected("une chaine");
            }
        }

        private void ThrowExpected(string expected)
        {
            throw new NdjsonException("JSON invalide : attendu " + expected + " mais trouve " + _tokenType + " a l'octet " + _position.ToString(System.Globalization.CultureInfo.InvariantCulture) + ".");
        }

        private void ThrowInvalidNumber(string target)
        {
            throw new NdjsonException("Impossible de convertir la valeur JSON '" + JsonEscaping.GetString(ValueSpan) + "' en " + target + ".");
        }

        private void ThrowUnexpectedCharacter(byte value)
        {
            throw new NdjsonException("Caractere inattendu '" + (char)value + "' a l'octet " + _position.ToString(System.Globalization.CultureInfo.InvariantCulture) + ".");
        }

        private void ThrowUnexpectedEnd()
        {
            throw new NdjsonException("Fin de donnees JSON inattendue a l'octet " + _position.ToString(System.Globalization.CultureInfo.InvariantCulture) + ".");
        }
    }
}
