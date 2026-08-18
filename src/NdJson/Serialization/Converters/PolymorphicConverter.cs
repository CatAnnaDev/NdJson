using System;
using System.Collections.Generic;
using System.Reflection;
using NdJson.Reflection;

namespace NdJson.Serialization.Converters
{
    internal sealed class PolymorphicConverter<TBase> : NdjsonConverter<TBase> where TBase : class
    {
        private readonly byte[] _discriminatorWithColon;
        private readonly byte[] _discriminatorName;
        private readonly bool _ignoreUnrecognized;
        private readonly Dictionary<string, Type> _typeByTag;
        private readonly Dictionary<Type, byte[]> _tagByType;
        private NdjsonConverter<TBase> _baseConverter;
        private readonly bool _baseIsConcrete;

        public PolymorphicConverter(NdjsonOptions options)
        {
            TypeInfo info = typeof(TBase).GetTypeInfo();
            NdjsonPolymorphicAttribute polymorphic = info.GetCustomAttribute<NdjsonPolymorphicAttribute>(false);
            string discriminator = polymorphic != null && !string.IsNullOrEmpty(polymorphic.DiscriminatorName) ? polymorphic.DiscriminatorName : "$type";
            _ignoreUnrecognized = polymorphic != null && polymorphic.IgnoreUnrecognized;
            _discriminatorWithColon = JsonEscaping.EncodePropertyName(discriminator);
            _discriminatorName = JsonEscaping.Encode(discriminator);
            _baseIsConcrete = !info.IsAbstract && !info.IsInterface;

            _typeByTag = new Dictionary<string, Type>(StringComparer.Ordinal);
            _tagByType = new Dictionary<Type, byte[]>();

            foreach (NdjsonDerivedAttribute derived in info.GetCustomAttributes<NdjsonDerivedAttribute>(false))
            {
                if (derived.DerivedType == null)
                {
                    continue;
                }

                string tag = string.IsNullOrEmpty(derived.Discriminator) ? derived.DerivedType.Name : derived.Discriminator;
                _typeByTag[tag] = derived.DerivedType;
                _tagByType[derived.DerivedType] = EncodeQuoted(tag);
            }

            if (_typeByTag.Count == 0)
            {
                throw new NdjsonException("Le type " + typeof(TBase).FullName + " porte [NdjsonPolymorphic] mais aucun [NdjsonDerived].");
            }
        }

        private static byte[] EncodeQuoted(string value)
        {
            byte[] encoded = JsonEscaping.Encode(value);
            byte[] result = new byte[encoded.Length + 2];
            result[0] = JsonConstants.Quote;
            Buffer.BlockCopy(encoded, 0, result, 1, encoded.Length);
            result[result.Length - 1] = JsonConstants.Quote;
            return result;
        }

        public override void Write(ref JsonWriter writer, in TBase value, NdjsonOptions options)
        {
            if (value == null)
            {
                writer.WriteNull();
                return;
            }

            Type runtimeType = value.GetType();
            byte[] tag;
            if (!_tagByType.TryGetValue(runtimeType, out tag))
            {
                if (runtimeType == typeof(TBase) && _baseIsConcrete)
                {
                    BaseConverter(options).Write(ref writer, in value, options);
                    return;
                }

                if (_ignoreUnrecognized)
                {
                    options.GetConverter(runtimeType).WriteObject(ref writer, value, options);
                    return;
                }

                throw new NdjsonException("Le type derive " + runtimeType.FullName + " n'est pas declare via [NdjsonDerived] sur " + typeof(TBase).FullName + ".");
            }

            INdjsonObjectConverter objectConverter = options.GetConverter(runtimeType) as INdjsonObjectConverter;
            if (objectConverter == null)
            {
                throw new NdjsonException("Le converter de " + runtimeType.FullName + " ne prend pas en charge la serialisation polymorphe.");
            }

            writer.WriteStartObject();
            writer.WritePropertyName(_discriminatorWithColon);
            writer.WritePreEncodedString(tag);
            objectConverter.WriteMembers(ref writer, value, options);
            writer.WriteEndObject();
        }

        public override TBase Read(ref JsonReader reader, NdjsonOptions options)
        {
            if (!reader.BeginObject())
            {
                return null;
            }

            int start = reader.TokenStartPosition;

            JsonReader scan = reader;
            string tag = null;
            while (scan.ReadNextProperty())
            {
                if (scan.PropertyEquals(_discriminatorName))
                {
                    scan.Advance();
                    tag = scan.GetString();
                    break;
                }

                scan.SkipValue();
            }

            if (tag == null)
            {
                if (_baseIsConcrete)
                {
                    return BaseConverter(options).Read(ref reader, options);
                }

                throw new NdjsonException("Discriminateur absent : impossible de determiner le type derive de " + typeof(TBase).FullName + ".");
            }

            Type derivedType;
            if (!_typeByTag.TryGetValue(tag, out derivedType))
            {
                if (_ignoreUnrecognized && _baseIsConcrete)
                {
                    return BaseConverter(options).Read(ref reader, options);
                }

                throw new NdjsonException("Discriminateur '" + tag + "' inconnu pour " + typeof(TBase).FullName + ".");
            }

            reader.SkipChildren();
            int end = reader.Position;

            JsonReader nested = new JsonReader(reader.RawBuffer.Slice(start, end - start), options.MaxDepth);
            nested.Advance();
            return (TBase)options.GetConverter(derivedType).ReadObject(ref nested, options);
        }

        private NdjsonConverter<TBase> BaseConverter(NdjsonOptions options)
        {
            NdjsonConverter<TBase> converter = _baseConverter;
            if (converter == null)
            {
                converter = (NdjsonConverter<TBase>)Activator.CreateInstance(typeof(ReflectionObjectConverter<>).MakeGenericType(typeof(TBase)), new object[] { options });
                _baseConverter = converter;
            }

            return converter;
        }
    }
}
