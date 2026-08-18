using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;
using NdJson.Serialization;
using NdJson.Serialization.Converters;

namespace NdJson.Reflection
{
    internal static class ReflectionConverterBuilder
    {
        internal static NdjsonConverter Create(Type type, NdjsonOptions options)
        {
            TypeInfo info = type.GetTypeInfo();

            if (info.GetCustomAttribute<NdjsonPolymorphicAttribute>(false) != null || info.GetCustomAttribute<NdjsonDerivedAttribute>(false) != null)
            {
                return (NdjsonConverter)Activator.CreateInstance(typeof(PolymorphicConverter<>).MakeGenericType(type), new object[] { options });
            }

            if (info.IsAbstract || info.IsInterface)
            {
                throw new NdjsonException("Impossible de deserialiser le type abstrait ou l'interface " + type.FullName + " sans attribut [NdjsonPolymorphic].");
            }

            return (NdjsonConverter)Activator.CreateInstance(typeof(ReflectionObjectConverter<>).MakeGenericType(type), new object[] { options });
        }
    }

    internal sealed class ReflectionObjectConverter<T> : NdjsonConverter<T>, INdjsonObjectConverter
    {
        private readonly ReflectionMember[] _members;
        private readonly ReflectionMember _extensionData;
        private readonly Type _extensionValueType;
        private readonly ConstructorInfo _constructor;
        private readonly int[] _constructorMemberIndex;
        private readonly object[] _constructorDefaults;
        private readonly Func<object> _factory;
        private readonly bool _isValueType;
        private readonly bool _hasRequired;

        public ReflectionObjectConverter(NdjsonOptions options)
        {
            Type type = typeof(T);
            TypeInfo info = type.GetTypeInfo();
            _isValueType = info.IsValueType;

            ReflectionMember extension;
            List<ReflectionMember> members = ReflectionMemberFactory.Collect(type, options, out extension);
            _members = members.ToArray();
            _extensionData = extension;

            if (extension != null)
            {
                Type dictionaryInterface = ConverterResolver.FindInterface(extension.MemberType, typeof(IDictionary<,>));
                if (dictionaryInterface == null)
                {
                    throw new NdjsonException("[NdjsonExtensionData] requiert un membre de type IDictionary<string, NdjsonValue> ou IDictionary<string, object> sur " + type.FullName + ".");
                }

                _extensionValueType = dictionaryInterface.GetGenericArguments()[1];
            }

            for (int i = 0; i < _members.Length; i++)
            {
                if (_members[i].Required)
                {
                    _hasRequired = true;
                }
            }

            if (!_isValueType)
            {
                ConstructorInfo selected = SelectConstructor(info, _members);
                if (selected == null)
                {
                    throw new NdjsonException("Aucun constructeur utilisable pour " + type.FullName + ".");
                }

                ParameterInfo[] parameters = selected.GetParameters();
                if (parameters.Length == 0)
                {
                    _factory = CreateFactory(type, selected);
                }
                else
                {
                    _constructor = selected;
                    _constructorMemberIndex = new int[parameters.Length];
                    _constructorDefaults = new object[parameters.Length];

                    for (int i = 0; i < parameters.Length; i++)
                    {
                        _constructorMemberIndex[i] = FindMemberForParameter(parameters[i].Name);
                        _constructorDefaults[i] = parameters[i].HasDefaultValue
                            ? parameters[i].DefaultValue
                            : (parameters[i].ParameterType.GetTypeInfo().IsValueType ? Activator.CreateInstance(parameters[i].ParameterType) : null);
                    }
                }
            }
        }

