using System;
using System.Collections.Generic;
using System.Globalization;

namespace NdJson.Serialization.Converters
{
    internal sealed class NullableConverter<T> : NdjsonConverter<T?> where T : struct
    {
        private NdjsonConverter<T> _inner;

        public override void Write(ref JsonWriter writer, in T? value, NdjsonOptions options)
        {
            if (!value.HasValue)
            {
                writer.WriteNull();
                return;
            }

            T inner = value.Value;
            Inner(options).Write(ref writer, in inner, options);
        }

        public override T? Read(ref JsonReader reader, NdjsonOptions options)
        {
            if (reader.IsNull)
            {
                return null;
            }

            return Inner(options).Read(ref reader, options);
        }

        private NdjsonConverter<T> Inner(NdjsonOptions options)
        {
            NdjsonConverter<T> inner = _inner;
            if (inner == null)
            {
                inner = options.GetConverter<T>();
                _inner = inner;
            }

            return inner;
        }
    }

    internal sealed class ArrayConverter<TElement> : NdjsonConverter<TElement[]>
    {
        private NdjsonConverter<TElement> _element;

        public override void Write(ref JsonWriter writer, in TElement[] value, NdjsonOptions options)
        {
            if (value == null)
            {
                writer.WriteNull();
                return;
            }

            NdjsonConverter<TElement> element = Element(options);
            writer.WriteStartArray();
            for (int i = 0; i < value.Length; i++)
            {
                element.Write(ref writer, in value[i], options);
            }

            writer.WriteEndArray();
        }

        public override TElement[] Read(ref JsonReader reader, NdjsonOptions options)
        {
            if (!reader.BeginArray())
            {
                return null;
            }

            NdjsonConverter<TElement> element = Element(options);
            TElement[] buffer = new TElement[4];
            int count = 0;

            while (reader.ReadNextArrayElement())
            {
                if (count == buffer.Length)
                {
                    Array.Resize(ref buffer, buffer.Length * 2);
                }

                buffer[count++] = element.Read(ref reader, options);
            }

            if (count == buffer.Length)
            {
                return buffer;
            }

            TElement[] result = new TElement[count];
            Array.Copy(buffer, result, count);
            return result;
        }

        private NdjsonConverter<TElement> Element(NdjsonOptions options)
        {
            NdjsonConverter<TElement> element = _element;
            if (element == null)
            {
                element = options.GetConverter<TElement>();
                _element = element;
            }

            return element;
        }
    }

    internal sealed class ListConverter<TElement> : NdjsonConverter<List<TElement>>
    {
        private NdjsonConverter<TElement> _element;

        public override void Write(ref JsonWriter writer, in List<TElement> value, NdjsonOptions options)
        {
            if (value == null)
            {
                writer.WriteNull();
                return;
            }

            NdjsonConverter<TElement> element = Element(options);
            writer.WriteStartArray();
            int count = value.Count;
            for (int i = 0; i < count; i++)
            {
                TElement item = value[i];
                element.Write(ref writer, in item, options);
            }

            writer.WriteEndArray();
        }

        public override List<TElement> Read(ref JsonReader reader, NdjsonOptions options)
        {
            if (!reader.BeginArray())
            {
                return null;
            }

            NdjsonConverter<TElement> element = Element(options);
            List<TElement> result = new List<TElement>();

            while (reader.ReadNextArrayElement())
            {
                result.Add(element.Read(ref reader, options));
            }

            return result;
        }

        private NdjsonConverter<TElement> Element(NdjsonOptions options)
        {
            NdjsonConverter<TElement> element = _element;
            if (element == null)
            {
                element = options.GetConverter<TElement>();
                _element = element;
            }

            return element;
        }
    }

