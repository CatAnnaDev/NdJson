using System;
using System.Collections.Generic;
using System.Reflection;
using NdJson.Reflection;
using NdJson.Serialization.Converters;

namespace NdJson.Serialization
{
    internal static class ConverterResolver
    {
        private static readonly Dictionary<Type, NdjsonConverter> BuiltIn = CreateBuiltIn();

        private static Dictionary<Type, NdjsonConverter> CreateBuiltIn()
        {
            Dictionary<Type, NdjsonConverter> map = new Dictionary<Type, NdjsonConverter>(32);
            map[typeof(string)] = StringConverter.Instance;
            map[typeof(bool)] = BooleanConverter.Instance;
            map[typeof(byte)] = ByteConverter.Instance;
            map[typeof(sbyte)] = SByteConverter.Instance;
            map[typeof(short)] = Int16Converter.Instance;
            map[typeof(ushort)] = UInt16Converter.Instance;
            map[typeof(int)] = Int32Converter.Instance;
            map[typeof(uint)] = UInt32Converter.Instance;
            map[typeof(long)] = Int64Converter.Instance;
            map[typeof(ulong)] = UInt64Converter.Instance;
            map[typeof(float)] = SingleConverter.Instance;
            map[typeof(double)] = DoubleConverter.Instance;
            map[typeof(decimal)] = DecimalConverter.Instance;
            map[typeof(char)] = CharConverter.Instance;
            map[typeof(DateTime)] = DateTimeConverter.Instance;
            map[typeof(DateTimeOffset)] = DateTimeOffsetConverter.Instance;
            map[typeof(TimeSpan)] = TimeSpanConverter.Instance;
            map[typeof(Guid)] = GuidConverter.Instance;
            map[typeof(Uri)] = UriConverter.Instance;
            map[typeof(byte[])] = ByteArrayConverter.Instance;
            map[typeof(NdjsonValue)] = NdjsonValueConverter.Instance;
            map[typeof(object)] = ObjectConverter.Instance;
            return map;
        }

        internal static NdjsonConverter Resolve(Type type, NdjsonOptions options)
        {
            IList<NdjsonConverter> userConverters = options.Converters;
            for (int i = userConverters.Count - 1; i >= 0; i--)
            {
                NdjsonConverter candidate = userConverters[i];
                NdjsonConverterFactory factory = candidate as NdjsonConverterFactory;
                if (factory != null)
                {
                    if (factory.CanConvert(type))
                    {
                        return factory.Create(type, options);
                    }

                    continue;
                }

                if (candidate.TargetType == type)
                {
                    return candidate;
                }
            }

            NdjsonConverter registered;
            if (NdjsonConverterRegistry.TryGetExplicit(type, out registered))
            {
                return registered;
            }

            NdjsonConverter builtIn;
            if (BuiltIn.TryGetValue(type, out builtIn))
            {
                return builtIn;
            }

            TypeInfo info = type.GetTypeInfo();

            NdjsonConverterAttribute converterAttribute = info.GetCustomAttribute<NdjsonConverterAttribute>(false);
            if (converterAttribute != null && converterAttribute.ConverterType != null)
            {
                return Instantiate(converterAttribute.ConverterType, type);
            }

            Type generatedConverterType;
            if (NdjsonConverterRegistry.TryGetGenerated(type, out generatedConverterType))
            {
                return Instantiate(generatedConverterType, type);
            }

            Type nullableUnderlying = Nullable.GetUnderlyingType(type);
            if (nullableUnderlying != null)
            {
                return Create(typeof(NullableConverter<>), nullableUnderlying);
            }

            if (info.IsEnum)
            {
                bool forceString = false;
                NdjsonNamingPolicy naming = options.EnumNamingPolicy;
                NdjsonEnumStringAttribute enumAttribute = info.GetCustomAttribute<NdjsonEnumStringAttribute>(false);
                if (enumAttribute != null)
                {
                    forceString = enumAttribute.Enabled;
                    if (enumAttribute.NamingPolicy != NdjsonNamingPolicy.Inherit)
                    {
                        naming = enumAttribute.NamingPolicy;
                    }
                }

                return (NdjsonConverter)Activator.CreateInstance(typeof(EnumConverter<>).MakeGenericType(type), new object[] { forceString, naming });
            }

            if (type.IsArray)
            {
                if (type.GetArrayRank() != 1)
                {
                    throw new NdjsonException("Les tableaux multidimensionnels ne sont pas pris en charge : " + type.FullName + ".");
                }

                return Create(typeof(ArrayConverter<>), type.GetElementType());
            }

            if (info.IsGenericType)
            {
                Type definition = type.GetGenericTypeDefinition();
                Type[] arguments = type.GetGenericArguments();

                if (definition == typeof(List<>))
                {
                    return Create(typeof(ListConverter<>), arguments[0]);
                }

                if (definition == typeof(Dictionary<,>))
                {
                    return Create(typeof(DictionaryConverter<,>), arguments[0], arguments[1]);
                }
            }

            Type dictionaryInterface = FindInterface(type, typeof(IDictionary<,>)) ?? FindInterface(type, typeof(IReadOnlyDictionary<,>));
            if (dictionaryInterface != null)
            {
                Type[] arguments = dictionaryInterface.GetGenericArguments();
                if (info.IsInterface)
                {
                    return Create(typeof(DictionaryInterfaceConverter<,,>), type, arguments[0], arguments[1]);
                }

                if (HasParameterlessConstructor(info) && FindInterface(type, typeof(IDictionary<,>)) != null)
                {
                    return Create(typeof(MutableDictionaryConverter<,,>), type, arguments[0], arguments[1]);
                }

                return Create(typeof(DictionaryInterfaceConverter<,,>), type, arguments[0], arguments[1]);
            }

            Type enumerableInterface = FindInterface(type, typeof(IEnumerable<>));
            if (enumerableInterface != null)
            {
                Type elementType = enumerableInterface.GetGenericArguments()[0];

                if (info.IsInterface)
                {
                    return Create(typeof(EnumerableInterfaceConverter<,>), type, elementType);
                }

                if (FindInterface(type, typeof(ICollection<>)) != null && HasParameterlessConstructor(info))
                {
                    return Create(typeof(CollectionConverter<,>), type, elementType);
                }

                if (HasEnumerableConstructor(type, elementType))
                {
                    return Create(typeof(EnumerableConstructorConverter<,>), type, elementType);
                }

                return Create(typeof(ReadOnlyEnumerableConverter<,>), type, elementType);
            }

            if (!options.EnableReflectionFallback)
            {
                throw new NdjsonException("Aucun converter pour le type " + type.FullName + " et le repli par reflexion est desactive. Ajoutez [NdjsonSerializable] sur le type ou enregistrez un converter.");
            }

            return ReflectionConverterBuilder.Create(type, options);
        }

