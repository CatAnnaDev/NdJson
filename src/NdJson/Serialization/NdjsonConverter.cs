using System;

namespace NdJson.Serialization
{
    public abstract class NdjsonConverter
    {
        public abstract Type TargetType { get; }

        public abstract void WriteObject(ref JsonWriter writer, object value, NdjsonOptions options);

        public abstract object ReadObject(ref JsonReader reader, NdjsonOptions options);
    }

    public abstract class NdjsonConverter<T> : NdjsonConverter
    {
        public override Type TargetType
        {
            get { return typeof(T); }
        }

        public abstract void Write(ref JsonWriter writer, in T value, NdjsonOptions options);

        public abstract T Read(ref JsonReader reader, NdjsonOptions options);

        public override void WriteObject(ref JsonWriter writer, object value, NdjsonOptions options)
        {
            if (value == null && default(T) == null)
            {
                writer.WriteNull();
                return;
            }

            T typed = (T)value;
            Write(ref writer, in typed, options);
        }

        public override object ReadObject(ref JsonReader reader, NdjsonOptions options)
        {
            return Read(ref reader, options);
        }
    }

    public abstract class NdjsonConverterFactory : NdjsonConverter
    {
        public override Type TargetType
        {
            get { return null; }
        }

        public abstract bool CanConvert(Type type);

        public abstract NdjsonConverter Create(Type type, NdjsonOptions options);

        public override void WriteObject(ref JsonWriter writer, object value, NdjsonOptions options)
        {
            throw new NotSupportedException("Une fabrique de converters ne serialise pas directement.");
        }

        public override object ReadObject(ref JsonReader reader, NdjsonOptions options)
        {
            throw new NotSupportedException("Une fabrique de converters ne deserialise pas directement.");
        }
    }
}
