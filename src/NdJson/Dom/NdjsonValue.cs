using System;
using System.Collections.Generic;
using System.Globalization;

namespace NdJson
{
    public enum NdjsonValueKind : byte
    {
        Null = 0,
        Object = 1,
        Array = 2,
        String = 3,
        Number = 4,
        Boolean = 5
    }

    public sealed class NdjsonValue
    {
        public static readonly NdjsonValue Null = new NdjsonValue();

        private readonly NdjsonValueKind _kind;
        private readonly bool _boolean;
        private readonly bool _isInteger;
        private readonly long _integer;
        private readonly double _number;
        private readonly string _text;
        private readonly List<NdjsonValue> _array;
        private readonly Dictionary<string, NdjsonValue> _object;

        private NdjsonValue()
        {
            _kind = NdjsonValueKind.Null;
        }

        private NdjsonValue(bool value)
        {
            _kind = NdjsonValueKind.Boolean;
            _boolean = value;
        }

        private NdjsonValue(long value)
        {
            _kind = NdjsonValueKind.Number;
            _integer = value;
            _number = value;
            _isInteger = true;
        }

        private NdjsonValue(double value)
        {
            _kind = NdjsonValueKind.Number;
            _number = value;
        }

        private NdjsonValue(string value)
        {
            if (value == null)
            {
                _kind = NdjsonValueKind.Null;
                return;
            }

            _kind = NdjsonValueKind.String;
            _text = value;
        }

        private NdjsonValue(List<NdjsonValue> value)
        {
            _kind = NdjsonValueKind.Array;
            _array = value;
        }

        private NdjsonValue(Dictionary<string, NdjsonValue> value)
        {
            _kind = NdjsonValueKind.Object;
            _object = value;
        }

        public NdjsonValueKind Kind
        {
            get { return _kind; }
        }

        public bool IsNull
        {
            get { return _kind == NdjsonValueKind.Null; }
        }

        public bool IsInteger
        {
            get { return _isInteger; }
        }

        public static NdjsonValue FromBoolean(bool value)
        {
            return new NdjsonValue(value);
        }

        public static NdjsonValue FromInt64(long value)
        {
            return new NdjsonValue(value);
        }

        public static NdjsonValue FromDouble(double value)
        {
            return new NdjsonValue(value);
        }

        public static NdjsonValue FromString(string value)
        {
            return value == null ? Null : new NdjsonValue(value);
        }

        public static NdjsonValue FromArray(List<NdjsonValue> value)
        {
            return value == null ? Null : new NdjsonValue(value);
        }

        public static NdjsonValue FromObject(Dictionary<string, NdjsonValue> value)
        {
            return value == null ? Null : new NdjsonValue(value);
        }

        public static NdjsonValue NewObject()
        {
            return new NdjsonValue(new Dictionary<string, NdjsonValue>(StringComparer.Ordinal));
        }

        public static NdjsonValue NewArray()
        {
            return new NdjsonValue(new List<NdjsonValue>());
        }

        public static implicit operator NdjsonValue(string value)
        {
            return FromString(value);
        }

        public static implicit operator NdjsonValue(long value)
        {
            return FromInt64(value);
        }

        public static implicit operator NdjsonValue(int value)
        {
            return FromInt64(value);
        }

        public static implicit operator NdjsonValue(double value)
        {
            return FromDouble(value);
        }

        public static implicit operator NdjsonValue(bool value)
        {
            return FromBoolean(value);
        }

        public List<NdjsonValue> AsArray
        {
            get
            {
                if (_kind != NdjsonValueKind.Array)
                {
                    ThrowKind("un tableau");
                }

                return _array;
            }
        }

        public Dictionary<string, NdjsonValue> AsObject
        {
            get
            {
                if (_kind != NdjsonValueKind.Object)
                {
                    ThrowKind("un objet");
                }

                return _object;
            }
        }

        public int Count
        {
            get
            {
                if (_kind == NdjsonValueKind.Array)
                {
                    return _array.Count;
                }

                if (_kind == NdjsonValueKind.Object)
                {
                    return _object.Count;
                }

                return 0;
            }
        }