    internal sealed class CollectionConverter<TCollection, TElement> : NdjsonConverter<TCollection>
        where TCollection : class, ICollection<TElement>, new()
    {
        private NdjsonConverter<TElement> _element;

        public override void Write(ref JsonWriter writer, in TCollection value, NdjsonOptions options)
        {
            if (value == null)
            {
                writer.WriteNull();
                return;
            }

            NdjsonConverter<TElement> element = Element(options);
            writer.WriteStartArray();
            foreach (TElement item in value)
            {
                element.Write(ref writer, in item, options);
            }

            writer.WriteEndArray();
        }

        public override TCollection Read(ref JsonReader reader, NdjsonOptions options)
        {
            if (!reader.BeginArray())
            {
                return null;
            }

            NdjsonConverter<TElement> element = Element(options);
            TCollection result = new TCollection();

            while (reader.ReadNextArrayElement())
            {
                result.Add(element.Read(ref reader, options));
            }

            return result;
        }

        private NdjsonConverter<TElement> Element(NdjsonOptions options)
        {
            NdjsonConverter<TElement> element = _element;
            if (element == null)
            {
                element = options.GetConverter<TElement>();
                _element = element;
            }

            return element;
        }
    }

    internal sealed class EnumerableInterfaceConverter<TInterface, TElement> : NdjsonConverter<TInterface>
        where TInterface : class
    {
        private NdjsonConverter<TElement> _element;

        public override void Write(ref JsonWriter writer, in TInterface value, NdjsonOptions options)
        {
            if (value == null)
            {
                writer.WriteNull();
                return;
            }

            NdjsonConverter<TElement> element = Element(options);
            writer.WriteStartArray();
            foreach (TElement item in (IEnumerable<TElement>)value)
            {
                element.Write(ref writer, in item, options);
            }

            writer.WriteEndArray();
        }

        public override TInterface Read(ref JsonReader reader, NdjsonOptions options)
        {
            if (!reader.BeginArray())
            {
                return null;
            }

            NdjsonConverter<TElement> element = Element(options);
            List<TElement> result = new List<TElement>();

            while (reader.ReadNextArrayElement())
            {
                result.Add(element.Read(ref reader, options));
            }

            return (TInterface)(object)result;
        }

        private NdjsonConverter<TElement> Element(NdjsonOptions options)
        {
            NdjsonConverter<TElement> element = _element;
            if (element == null)
            {
                element = options.GetConverter<TElement>();
                _element = element;
            }

            return element;
        }
    }

    internal static class DictionaryKey
    {
        internal static string ToKeyString(object key)
        {
            IFormattable formattable = key as IFormattable;
            if (formattable != null)
            {
                return formattable.ToString(null, CultureInfo.InvariantCulture);
            }

            return key.ToString();
        }

        internal static object FromKeyString(string key, Type keyType)
        {
            if (keyType == typeof(string))
            {
                return key;
            }

            if (keyType.IsEnum)
            {
                return Enum.Parse(keyType, key, true);
            }

            if (keyType == typeof(Guid))
            {
                return Guid.Parse(key);
            }

            return System.Convert.ChangeType(key, keyType, CultureInfo.InvariantCulture);
        }
    }

    internal sealed class DictionaryConverter<TKey, TValue> : NdjsonConverter<Dictionary<TKey, TValue>>
    {
        private NdjsonConverter<TValue> _value;
        private readonly bool _stringKey = typeof(TKey) == typeof(string);

        public override void Write(ref JsonWriter writer, in Dictionary<TKey, TValue> value, NdjsonOptions options)
        {
            if (value == null)
            {
                writer.WriteNull();
                return;
            }

            NdjsonConverter<TValue> valueConverter = Value(options);
            writer.WriteStartObject();
            foreach (KeyValuePair<TKey, TValue> pair in value)
            {
                writer.WritePropertyName(_stringKey ? (string)(object)pair.Key : DictionaryKey.ToKeyString(pair.Key));
                TValue item = pair.Value;
                valueConverter.Write(ref writer, in item, options);
            }

            writer.WriteEndObject();
        }

