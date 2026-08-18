using System;

namespace NdJson
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Assembly, AllowMultiple = true, Inherited = false)]
    public sealed class NdjsonSerializableAttribute : Attribute
    {
        public NdjsonSerializableAttribute()
        {
        }

        public NdjsonSerializableAttribute(Type type)
        {
            Type = type;
        }

        public Type Type { get; private set; }

        public NdjsonNamingPolicy NamingPolicy { get; set; } = NdjsonNamingPolicy.Inherit;

        public bool IncludeFields { get; set; } = true;

        public bool IncludePrivateMembers { get; set; }

        public NdjsonIgnoreCondition DefaultIgnoreCondition { get; set; } = NdjsonIgnoreCondition.Inherit;

        public string GeneratedConverterName { get; set; }
    }

    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false, Inherited = true)]
    public sealed class NdjsonPropertyAttribute : Attribute
    {
        public NdjsonPropertyAttribute()
        {
        }

        public NdjsonPropertyAttribute(string name)
        {
            Name = name;
        }

        public string Name { get; private set; }

        public int Order { get; set; }

        public bool Required { get; set; }
    }

    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false, Inherited = true)]
    public sealed class NdjsonIgnoreAttribute : Attribute
    {
        public NdjsonIgnoreAttribute()
        {
            Condition = NdjsonIgnoreCondition.Always;
        }

        public NdjsonIgnoreAttribute(NdjsonIgnoreCondition condition)
        {
            Condition = condition;
        }

        public NdjsonIgnoreCondition Condition { get; set; }
    }

    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false, Inherited = true)]
    public sealed class NdjsonIncludeAttribute : Attribute
    {
    }

    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false, Inherited = true)]
    public sealed class NdjsonRequiredAttribute : Attribute
    {
    }

    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Enum | AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Interface, AllowMultiple = false, Inherited = false)]
    public sealed class NdjsonConverterAttribute : Attribute
    {
        public NdjsonConverterAttribute(Type converterType)
        {
            ConverterType = converterType;
        }

        public Type ConverterType { get; private set; }
    }

    [AttributeUsage(AttributeTargets.Enum | AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false, Inherited = false)]
    public sealed class NdjsonEnumStringAttribute : Attribute
    {
        public NdjsonEnumStringAttribute()
        {
            Enabled = true;
        }

        public NdjsonEnumStringAttribute(bool enabled)
        {
            Enabled = enabled;
        }

        public bool Enabled { get; private set; }

        public NdjsonNamingPolicy NamingPolicy { get; set; } = NdjsonNamingPolicy.Inherit;
    }

    [AttributeUsage(AttributeTargets.Field, AllowMultiple = false, Inherited = false)]
    public sealed class NdjsonEnumMemberAttribute : Attribute
    {
        public NdjsonEnumMemberAttribute(string name)
        {
            Name = name;
        }

        public string Name { get; private set; }
    }

    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false, Inherited = false)]
    public sealed class NdjsonDateFormatAttribute : Attribute
    {
        public NdjsonDateFormatAttribute(NdjsonDateFormat format)
        {
            Format = format;
        }

        public NdjsonDateFormat Format { get; private set; }
    }

    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false, Inherited = false)]
    public sealed class NdjsonExtensionDataAttribute : Attribute
    {
    }

    [AttributeUsage(AttributeTargets.Constructor, AllowMultiple = false, Inherited = false)]
    public sealed class NdjsonConstructorAttribute : Attribute
    {
    }

    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Interface, AllowMultiple = false, Inherited = false)]
    public sealed class NdjsonPolymorphicAttribute : Attribute
    {
        public NdjsonPolymorphicAttribute()
        {
        }

        public NdjsonPolymorphicAttribute(string discriminatorName)
        {
            DiscriminatorName = discriminatorName;
        }

        public string DiscriminatorName { get; set; } = "$type";

        public bool IgnoreUnrecognized { get; set; }
    }

    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Interface, AllowMultiple = true, Inherited = false)]
    public sealed class NdjsonDerivedAttribute : Attribute
    {
        public NdjsonDerivedAttribute(Type derivedType)
        {
            DerivedType = derivedType;
        }

        public NdjsonDerivedAttribute(Type derivedType, string discriminator)
        {
            DerivedType = derivedType;
            Discriminator = discriminator;
        }

        public Type DerivedType { get; private set; }

        public string Discriminator { get; private set; }
    }

    [AttributeUsage(AttributeTargets.Assembly, AllowMultiple = false, Inherited = false)]
    public sealed class NdjsonDefaultsAttribute : Attribute
    {
        public NdjsonNamingPolicy NamingPolicy { get; set; } = NdjsonNamingPolicy.Inherit;

        public NdjsonIgnoreCondition DefaultIgnoreCondition { get; set; } = NdjsonIgnoreCondition.Inherit;

        public bool IncludeFields { get; set; } = true;
    }

    [AttributeUsage(AttributeTargets.Assembly, AllowMultiple = true, Inherited = false)]
    public sealed class NdjsonGeneratedConverterAttribute : Attribute
    {
        public NdjsonGeneratedConverterAttribute(Type targetType, Type converterType)
        {
            TargetType = targetType;
            ConverterType = converterType;
        }

        public Type TargetType { get; private set; }

        public Type ConverterType { get; private set; }
    }

    public enum NdjsonIgnoreCondition
    {
        Inherit = 0,
        Never = 1,
        Always = 2,
        WhenWritingNull = 3,
        WhenWritingDefault = 4
    }

    public enum NdjsonDateFormat
    {
        Inherit = 0,
        Iso8601 = 1,
        UnixSeconds = 2,
        UnixMilliseconds = 3,
        Ticks = 4
    }

    public enum NdjsonNonFiniteHandling
    {
        Throw = 0,
        WriteNull = 1,
        WriteString = 2
    }
}