        public NdjsonValue this[int index]
        {
            get
            {
                if (_kind != NdjsonValueKind.Array || index < 0 || index >= _array.Count)
                {
                    return Null;
                }

                return _array[index];
            }
            set
            {
                AsArray[index] = value ?? Null;
            }
        }

        public NdjsonValue this[string name]
        {
            get
            {
                NdjsonValue result;
                if (_kind != NdjsonValueKind.Object || !_object.TryGetValue(name, out result))
                {
                    return Null;
                }

                return result;
            }
            set
            {
                AsObject[name] = value ?? Null;
            }
        }

        public bool ContainsKey(string name)
        {
            return _kind == NdjsonValueKind.Object && _object.ContainsKey(name);
        }

        public bool TryGetValue(string name, out NdjsonValue value)
        {
            if (_kind == NdjsonValueKind.Object)
            {
                return _object.TryGetValue(name, out value);
            }

            value = Null;
            return false;
        }

        public void Add(NdjsonValue value)
        {
            AsArray.Add(value ?? Null);
        }

        public string GetString()
        {
            switch (_kind)
            {
                case NdjsonValueKind.String:
                    return _text;
                case NdjsonValueKind.Null:
                    return null;
                case NdjsonValueKind.Boolean:
                    return _boolean ? "true" : "false";
                case NdjsonValueKind.Number:
                    return _isInteger ? _integer.ToString(CultureInfo.InvariantCulture) : _number.ToString("R", CultureInfo.InvariantCulture);
                default:
                    return ToJsonString();
            }
        }

        public bool GetBoolean()
        {
            if (_kind == NdjsonValueKind.Boolean)
            {
                return _boolean;
            }

            if (_kind == NdjsonValueKind.Number)
            {
                return _isInteger ? _integer != 0 : _number != 0;
            }

            if (_kind == NdjsonValueKind.String)
            {
                return string.Equals(_text, "true", StringComparison.OrdinalIgnoreCase);
            }

            return false;
        }

        public long GetInt64()
        {
            if (_kind == NdjsonValueKind.Number)
            {
                return _isInteger ? _integer : (long)_number;
            }

            if (_kind == NdjsonValueKind.String)
            {
                long parsed;
                if (long.TryParse(_text, NumberStyles.Integer, CultureInfo.InvariantCulture, out parsed))
                {
                    return parsed;
                }
            }

            if (_kind == NdjsonValueKind.Boolean)
            {
                return _boolean ? 1 : 0;
            }

            ThrowKind("un nombre");
            return 0;
        }

        public int GetInt32()
        {
            return (int)GetInt64();
        }

        public double GetDouble()
        {
            if (_kind == NdjsonValueKind.Number)
            {
                return _isInteger ? _integer : _number;
            }

            if (_kind == NdjsonValueKind.String)
            {
                double parsed;
                if (double.TryParse(_text, NumberStyles.Float, CultureInfo.InvariantCulture, out parsed))
                {
                    return parsed;
                }
            }

            if (_kind == NdjsonValueKind.Boolean)
            {
                return _boolean ? 1 : 0;
            }

            ThrowKind("un nombre");
            return 0;
        }

        public float GetSingle()
        {
            return (float)GetDouble();
        }

        public static NdjsonValue Parse(string json)
        {
            if (json == null)
            {
                throw new ArgumentNullException(nameof(json));
            }

            return Parse(JsonEscaping.Encode(json));
        }

        public static NdjsonValue Parse(ReadOnlySpan<byte> utf8Json)
        {
            JsonReader reader = new JsonReader(utf8Json);
            return ReadValue(ref reader);
        }

        public static NdjsonValue ReadValue(ref JsonReader reader)
        {
            if (!reader.Read())
            {
                throw new NdjsonException("Valeur JSON attendue mais fin de donnees atteinte.");
            }

            return ReadCurrent(ref reader);
        }