        public override Dictionary<TKey, TValue> Read(ref JsonReader reader, NdjsonOptions options)
        {
            if (!reader.BeginObject())
            {
                return null;
            }

            NdjsonConverter<TValue> valueConverter = Value(options);
            Dictionary<TKey, TValue> result = new Dictionary<TKey, TValue>();

            while (reader.ReadNextProperty())
            {
                string name = reader.GetString();
                TKey key = _stringKey ? (TKey)(object)name : (TKey)DictionaryKey.FromKeyString(name, typeof(TKey));
                reader.Advance();
                result[key] = valueConverter.Read(ref reader, options);
            }

            return result;
        }

        private NdjsonConverter<TValue> Value(NdjsonOptions options)
        {
            NdjsonConverter<TValue> value = _value;
            if (value == null)
            {
                value = options.GetConverter<TValue>();
                _value = value;
            }

            return value;
        }
    }

    internal sealed class DictionaryInterfaceConverter<TInterface, TKey, TValue> : NdjsonConverter<TInterface>
        where TInterface : class
    {
        private NdjsonConverter<TValue> _value;
        private readonly bool _stringKey = typeof(TKey) == typeof(string);

        public override void Write(ref JsonWriter writer, in TInterface value, NdjsonOptions options)
        {
            if (value == null)
            {
                writer.WriteNull();
                return;
            }

            NdjsonConverter<TValue> valueConverter = Value(options);
            writer.WriteStartObject();
            foreach (KeyValuePair<TKey, TValue> pair in (IEnumerable<KeyValuePair<TKey, TValue>>)value)
            {
                writer.WritePropertyName(_stringKey ? (string)(object)pair.Key : DictionaryKey.ToKeyString(pair.Key));
                TValue item = pair.Value;
                valueConverter.Write(ref writer, in item, options);
            }

            writer.WriteEndObject();
        }

        public override TInterface Read(ref JsonReader reader, NdjsonOptions options)
        {
            if (!reader.BeginObject())
            {
                return null;
            }

            NdjsonConverter<TValue> valueConverter = Value(options);
            Dictionary<TKey, TValue> result = new Dictionary<TKey, TValue>();

            while (reader.ReadNextProperty())
            {
                string name = reader.GetString();
                TKey key = _stringKey ? (TKey)(object)name : (TKey)DictionaryKey.FromKeyString(name, typeof(TKey));
                reader.Advance();
                result[key] = valueConverter.Read(ref reader, options);
            }

            return (TInterface)(object)result;
        }

        private NdjsonConverter<TValue> Value(NdjsonOptions options)
        {
            NdjsonConverter<TValue> value = _value;
            if (value == null)
            {
                value = options.GetConverter<TValue>();
                _value = value;
            }

            return value;
        }
    }

    internal sealed class MutableDictionaryConverter<TDictionary, TKey, TValue> : NdjsonConverter<TDictionary>
        where TDictionary : class, IDictionary<TKey, TValue>, new()
    {
        private NdjsonConverter<TValue> _value;
        private readonly bool _stringKey = typeof(TKey) == typeof(string);

        public override void Write(ref JsonWriter writer, in TDictionary value, NdjsonOptions options)
        {
            if (value == null)
            {
                writer.WriteNull();
                return;
            }

            NdjsonConverter<TValue> valueConverter = Value(options);
            writer.WriteStartObject();
            foreach (KeyValuePair<TKey, TValue> pair in value)
            {
                writer.WritePropertyName(_stringKey ? (string)(object)pair.Key : DictionaryKey.ToKeyString(pair.Key));
                TValue item = pair.Value;
                valueConverter.Write(ref writer, in item, options);
            }

            writer.WriteEndObject();
        }

        public override TDictionary Read(ref JsonReader reader, NdjsonOptions options)
        {
            if (!reader.BeginObject())
            {
                return null;
            }

            NdjsonConverter<TValue> valueConverter = Value(options);
            TDictionary result = new TDictionary();

            while (reader.ReadNextProperty())
            {
                string name = reader.GetString();
                TKey key = _stringKey ? (TKey)(object)name : (TKey)DictionaryKey.FromKeyString(name, typeof(TKey));
                reader.Advance();
                result[key] = valueConverter.Read(ref reader, options);
            }

            return result;
        }

        private NdjsonConverter<TValue> Value(NdjsonOptions options)
        {
            NdjsonConverter<TValue> value = _value;
            if (value == null)
            {
                value = options.GetConverter<TValue>();
                _value = value;
            }

            return value;
        }
    }

