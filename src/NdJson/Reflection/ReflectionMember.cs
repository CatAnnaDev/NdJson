using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;
using NdJson.Serialization;
using NdJson.Serialization.Converters;

namespace NdJson.Reflection
{
    internal sealed class ReflectionMember
    {
        internal string Name;
        internal byte[] EncodedNameWithColon;
        internal byte[] EncodedName;
        internal Type MemberType;
        internal MemberInfo Member;
        internal Func<object, object> Getter;
        internal Action<object, object> Setter;
        internal NdjsonIgnoreCondition IgnoreCondition;
        internal bool Required;
        internal int Order;
        internal bool IsExtensionData;
        internal object DefaultValue;
        internal string ParameterName;
        internal int Index;

        private NdjsonConverter _converter;
        private Type _explicitConverterType;

        internal void SetExplicitConverter(Type converterType)
        {
            _explicitConverterType = converterType;
        }

        internal void SetPreResolvedConverter(NdjsonConverter converter)
        {
            _converter = converter;
        }

        internal NdjsonConverter Converter(NdjsonOptions options)
        {
            NdjsonConverter converter = _converter;
            if (converter != null)
            {
                return converter;
            }

            if (_explicitConverterType != null)
            {
                converter = Activator.CreateInstance(_explicitConverterType) as NdjsonConverter;
                if (converter == null)
                {
                    throw new NdjsonException("Le converter " + _explicitConverterType.FullName + " declare sur " + Name + " n'est pas un NdjsonConverter.");
                }
            }
            else
            {
                converter = options.GetConverter(MemberType);
            }

            _converter = converter;
            return converter;
        }

        internal static Func<object, object> CreateGetter(MemberInfo member, Type declaringType)
        {
            PropertyInfo property = member as PropertyInfo;
            FieldInfo field = member as FieldInfo;

            if (DynamicCodeSupport.IsSupported && !declaringType.GetTypeInfo().IsValueType)
            {
                try
                {
                    ParameterExpression instance = Expression.Parameter(typeof(object), "instance");
                    Expression access = Expression.MakeMemberAccess(Expression.Convert(instance, declaringType), member);
                    return Expression.Lambda<Func<object, object>>(Expression.Convert(access, typeof(object)), instance).Compile();
                }
                catch (Exception)
                {
                }
            }

            if (property != null)
            {
                return instance => property.GetValue(instance, null);
            }

            return instance => field.GetValue(instance);
        }

        internal static Action<object, object> CreateSetter(MemberInfo member, Type declaringType)
        {
            PropertyInfo property = member as PropertyInfo;
            FieldInfo field = member as FieldInfo;

            if (property != null && !property.CanWrite)
            {
                return null;
            }

            if (field != null && (field.IsInitOnly || field.IsLiteral))
            {
                return null;
            }

            if (DynamicCodeSupport.IsSupported && !declaringType.GetTypeInfo().IsValueType)
            {
                try
                {
                    ParameterExpression instance = Expression.Parameter(typeof(object), "instance");
                    ParameterExpression value = Expression.Parameter(typeof(object), "value");
                    Type memberType = property != null ? property.PropertyType : field.FieldType;
                    Expression target = Expression.MakeMemberAccess(Expression.Convert(instance, declaringType), member);
                    Expression assign = Expression.Assign(target, Expression.Convert(value, memberType));
                    return Expression.Lambda<Action<object, object>>(assign, instance, value).Compile();
                }
                catch (Exception)
                {
                }
            }

            if (property != null)
            {
                return (instance, value) => property.SetValue(instance, value, null);
            }

            return (instance, value) => field.SetValue(instance, value);
        }
    }

    internal static class ReflectionMemberFactory
    {
        internal static List<ReflectionMember> Collect(Type type, NdjsonOptions options, out ReflectionMember extensionData)
        {
            NdjsonSerializableAttribute typeAttribute = type.GetTypeInfo().GetCustomAttribute<NdjsonSerializableAttribute>(false);
            NdjsonNamingPolicy naming = options.NamingPolicy;
            bool includeFields = options.IncludeFields;
            bool includePrivate = false;
            NdjsonIgnoreCondition defaultIgnore = options.DefaultIgnoreCondition;

            if (typeAttribute != null)
            {
                if (typeAttribute.NamingPolicy != NdjsonNamingPolicy.Inherit)
                {
                    naming = typeAttribute.NamingPolicy;
                }

                includeFields = typeAttribute.IncludeFields;
                includePrivate = typeAttribute.IncludePrivateMembers;
                if (typeAttribute.DefaultIgnoreCondition != NdjsonIgnoreCondition.Inherit)
                {
                    defaultIgnore = typeAttribute.DefaultIgnoreCondition;
                }
            }

            List<List<ReflectionMember>> levels = new List<List<ReflectionMember>>();
            HashSet<string> seen = new HashSet<string>(StringComparer.Ordinal);
            extensionData = null;

            Type current = type;
            int depth = 0;
            while (current != null && current != typeof(object))
            {
                TypeInfo info = current.GetTypeInfo();
                List<ReflectionMember> members = new List<ReflectionMember>();

                foreach (PropertyInfo property in info.DeclaredProperties)
                {
                    if (property.GetIndexParameters().Length != 0)
                    {
                        continue;
                    }

                    MethodInfo getter = property.GetMethod;
                    if (getter == null || getter.IsStatic)
                    {
                        continue;
                    }

                    bool isPublic = getter.IsPublic;
                    bool forced = property.GetCustomAttribute<NdjsonIncludeAttribute>() != null;
                    if (!isPublic && !forced && !includePrivate)
                    {
                        continue;
                    }

                    if (!seen.Add(property.Name))
                    {
                        continue;
                    }

                    ReflectionMember member = Build(property, property.PropertyType, current, naming, defaultIgnore, depth, options);
                    if (member == null)
                    {
                        continue;
                    }

                    if (member.IsExtensionData)
                    {
                        extensionData = member;
                        continue;
                    }

                    members.Add(member);
                }

                foreach (FieldInfo field in info.DeclaredFields)
                {
                    if (field.IsStatic || field.IsLiteral)
                    {
                        continue;
                    }

                    bool forced = field.GetCustomAttribute<NdjsonIncludeAttribute>() != null;
                    if (!forced)
                    {
                        if (!includeFields || !field.IsPublic)
                        {
                            continue;
                        }
                    }

                    if (field.Name.IndexOf('<') >= 0)
                    {
                        continue;
                    }

                    if (!seen.Add(field.Name))
                    {
                        continue;
                    }

                    ReflectionMember member = Build(field, field.FieldType, current, naming, defaultIgnore, depth, options);
                    if (member == null)
                    {
                        continue;
                    }

                    if (member.IsExtensionData)
                    {
                        extensionData = member;
                        continue;
                    }

                    members.Add(member);
                }

                levels.Add(members);
                current = info.BaseType;
                depth++;
            }

            List<ReflectionMember> ordered = new List<ReflectionMember>();
            for (int level = levels.Count - 1; level >= 0; level--)
            {
                ordered.AddRange(levels[level]);
            }

            for (int i = 0; i < ordered.Count; i++)
            {
                ordered[i].Index = i;
            }

            ordered.Sort(CompareMembers);
            return ordered;
        }

