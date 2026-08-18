#if GODOT
using System;
using System.Collections.Generic;
using System.IO;
using Godot;
using NdJson.Serialization;

namespace NdJson.GodotIntegration
{
    public static class NdjsonGodot
    {
        private static bool _registered;

#if NET5_0_OR_GREATER
#pragma warning disable CA2255
        [System.Runtime.CompilerServices.ModuleInitializer]
        internal static void Initialize()
        {
            Register();
        }
#pragma warning restore CA2255
#endif

        public static void Register()
        {
            if (_registered)
            {
                return;
            }

            _registered = true;

            NdjsonConverterRegistry.Register(new GodotVector2Converter());
            NdjsonConverterRegistry.Register(new GodotVector3Converter());
            NdjsonConverterRegistry.Register(new GodotVector4Converter());
            NdjsonConverterRegistry.Register(new GodotVector2IConverter());
            NdjsonConverterRegistry.Register(new GodotVector3IConverter());
            NdjsonConverterRegistry.Register(new GodotQuaternionConverter());
            NdjsonConverterRegistry.Register(new GodotColorConverter());
            NdjsonConverterRegistry.Register(new GodotRect2Converter());
            NdjsonConverterRegistry.Register(new GodotAabbConverter());
        }

        public static IEnumerable<T> ReadLines<T>(string godotPath, NdjsonOptions options = null)
        {
            Register();
            byte[] data = Godot.FileAccess.GetFileAsBytes(godotPath);
            if (data == null || data.Length == 0)
            {
                return new List<T>();
            }

            return NdjsonSerializer.DeserializeLines<T>(data, options);
        }

        public static List<T> ReadAll<T>(string godotPath, NdjsonOptions options = null)
        {
            return new List<T>(ReadLines<T>(godotPath, options));
        }

        public static void WriteAll<T>(string godotPath, IEnumerable<T> values, NdjsonOptions options = null, bool append = false)
        {
            Register();
            string real = ProjectSettings.GlobalizePath(godotPath);
            string directory = Path.GetDirectoryName(real);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            NdjsonFile.WriteAll(real, values, options, append);
        }

        public static void Append<T>(string godotPath, T value, NdjsonOptions options = null)
        {
            Register();
            NdjsonFile.Append(ProjectSettings.GlobalizePath(godotPath), value, options);
        }
    }

    internal static class ComponentKey
    {
        internal static char Read(ref JsonReader reader)
        {
            ReadOnlySpan<byte> name = reader.PropertyNameSpan;
            if (name.Length != 1)
            {
                return '\0';
            }

            return char.ToLowerInvariant((char)name[0]);
        }
    }

    public sealed class GodotVector2Converter : NdjsonConverter<Vector2>
    {
        private static readonly byte[] N0 = NdjsonGeneratedSupport.EncodeName("x");
        private static readonly byte[] N1 = NdjsonGeneratedSupport.EncodeName("y");

        public override void Write(ref JsonWriter writer, in Vector2 value, NdjsonOptions options)
        {
            writer.WriteStartObject();
            writer.WritePropertyName(N0);
            writer.WriteNumber((float)value.X, options.NonFiniteHandling);
            writer.WritePropertyName(N1);
            writer.WriteNumber((float)value.Y, options.NonFiniteHandling);
            writer.WriteEndObject();
        }

        public override Vector2 Read(ref JsonReader reader, NdjsonOptions options)
        {
            float a = 0f;
            float b = 0f;

            if (reader.TokenType == JsonTokenType.StartArray)
            {
                int index = 0;
                while (reader.ReadNextArrayElement())
                {
                    float component = reader.GetSingle();
                    switch (index)
                    {
                        case 0:
                            a = component;
                            break;
                        case 1:
                            b = component;
                            break;
                    }

                    index++;
                }

                return new Vector2(a, b);
            }

            if (!reader.BeginObject())
            {
                return new Vector2(a, b);
            }

            while (reader.ReadNextProperty())
            {
                char key = ComponentKey.Read(ref reader);
                reader.Advance();
                switch (key)
                {
                    case 'x':
                        a = reader.GetSingle();
                        break;
                    case 'y':
                        b = reader.GetSingle();
                        break;
                    default:
                        reader.SkipChildren();
                        break;
                }
            }

            return new Vector2(a, b);
        }
    }

    public sealed class GodotVector3Converter : NdjsonConverter<Vector3>
    {
        private static readonly byte[] N0 = NdjsonGeneratedSupport.EncodeName("x");
        private static readonly byte[] N1 = NdjsonGeneratedSupport.EncodeName("y");
        private static readonly byte[] N2 = NdjsonGeneratedSupport.EncodeName("z");