        private static ConstructorInfo SelectConstructor(TypeInfo info, ReflectionMember[] members)
        {
            ConstructorInfo parameterless = null;
            ConstructorInfo widest = null;
            int widestCount = -1;

            foreach (ConstructorInfo constructor in info.DeclaredConstructors)
            {
                if (constructor.IsStatic)
                {
                    continue;
                }

                if (constructor.GetCustomAttribute<NdjsonConstructorAttribute>() != null)
                {
                    return constructor;
                }

                if (!constructor.IsPublic)
                {
                    continue;
                }

                ParameterInfo[] parameters = constructor.GetParameters();
                if (parameters.Length == 0)
                {
                    parameterless = constructor;
                    continue;
                }

                if (parameters.Length > widestCount)
                {
                    widestCount = parameters.Length;
                    widest = constructor;
                }
            }

            if (parameterless == null)
            {
                return widest;
            }

            if (widest != null && RecoversUnsettableMembers(widest, members))
            {
                return widest;
            }

            return parameterless;
        }

        private static bool RecoversUnsettableMembers(ConstructorInfo constructor, ReflectionMember[] members)
        {
            ParameterInfo[] parameters = constructor.GetParameters();
            for (int i = 0; i < members.Length; i++)
            {
                if (members[i].Setter != null)
                {
                    continue;
                }

                for (int j = 0; j < parameters.Length; j++)
                {
                    if (string.Equals(parameters[j].Name, members[i].ParameterName, StringComparison.OrdinalIgnoreCase))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        private int FindMemberForParameter(string parameterName)
        {
            for (int i = 0; i < _members.Length; i++)
            {
                if (string.Equals(_members[i].ParameterName, parameterName, StringComparison.OrdinalIgnoreCase))
                {
                    return i;
                }
            }

            for (int i = 0; i < _members.Length; i++)
            {
                if (string.Equals(_members[i].Name, parameterName, StringComparison.OrdinalIgnoreCase))
                {
                    return i;
                }
            }

            return -1;
        }

        private static Func<object> CreateFactory(Type type, ConstructorInfo constructor)
        {
            if (DynamicCodeSupport.IsSupported)
            {
                try
                {
                    return Expression.Lambda<Func<object>>(Expression.Convert(Expression.New(constructor), typeof(object))).Compile();
                }
                catch (Exception)
                {
                }
            }

            return () => Activator.CreateInstance(type);
        }

        public override void Write(ref JsonWriter writer, in T value, NdjsonOptions options)
        {
            if (!_isValueType && value == null)
            {
                writer.WriteNull();
                return;
            }

            writer.WriteStartObject();
            WriteMembers(ref writer, value, options);
            writer.WriteEndObject();
        }

        public void WriteMembers(ref JsonWriter writer, object value, NdjsonOptions options)
        {
            ReflectionMember[] members = _members;
            for (int i = 0; i < members.Length; i++)
            {
                ReflectionMember member = members[i];
                object memberValue = member.Getter(value);

                if (member.IgnoreCondition == NdjsonIgnoreCondition.WhenWritingNull && memberValue == null)
                {
                    continue;
                }

                if (member.IgnoreCondition == NdjsonIgnoreCondition.WhenWritingDefault && IsDefault(member, memberValue))
                {
                    continue;
                }

                writer.WritePropertyName(member.EncodedNameWithColon);
                member.Converter(options).WriteObject(ref writer, memberValue, options);
            }

            if (_extensionData != null)
            {
                IDictionary extra = _extensionData.Getter(value) as IDictionary;
                if (extra != null)
                {
                    NdjsonConverter valueConverter = options.GetConverter(_extensionValueType);
                    foreach (DictionaryEntry entry in extra)
                    {
                        writer.WritePropertyName((string)entry.Key);
                        valueConverter.WriteObject(ref writer, entry.Value, options);
                    }
                }
            }
        }

        private static bool IsDefault(ReflectionMember member, object value)
        {
            if (value == null)
            {
                return true;
            }

            object defaultValue = member.DefaultValue;
            return defaultValue != null && defaultValue.Equals(value);
        }

        public override T Read(ref JsonReader reader, NdjsonOptions options)
        {
            if (!reader.BeginObject())
            {
                return default(T);
            }

            if (_constructor != null)
            {
                return ReadWithConstructor(ref reader, options);
            }

            object instance = _isValueType ? (object)default(T) : _factory();
            bool[] seen = _hasRequired ? new bool[_members.Length] : null;

            while (reader.ReadNextProperty())
            {
                int index = FindMember(ref reader, options);
                if (index < 0)
                {
                    ReadUnknown(ref reader, options, instance);
                    continue;
                }

                ReflectionMember member = _members[index];
                if (member.Setter == null)
                {
                    reader.SkipValue();
                    continue;
                }

                reader.Advance();
                member.Setter(instance, member.Converter(options).ReadObject(ref reader, options));
                if (seen != null)
                {
                    seen[index] = true;
                }
            }

            VerifyRequired(seen, options);
            return (T)instance;
        }

        private T ReadWithConstructor(ref JsonReader reader, NdjsonOptions options)
        {
            object[] slots = new object[_members.Length];
            bool[] assigned = new bool[_members.Length];

            while (reader.ReadNextProperty())
            {
                int index = FindMember(ref reader, options);
                if (index < 0)
                {
                    ReadUnknown(ref reader, options, null);
                    continue;
                }

                ReflectionMember member = _members[index];
                reader.Advance();
                slots[index] = member.Converter(options).ReadObject(ref reader, options);
                assigned[index] = true;
            }

            object[] arguments = new object[_constructorMemberIndex.Length];
            for (int i = 0; i < arguments.Length; i++)
            {
                int memberIndex = _constructorMemberIndex[i];
                arguments[i] = memberIndex >= 0 && assigned[memberIndex] ? slots[memberIndex] : _constructorDefaults[i];
            }

            object instance = _constructor.Invoke(arguments);

            for (int i = 0; i < _members.Length; i++)
            {
                if (assigned[i] && _members[i].Setter != null && !IsConsumedByConstructor(i))
                {
                    _members[i].Setter(instance, slots[i]);
                }
            }

            VerifyRequired(_hasRequired ? assigned : null, options);
            return (T)instance;
        }

        private bool IsConsumedByConstructor(int memberIndex)
        {
            for (int i = 0; i < _constructorMemberIndex.Length; i++)
            {
                if (_constructorMemberIndex[i] == memberIndex)
                {
                    return true;
                }
            }

            return false;
        }

        private void VerifyRequired(bool[] seen, NdjsonOptions options)
        {
            if (seen == null || !options.ThrowOnMissingRequired)
            {
                return;
            }

            for (int i = 0; i < _members.Length; i++)
            {
                if (_members[i].Required && !seen[i])
                {
                    throw new NdjsonException("Propriete requise absente : '" + _members[i].Name + "' sur " + typeof(T).FullName + ".");
                }
            }
        }

        private void ReadUnknown(ref JsonReader reader, NdjsonOptions options, object instance)
        {
            if (_extensionData == null || instance == null)
            {
                reader.SkipValue();
                return;
            }

            string name = reader.GetString();
            IDictionary target = _extensionData.Getter(instance) as IDictionary;
            if (target == null)
            {
                if (_extensionData.Setter == null)
                {
                    reader.SkipValue();
                    return;
                }

                target = (IDictionary)Activator.CreateInstance(_extensionData.MemberType);
                _extensionData.Setter(instance, target);
            }

            reader.Advance();
            target[name] = options.GetConverter(_extensionValueType).ReadObject(ref reader, options);
        }

        private int FindMember(ref JsonReader reader, NdjsonOptions options)
        {
            ReflectionMember[] members = _members;
            if (options.PropertyNameCaseInsensitive)
            {
                for (int i = 0; i < members.Length; i++)
                {
                    if (reader.PropertyEqualsIgnoreCase(members[i].EncodedName))
                    {
                        return i;
                    }
                }

                return -1;
            }

            for (int i = 0; i < members.Length; i++)
            {
                if (reader.PropertyEquals(members[i].EncodedName))
                {
                    return i;
                }
            }

            return -1;
        }
    }
}