        private static NdjsonConverter Create(Type openConverter, params Type[] arguments)
        {
            Type closed = openConverter.MakeGenericType(arguments);
            return (NdjsonConverter)Activator.CreateInstance(closed);
        }

        private static NdjsonConverter Instantiate(Type converterType, Type targetType)
        {
            Type resolved = converterType;
            if (converterType.GetTypeInfo().IsGenericTypeDefinition)
            {
                resolved = converterType.MakeGenericType(targetType.GetGenericArguments());
            }

            FieldInfo instanceField = resolved.GetField("Instance", BindingFlags.Public | BindingFlags.Static);
            if (instanceField != null && typeof(NdjsonConverter).GetTypeInfo().IsAssignableFrom(instanceField.FieldType.GetTypeInfo()))
            {
                NdjsonConverter instance = (NdjsonConverter)instanceField.GetValue(null);
                if (instance != null)
                {
                    return instance;
                }
            }

            NdjsonConverter created = Activator.CreateInstance(resolved) as NdjsonConverter;
            if (created == null)
            {
                throw new NdjsonException("Le type " + converterType.FullName + " n'est pas un NdjsonConverter valide.");
            }

            NdjsonConverterFactory factory = created as NdjsonConverterFactory;
            if (factory != null)
            {
                return factory.Create(targetType, NdjsonOptions.Default);
            }

            return created;
        }

        internal static Type FindInterface(Type type, Type openGeneric)
        {
            TypeInfo info = type.GetTypeInfo();
            if (info.IsGenericType && type.GetGenericTypeDefinition() == openGeneric)
            {
                return type;
            }

            foreach (Type candidate in info.ImplementedInterfaces)
            {
                if (candidate.GetTypeInfo().IsGenericType && candidate.GetGenericTypeDefinition() == openGeneric)
                {
                    return candidate;
                }
            }

            return null;
        }

        private static bool HasParameterlessConstructor(TypeInfo info)
        {
            if (info.IsAbstract || info.IsInterface)
            {
                return false;
            }

            foreach (ConstructorInfo constructor in info.DeclaredConstructors)
            {
                if (constructor.IsPublic && constructor.GetParameters().Length == 0)
                {
                    return true;
                }
            }

            return false;
        }

        private static bool HasEnumerableConstructor(Type type, Type elementType)
        {
            return FindEnumerableConstructor(type, elementType) != null;
        }

        internal static ConstructorInfo FindEnumerableConstructor(Type type, Type elementType)
        {
            Type enumerableType = typeof(IEnumerable<>).MakeGenericType(elementType);
            foreach (ConstructorInfo constructor in type.GetTypeInfo().DeclaredConstructors)
            {
                if (!constructor.IsPublic)
                {
                    continue;
                }

                ParameterInfo[] parameters = constructor.GetParameters();
                if (parameters.Length == 1 && parameters[0].ParameterType.GetTypeInfo().IsAssignableFrom(enumerableType.GetTypeInfo()))
                {
                    return constructor;
                }
            }

            return null;
        }
    }
}