        public override void Write(ref JsonWriter writer, in Vector3 value, NdjsonOptions options)
        {
            writer.WriteStartObject();
            writer.WritePropertyName(N0);
            writer.WriteNumber((float)value.X, options.NonFiniteHandling);
            writer.WritePropertyName(N1);
            writer.WriteNumber((float)value.Y, options.NonFiniteHandling);
            writer.WritePropertyName(N2);
            writer.WriteNumber((float)value.Z, options.NonFiniteHandling);
            writer.WriteEndObject();
        }

        public override Vector3 Read(ref JsonReader reader, NdjsonOptions options)
        {
            float a = 0f;
            float b = 0f;
            float c = 0f;

            if (reader.TokenType == JsonTokenType.StartArray)
            {
                int index = 0;
                while (reader.ReadNextArrayElement())
                {
                    float component = reader.GetSingle();
                    switch (index)
                    {
                        case 0:
                            a = component;
                            break;
                        case 1:
                            b = component;
                            break;
                        case 2:
                            c = component;
                            break;
                    }

                    index++;
                }

                return new Vector3(a, b, c);
            }

            if (!reader.BeginObject())
            {
                return new Vector3(a, b, c);
            }

            while (reader.ReadNextProperty())
            {
                char key = ComponentKey.Read(ref reader);
                reader.Advance();
                switch (key)
                {
                    case 'x':
                        a = reader.GetSingle();
                        break;
                    case 'y':
                        b = reader.GetSingle();
                        break;
                    case 'z':
                        c = reader.GetSingle();
                        break;
                    default:
                        reader.SkipChildren();
                        break;
                }
            }

            return new Vector3(a, b, c);
        }
    }

    public sealed class GodotVector4Converter : NdjsonConverter<Vector4>
    {
        private static readonly byte[] N0 = NdjsonGeneratedSupport.EncodeName("x");
        private static readonly byte[] N1 = NdjsonGeneratedSupport.EncodeName("y");
        private static readonly byte[] N2 = NdjsonGeneratedSupport.EncodeName("z");
        private static readonly byte[] N3 = NdjsonGeneratedSupport.EncodeName("w");

        public override void Write(ref JsonWriter writer, in Vector4 value, NdjsonOptions options)
        {
            writer.WriteStartObject();
            writer.WritePropertyName(N0);
            writer.WriteNumber((float)value.X, options.NonFiniteHandling);
            writer.WritePropertyName(N1);
            writer.WriteNumber((float)value.Y, options.NonFiniteHandling);
            writer.WritePropertyName(N2);
            writer.WriteNumber((float)value.Z, options.NonFiniteHandling);
            writer.WritePropertyName(N3);
            writer.WriteNumber((float)value.W, options.NonFiniteHandling);
            writer.WriteEndObject();
        }

        public override Vector4 Read(ref JsonReader reader, NdjsonOptions options)
        {
            float a = 0f;
            float b = 0f;
            float c = 0f;
            float d = 0f;

            if (reader.TokenType == JsonTokenType.StartArray)
            {
                int index = 0;
                while (reader.ReadNextArrayElement())
                {
                    float component = reader.GetSingle();
                    switch (index)
                    {
                        case 0:
                            a = component;
                            break;
                        case 1:
                            b = component;
                            break;
                        case 2:
                            c = component;
                            break;
                        case 3:
                            d = component;
                            break;
                    }

                    index++;
                }

                return new Vector4(a, b, c, d);
            }

            if (!reader.BeginObject())
            {
                return new Vector4(a, b, c, d);
            }

            while (reader.ReadNextProperty())
            {
                char key = ComponentKey.Read(ref reader);
                reader.Advance();
                switch (key)
                {
                    case 'x':
                        a = reader.GetSingle();
                        break;
                    case 'y':
                        b = reader.GetSingle();
                        break;
                    case 'z':
                        c = reader.GetSingle();
                        break;
                    case 'w':
                        d = reader.GetSingle();
                        break;
                    default:
                        reader.SkipChildren();
                        break;
                }
            }

            return new Vector4(a, b, c, d);
        }
    }

    public sealed class GodotVector2IConverter : NdjsonConverter<Vector2I>
    {
        private static readonly byte[] N0 = NdjsonGeneratedSupport.EncodeName("x");
        private static readonly byte[] N1 = NdjsonGeneratedSupport.EncodeName("y");

        public override void Write(ref JsonWriter writer, in Vector2I value, NdjsonOptions options)
        {
            writer.WriteStartObject();
            writer.WritePropertyName(N0);
            writer.WriteNumber((float)value.X, options.NonFiniteHandling);
            writer.WritePropertyName(N1);
            writer.WriteNumber((float)value.Y, options.NonFiniteHandling);
            writer.WriteEndObject();
        }