    internal sealed class EnumerableConstructorConverter<TCollection, TElement> : NdjsonConverter<TCollection>
        where TCollection : class, IEnumerable<TElement>
    {
        private NdjsonConverter<TElement> _element;
        private readonly System.Reflection.ConstructorInfo _constructor = ConverterResolver.FindEnumerableConstructor(typeof(TCollection), typeof(TElement));

        public override void Write(ref JsonWriter writer, in TCollection value, NdjsonOptions options)
        {
            if (value == null)
            {
                writer.WriteNull();
                return;
            }

            NdjsonConverter<TElement> element = Element(options);
            writer.WriteStartArray();
            foreach (TElement item in value)
            {
                element.Write(ref writer, in item, options);
            }

            writer.WriteEndArray();
        }

        public override TCollection Read(ref JsonReader reader, NdjsonOptions options)
        {
            if (!reader.BeginArray())
            {
                return null;
            }

            NdjsonConverter<TElement> element = Element(options);
            List<TElement> items = new List<TElement>();

            while (reader.ReadNextArrayElement())
            {
                items.Add(element.Read(ref reader, options));
            }

            return (TCollection)_constructor.Invoke(new object[] { items });
        }

        private NdjsonConverter<TElement> Element(NdjsonOptions options)
        {
            NdjsonConverter<TElement> element = _element;
            if (element == null)
            {
                element = options.GetConverter<TElement>();
                _element = element;
            }

            return element;
        }
    }

    internal sealed class ReadOnlyEnumerableConverter<TCollection, TElement> : NdjsonConverter<TCollection>
        where TCollection : class, IEnumerable<TElement>
    {
        private NdjsonConverter<TElement> _element;

        public override void Write(ref JsonWriter writer, in TCollection value, NdjsonOptions options)
        {
            if (value == null)
            {
                writer.WriteNull();
                return;
            }

            NdjsonConverter<TElement> element = Element(options);
            writer.WriteStartArray();
            foreach (TElement item in value)
            {
                element.Write(ref writer, in item, options);
            }

            writer.WriteEndArray();
        }

        public override TCollection Read(ref JsonReader reader, NdjsonOptions options)
        {
            throw new NdjsonException("Le type " + typeof(TCollection).FullName + " ne peut pas etre deserialise : aucun constructeur sans parametre ni constructeur acceptant IEnumerable<" + typeof(TElement).Name + ">.");
        }

        private NdjsonConverter<TElement> Element(NdjsonOptions options)
        {
            NdjsonConverter<TElement> element = _element;
            if (element == null)
            {
                element = options.GetConverter<TElement>();
                _element = element;
            }

            return element;
        }
    }

    internal sealed class NullableWrappingConverter<T> : NdjsonConverter<T?> where T : struct
    {
        private readonly NdjsonConverter<T> _inner;

        public NullableWrappingConverter(NdjsonConverter inner)
        {
            _inner = (NdjsonConverter<T>)inner;
        }

        public override void Write(ref JsonWriter writer, in T? value, NdjsonOptions options)
        {
            if (!value.HasValue)
            {
                writer.WriteNull();
                return;
            }

            T inner = value.Value;
            _inner.Write(ref writer, in inner, options);
        }

        public override T? Read(ref JsonReader reader, NdjsonOptions options)
        {
            if (reader.IsNull)
            {
                return null;
            }

            return _inner.Read(ref reader, options);
        }
    }
}