        private static int CompareMembers(ReflectionMember left, ReflectionMember right)
        {
            if (left.Order != right.Order)
            {
                return left.Order.CompareTo(right.Order);
            }

            return left.Index.CompareTo(right.Index);
        }

        private static ReflectionMember Build(MemberInfo member, Type memberType, Type declaringType, NdjsonNamingPolicy naming, NdjsonIgnoreCondition defaultIgnore, int depth, NdjsonOptions options)
        {
            NdjsonIgnoreAttribute ignore = member.GetCustomAttribute<NdjsonIgnoreAttribute>();
            if (ignore != null && ignore.Condition == NdjsonIgnoreCondition.Always)
            {
                return null;
            }

            ReflectionMember result = new ReflectionMember();
            result.Member = member;
            result.MemberType = memberType;
            result.IgnoreCondition = ignore != null && ignore.Condition != NdjsonIgnoreCondition.Inherit ? ignore.Condition : defaultIgnore;
            result.IsExtensionData = member.GetCustomAttribute<NdjsonExtensionDataAttribute>() != null;

            NdjsonPropertyAttribute propertyAttribute = member.GetCustomAttribute<NdjsonPropertyAttribute>();
            string name = propertyAttribute != null && !string.IsNullOrEmpty(propertyAttribute.Name)
                ? propertyAttribute.Name
                : NdjsonNaming.Convert(member.Name, naming);

            result.Name = name;
            result.ParameterName = member.Name;
            result.Order = propertyAttribute != null ? propertyAttribute.Order : 0;
            result.Required = (propertyAttribute != null && propertyAttribute.Required) || member.GetCustomAttribute<NdjsonRequiredAttribute>() != null;
            result.EncodedNameWithColon = JsonEscaping.EncodePropertyName(name);
            result.EncodedName = JsonEscaping.Encode(name);
            result.Getter = ReflectionMember.CreateGetter(member, declaringType);
            result.Setter = ReflectionMember.CreateSetter(member, declaringType);
            result.DefaultValue = memberType.GetTypeInfo().IsValueType ? Activator.CreateInstance(memberType) : null;

            NdjsonConverterAttribute converterAttribute = member.GetCustomAttribute<NdjsonConverterAttribute>();
            if (converterAttribute != null && converterAttribute.ConverterType != null)
            {
                result.SetExplicitConverter(converterAttribute.ConverterType);
            }
            else
            {
                NdjsonDateFormatAttribute dateFormat = member.GetCustomAttribute<NdjsonDateFormatAttribute>();
                if (dateFormat != null)
                {
                    if (memberType == typeof(DateTime))
                    {
                        result.SetPreResolvedConverter(new DateTimeConverter(dateFormat.Format));
                    }
                    else if (memberType == typeof(DateTimeOffset))
                    {
                        result.SetPreResolvedConverter(new DateTimeOffsetConverter(dateFormat.Format));
                    }
                }
                else
                {
                    NdjsonEnumStringAttribute enumString = member.GetCustomAttribute<NdjsonEnumStringAttribute>();
                    Type enumType = Nullable.GetUnderlyingType(memberType) ?? memberType;
                    if (enumString != null && enumType.GetTypeInfo().IsEnum)
                    {
                        NdjsonNamingPolicy enumNaming = enumString.NamingPolicy == NdjsonNamingPolicy.Inherit ? options.EnumNamingPolicy : enumString.NamingPolicy;
                        NdjsonConverter enumConverter = (NdjsonConverter)Activator.CreateInstance(typeof(EnumConverter<>).MakeGenericType(enumType), new object[] { enumString.Enabled, enumNaming });
                        if (enumType != memberType)
                        {
                            enumConverter = (NdjsonConverter)Activator.CreateInstance(typeof(NullableWrappingConverter<>).MakeGenericType(enumType), new object[] { enumConverter });
                        }

                        result.SetPreResolvedConverter(enumConverter);
                    }
                }
            }

            return result;
        }
    }
}