        public override Vector2I Read(ref JsonReader reader, NdjsonOptions options)
        {
            float a = 0f;
            float b = 0f;

            if (reader.TokenType == JsonTokenType.StartArray)
            {
                int index = 0;
                while (reader.ReadNextArrayElement())
                {
                    float component = reader.GetSingle();
                    switch (index)
                    {
                        case 0:
                            a = component;
                            break;
                        case 1:
                            b = component;
                            break;
                    }

                    index++;
                }

                return new Vector2I((int)a, (int)b);
            }

            if (!reader.BeginObject())
            {
                return new Vector2I((int)a, (int)b);
            }

            while (reader.ReadNextProperty())
            {
                char key = ComponentKey.Read(ref reader);
                reader.Advance();
                switch (key)
                {
                    case 'x':
                        a = reader.GetSingle();
                        break;
                    case 'y':
                        b = reader.GetSingle();
                        break;
                    default:
                        reader.SkipChildren();
                        break;
                }
            }

            return new Vector2I((int)a, (int)b);
        }
    }

    public sealed class GodotVector3IConverter : NdjsonConverter<Vector3I>
    {
        private static readonly byte[] N0 = NdjsonGeneratedSupport.EncodeName("x");
        private static readonly byte[] N1 = NdjsonGeneratedSupport.EncodeName("y");
        private static readonly byte[] N2 = NdjsonGeneratedSupport.EncodeName("z");

        public override void Write(ref JsonWriter writer, in Vector3I value, NdjsonOptions options)
        {
            writer.WriteStartObject();
            writer.WritePropertyName(N0);
            writer.WriteNumber((float)value.X, options.NonFiniteHandling);
            writer.WritePropertyName(N1);
            writer.WriteNumber((float)value.Y, options.NonFiniteHandling);
            writer.WritePropertyName(N2);
            writer.WriteNumber((float)value.Z, options.NonFiniteHandling);
            writer.WriteEndObject();
        }

        public override Vector3I Read(ref JsonReader reader, NdjsonOptions options)
        {
            float a = 0f;
            float b = 0f;
            float c = 0f;

            if (reader.TokenType == JsonTokenType.StartArray)
            {
                int index = 0;
                while (reader.ReadNextArrayElement())
                {
                    float component = reader.GetSingle();
                    switch (index)
                    {
                        case 0:
                            a = component;
                            break;
                        case 1:
                            b = component;
                            break;
                        case 2:
                            c = component;
                            break;
                    }

                    index++;
                }

                return new Vector3I((int)a, (int)b, (int)c);
            }

            if (!reader.BeginObject())
            {
                return new Vector3I((int)a, (int)b, (int)c);
            }

            while (reader.ReadNextProperty())
            {
                char key = ComponentKey.Read(ref reader);
                reader.Advance();
                switch (key)
                {
                    case 'x':
                        a = reader.GetSingle();
                        break;
                    case 'y':
                        b = reader.GetSingle();
                        break;
                    case 'z':
                        c = reader.GetSingle();
                        break;
                    default:
                        reader.SkipChildren();
                        break;
                }
            }

            return new Vector3I((int)a, (int)b, (int)c);
        }
    }

    public sealed class GodotQuaternionConverter : NdjsonConverter<Quaternion>
    {
        private static readonly byte[] N0 = NdjsonGeneratedSupport.EncodeName("x");
        private static readonly byte[] N1 = NdjsonGeneratedSupport.EncodeName("y");
        private static readonly byte[] N2 = NdjsonGeneratedSupport.EncodeName("z");
        private static readonly byte[] N3 = NdjsonGeneratedSupport.EncodeName("w");

        public override void Write(ref JsonWriter writer, in Quaternion value, NdjsonOptions options)
        {
            writer.WriteStartObject();
            writer.WritePropertyName(N0);
            writer.WriteNumber((float)value.X, options.NonFiniteHandling);
            writer.WritePropertyName(N1);
            writer.WriteNumber((float)value.Y, options.NonFiniteHandling);
            writer.WritePropertyName(N2);
            writer.WriteNumber((float)value.Z, options.NonFiniteHandling);
            writer.WritePropertyName(N3);
            writer.WriteNumber((float)value.W, options.NonFiniteHandling);
            writer.WriteEndObject();
        }