        public static NdjsonValue ReadCurrent(ref JsonReader reader)
        {
            switch (reader.TokenType)
            {
                case JsonTokenType.Null:
                    return Null;
                case JsonTokenType.True:
                    return new NdjsonValue(true);
                case JsonTokenType.False:
                    return new NdjsonValue(false);
                case JsonTokenType.String:
                    return new NdjsonValue(reader.GetString());
                case JsonTokenType.Number:
                    {
                        long integer;
                        if (JsonNumber.TryParseInt64(reader.ValueSpan, out integer))
                        {
                            return new NdjsonValue(integer);
                        }

                        return new NdjsonValue(reader.GetDouble());
                    }
                case JsonTokenType.StartArray:
                    {
                        List<NdjsonValue> items = new List<NdjsonValue>();
                        while (reader.ReadNextArrayElement())
                        {
                            items.Add(ReadCurrent(ref reader));
                        }

                        return new NdjsonValue(items);
                    }
                case JsonTokenType.StartObject:
                    {
                        Dictionary<string, NdjsonValue> members = new Dictionary<string, NdjsonValue>(StringComparer.Ordinal);
                        while (reader.ReadNextProperty())
                        {
                            string name = reader.GetString();
                            members[name] = ReadValue(ref reader);
                        }

                        return new NdjsonValue(members);
                    }
                default:
                    throw new NdjsonException("Jeton JSON inattendu : " + reader.TokenType + ".");
            }
        }

        public void WriteTo(ref JsonWriter writer, NdjsonOptions options)
        {
            switch (_kind)
            {
                case NdjsonValueKind.Null:
                    writer.WriteNull();
                    return;
                case NdjsonValueKind.Boolean:
                    writer.WriteBoolean(_boolean);
                    return;
                case NdjsonValueKind.String:
                    writer.WriteString(_text);
                    return;
                case NdjsonValueKind.Number:
                    if (_isInteger)
                    {
                        writer.WriteNumber(_integer);
                    }
                    else
                    {
                        writer.WriteNumber(_number, options == null ? NdjsonNonFiniteHandling.WriteNull : options.NonFiniteHandling);
                    }

                    return;
                case NdjsonValueKind.Array:
                    writer.WriteStartArray();
                    for (int i = 0; i < _array.Count; i++)
                    {
                        NdjsonValue item = _array[i];
                        if (item == null)
                        {
                            writer.WriteNull();
                        }
                        else
                        {
                            item.WriteTo(ref writer, options);
                        }
                    }

                    writer.WriteEndArray();
                    return;
                default:
                    writer.WriteStartObject();
                    foreach (KeyValuePair<string, NdjsonValue> pair in _object)
                    {
                        writer.WritePropertyName(pair.Key);
                        if (pair.Value == null)
                        {
                            writer.WriteNull();
                        }
                        else
                        {
                            pair.Value.WriteTo(ref writer, options);
                        }
                    }

                    writer.WriteEndObject();
                    return;
            }
        }

        public string ToJsonString()
        {
            JsonWriter writer = JsonWriter.Create(256);
            try
            {
                WriteTo(ref writer, NdjsonOptions.Default);
                return JsonEscaping.GetString(writer.WrittenSpan);
            }
            finally
            {
                writer.Release();
            }
        }

        public override string ToString()
        {
            return ToJsonString();
        }

        public object ToClrObject()
        {
            switch (_kind)
            {
                case NdjsonValueKind.Null:
                    return null;
                case NdjsonValueKind.Boolean:
                    return _boolean;
                case NdjsonValueKind.String:
                    return _text;
                case NdjsonValueKind.Number:
                    return _isInteger ? (object)_integer : _number;
                case NdjsonValueKind.Array:
                    {
                        List<object> items = new List<object>(_array.Count);
                        for (int i = 0; i < _array.Count; i++)
                        {
                            items.Add(_array[i] == null ? null : _array[i].ToClrObject());
                        }

                        return items;
                    }
                default:
                    {
                        Dictionary<string, object> members = new Dictionary<string, object>(_object.Count, StringComparer.Ordinal);
                        foreach (KeyValuePair<string, NdjsonValue> pair in _object)
                        {
                            members[pair.Key] = pair.Value == null ? null : pair.Value.ToClrObject();
                        }

                        return members;
                    }
            }
        }

        private void ThrowKind(string expected)
        {
            throw new NdjsonException("La valeur JSON est de type " + _kind + " alors que " + expected + " etait attendu.");
        }
    }
}