        public override Quaternion Read(ref JsonReader reader, NdjsonOptions options)
        {
            float a = 0f;
            float b = 0f;
            float c = 0f;
            float d = 0f;

            if (reader.TokenType == JsonTokenType.StartArray)
            {
                int index = 0;
                while (reader.ReadNextArrayElement())
                {
                    float component = reader.GetSingle();
                    switch (index)
                    {
                        case 0:
                            a = component;
                            break;
                        case 1:
                            b = component;
                            break;
                        case 2:
                            c = component;
                            break;
                        case 3:
                            d = component;
                            break;
                    }

                    index++;
                }

                return new Quaternion(a, b, c, d);
            }

            if (!reader.BeginObject())
            {
                return new Quaternion(a, b, c, d);
            }

            while (reader.ReadNextProperty())
            {
                char key = ComponentKey.Read(ref reader);
                reader.Advance();
                switch (key)
                {
                    case 'x':
                        a = reader.GetSingle();
                        break;
                    case 'y':
                        b = reader.GetSingle();
                        break;
                    case 'z':
                        c = reader.GetSingle();
                        break;
                    case 'w':
                        d = reader.GetSingle();
                        break;
                    default:
                        reader.SkipChildren();
                        break;
                }
            }

            return new Quaternion(a, b, c, d);
        }
    }

    public sealed class GodotColorConverter : NdjsonConverter<Color>
    {
        private static readonly byte[] N0 = NdjsonGeneratedSupport.EncodeName("r");
        private static readonly byte[] N1 = NdjsonGeneratedSupport.EncodeName("g");
        private static readonly byte[] N2 = NdjsonGeneratedSupport.EncodeName("b");
        private static readonly byte[] N3 = NdjsonGeneratedSupport.EncodeName("a");

        public override void Write(ref JsonWriter writer, in Color value, NdjsonOptions options)
        {
            writer.WriteStartObject();
            writer.WritePropertyName(N0);
            writer.WriteNumber((float)value.R, options.NonFiniteHandling);
            writer.WritePropertyName(N1);
            writer.WriteNumber((float)value.G, options.NonFiniteHandling);
            writer.WritePropertyName(N2);
            writer.WriteNumber((float)value.B, options.NonFiniteHandling);
            writer.WritePropertyName(N3);
            writer.WriteNumber((float)value.A, options.NonFiniteHandling);
            writer.WriteEndObject();
        }

        public override Color Read(ref JsonReader reader, NdjsonOptions options)
        {
            float a = 0f;
            float b = 0f;
            float c = 0f;
            float d = 1f;

            if (reader.TokenType == JsonTokenType.StartArray)
            {
                int index = 0;
                while (reader.ReadNextArrayElement())
                {
                    float component = reader.GetSingle();
                    switch (index)
                    {
                        case 0:
                            a = component;
                            break;
                        case 1:
                            b = component;
                            break;
                        case 2:
                            c = component;
                            break;
                        case 3:
                            d = component;
                            break;
                    }

                    index++;
                }

                return new Color(a, b, c, d);
            }

            if (!reader.BeginObject())
            {
                return new Color(a, b, c, d);
            }

            while (reader.ReadNextProperty())
            {
                char key = ComponentKey.Read(ref reader);
                reader.Advance();
                switch (key)
                {
                    case 'r':
                        a = reader.GetSingle();
                        break;
                    case 'g':
                        b = reader.GetSingle();
                        break;
                    case 'b':
                        c = reader.GetSingle();
                        break;
                    case 'a':
                        d = reader.GetSingle();
                        break;
                    default:
                        reader.SkipChildren();
                        break;
                }
            }

            return new Color(a, b, c, d);
        }
    }

    public sealed class GodotRect2Converter : NdjsonConverter<Rect2>
    {
        private static readonly byte[] N0 = NdjsonGeneratedSupport.EncodeName("x");
        private static readonly byte[] N1 = NdjsonGeneratedSupport.EncodeName("y");
        private static readonly byte[] N2 = NdjsonGeneratedSupport.EncodeName("w");
        private static readonly byte[] N3 = NdjsonGeneratedSupport.EncodeName("h");

        public override void Write(ref JsonWriter writer, in Rect2 value, NdjsonOptions options)
        {
            writer.WriteStartObject();
            writer.WritePropertyName(N0);
            writer.WriteNumber((float)value.Position.X, options.NonFiniteHandling);
            writer.WritePropertyName(N1);
            writer.WriteNumber((float)value.Position.Y, options.NonFiniteHandling);
            writer.WritePropertyName(N2);
            writer.WriteNumber((float)value.Size.X, options.NonFiniteHandling);
            writer.WritePropertyName(N3);
            writer.WriteNumber((float)value.Size.Y, options.NonFiniteHandling);
            writer.WriteEndObject();
        }

        public override Rect2 Read(ref JsonReader reader, NdjsonOptions options)
        {
            float a = 0f;
            float b = 0f;
            float c = 0f;
            float d = 0f;

            if (reader.TokenType == JsonTokenType.StartArray)
            {
                int index = 0;
                while (reader.ReadNextArrayElement())
                {
                    float component = reader.GetSingle();
                    switch (index)
                    {
                        case 0:
                            a = component;
                            break;
                        case 1:
                            b = component;
                            break;
                        case 2:
                            c = component;
                            break;
                        case 3:
                            d = component;
                            break;
                    }

                    index++;
                }

                return new Rect2(a, b, c, d);
            }

            if (!reader.BeginObject())
            {
                return new Rect2(a, b, c, d);
            }

            while (reader.ReadNextProperty())
            {
                char key = ComponentKey.Read(ref reader);
                reader.Advance();
                switch (key)
                {
                    case 'x':
                        a = reader.GetSingle();
                        break;
                    case 'y':
                        b = reader.GetSingle();
                        break;
                    case 'w':
                        c = reader.GetSingle();
                        break;
                    case 'h':
                        d = reader.GetSingle();
                        break;
                    default:
                        reader.SkipChildren();
                        break;
                }
            }

            return new Rect2(a, b, c, d);
        }
    }

    public sealed class GodotAabbConverter : NdjsonConverter<Aabb>
    {
        private static readonly byte[] N0 = NdjsonGeneratedSupport.EncodeName("x");
        private static readonly byte[] N1 = NdjsonGeneratedSupport.EncodeName("y");
        private static readonly byte[] N2 = NdjsonGeneratedSupport.EncodeName("z");
        private static readonly byte[] N3 = NdjsonGeneratedSupport.EncodeName("w");
        private static readonly byte[] N4 = NdjsonGeneratedSupport.EncodeName("h");
        private static readonly byte[] N5 = NdjsonGeneratedSupport.EncodeName("d");

        public override void Write(ref JsonWriter writer, in Aabb value, NdjsonOptions options)
        {
            writer.WriteStartObject();
            writer.WritePropertyName(N0);
            writer.WriteNumber((float)value.Position.X, options.NonFiniteHandling);
            writer.WritePropertyName(N1);
            writer.WriteNumber((float)value.Position.Y, options.NonFiniteHandling);
            writer.WritePropertyName(N2);
            writer.WriteNumber((float)value.Position.Z, options.NonFiniteHandling);
            writer.WritePropertyName(N3);
            writer.WriteNumber((float)value.Size.X, options.NonFiniteHandling);
            writer.WritePropertyName(N4);
            writer.WriteNumber((float)value.Size.Y, options.NonFiniteHandling);
            writer.WritePropertyName(N5);
            writer.WriteNumber((float)value.Size.Z, options.NonFiniteHandling);
            writer.WriteEndObject();
        }

        public override Aabb Read(ref JsonReader reader, NdjsonOptions options)
        {
            float a = 0f;
            float b = 0f;
            float c = 0f;
            float d = 0f;
            float e = 0f;
            float f = 0f;

            if (reader.TokenType == JsonTokenType.StartArray)
            {
                int index = 0;
                while (reader.ReadNextArrayElement())
                {
                    float component = reader.GetSingle();
                    switch (index)
                    {
                        case 0:
                            a = component;
                            break;
                        case 1:
                            b = component;
                            break;
                        case 2:
                            c = component;
                            break;
                        case 3:
                            d = component;
                            break;
                        case 4:
                            e = component;
                            break;
                        case 5:
                            f = component;
                            break;
                    }

                    index++;
                }

                return new Aabb(new Vector3(a, b, c), new Vector3(d, e, f));
            }

            if (!reader.BeginObject())
            {
                return new Aabb(new Vector3(a, b, c), new Vector3(d, e, f));
            }

            while (reader.ReadNextProperty())
            {
                char key = ComponentKey.Read(ref reader);
                reader.Advance();
                switch (key)
                {
                    case 'x':
                        a = reader.GetSingle();
                        break;
                    case 'y':
                        b = reader.GetSingle();
                        break;
                    case 'z':
                        c = reader.GetSingle();
                        break;
                    case 'w':
                        d = reader.GetSingle();
                        break;
                    case 'h':
                        e = reader.GetSingle();
                        break;
                    case 'd':
                        f = reader.GetSingle();
                        break;
                    default:
                        reader.SkipChildren();
                        break;
                }
            }

            return new Aabb(new Vector3(a, b, c), new Vector3(d, e, f));
        }
    }

}
#endif
