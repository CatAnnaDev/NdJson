#if UNITY_2019_1_OR_NEWER
using System;
using System.Collections.Generic;
using System.IO;
using NdJson.Serialization;
using UnityEngine;

namespace NdJson.Unity
{
    public static class NdjsonUnity
    {
        private static bool _registered;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        public static void Register()
        {
            if (_registered)
            {
                return;
            }

            _registered = true;

            NdjsonConverterRegistry.Register(new Vector2Converter());
            NdjsonConverterRegistry.Register(new Vector3Converter());
            NdjsonConverterRegistry.Register(new Vector4Converter());
            NdjsonConverterRegistry.Register(new Vector2IntConverter());
            NdjsonConverterRegistry.Register(new Vector3IntConverter());
            NdjsonConverterRegistry.Register(new QuaternionConverter());
            NdjsonConverterRegistry.Register(new ColorConverter());
            NdjsonConverterRegistry.Register(new Color32Converter());
            NdjsonConverterRegistry.Register(new RectConverter());
            NdjsonConverterRegistry.Register(new BoundsConverter());
        }

        public static string PersistentPath(string fileName)
        {
            return Path.Combine(Application.persistentDataPath, fileName);
        }

        public static void SaveToPersistent<T>(string fileName, IEnumerable<T> values, NdjsonOptions options = null)
        {
            Register();
            NdjsonFile.WriteAll(PersistentPath(fileName), values, options, false);
        }

        public static void AppendToPersistent<T>(string fileName, T value, NdjsonOptions options = null)
        {
            Register();
            NdjsonFile.Append(PersistentPath(fileName), value, options);
        }

        public static List<T> LoadFromPersistent<T>(string fileName, NdjsonOptions options = null)
        {
            Register();
            string path = PersistentPath(fileName);
            if (!File.Exists(path))
            {
                return new List<T>();
            }

            return NdjsonFile.ReadAll<T>(path, options);
        }

        public static IEnumerable<T> ReadTextAsset<T>(TextAsset asset, NdjsonOptions options = null)
        {
            Register();
            if (asset == null)
            {
                return new List<T>();
            }

            return NdjsonSerializer.DeserializeLines<T>(asset.bytes, options);
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

    public sealed class Vector2Converter : NdjsonConverter<Vector2>
    {
        private static readonly byte[] N0 = NdjsonGeneratedSupport.EncodeName("x");
        private static readonly byte[] N1 = NdjsonGeneratedSupport.EncodeName("y");

        public override void Write(ref JsonWriter writer, in Vector2 value, NdjsonOptions options)
        {
            writer.WriteStartObject();
            writer.WritePropertyName(N0);
            writer.WriteNumber((float)value.x, options.NonFiniteHandling);
            writer.WritePropertyName(N1);
            writer.WriteNumber((float)value.y, options.NonFiniteHandling);
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

    public sealed class Vector3Converter : NdjsonConverter<Vector3>
    {
        private static readonly byte[] N0 = NdjsonGeneratedSupport.EncodeName("x");
        private static readonly byte[] N1 = NdjsonGeneratedSupport.EncodeName("y");
        private static readonly byte[] N2 = NdjsonGeneratedSupport.EncodeName("z");

        public override void Write(ref JsonWriter writer, in Vector3 value, NdjsonOptions options)
        {
            writer.WriteStartObject();
            writer.WritePropertyName(N0);
            writer.WriteNumber((float)value.x, options.NonFiniteHandling);
            writer.WritePropertyName(N1);
            writer.WriteNumber((float)value.y, options.NonFiniteHandling);
            writer.WritePropertyName(N2);
            writer.WriteNumber((float)value.z, options.NonFiniteHandling);
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

    public sealed class Vector4Converter : NdjsonConverter<Vector4>
    {
        private static readonly byte[] N0 = NdjsonGeneratedSupport.EncodeName("x");
        private static readonly byte[] N1 = NdjsonGeneratedSupport.EncodeName("y");
        private static readonly byte[] N2 = NdjsonGeneratedSupport.EncodeName("z");
        private static readonly byte[] N3 = NdjsonGeneratedSupport.EncodeName("w");

        public override void Write(ref JsonWriter writer, in Vector4 value, NdjsonOptions options)
        {
            writer.WriteStartObject();
            writer.WritePropertyName(N0);
            writer.WriteNumber((float)value.x, options.NonFiniteHandling);
            writer.WritePropertyName(N1);
            writer.WriteNumber((float)value.y, options.NonFiniteHandling);
            writer.WritePropertyName(N2);
            writer.WriteNumber((float)value.z, options.NonFiniteHandling);
            writer.WritePropertyName(N3);
            writer.WriteNumber((float)value.w, options.NonFiniteHandling);
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

    public sealed class Vector2IntConverter : NdjsonConverter<Vector2Int>
    {
        private static readonly byte[] N0 = NdjsonGeneratedSupport.EncodeName("x");
        private static readonly byte[] N1 = NdjsonGeneratedSupport.EncodeName("y");

        public override void Write(ref JsonWriter writer, in Vector2Int value, NdjsonOptions options)
        {
            writer.WriteStartObject();
            writer.WritePropertyName(N0);
            writer.WriteNumber((float)value.x, options.NonFiniteHandling);
            writer.WritePropertyName(N1);
            writer.WriteNumber((float)value.y, options.NonFiniteHandling);
            writer.WriteEndObject();
        }

        public override Vector2Int Read(ref JsonReader reader, NdjsonOptions options)
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

                return new Vector2Int((int)a, (int)b);
            }

            if (!reader.BeginObject())
            {
                return new Vector2Int((int)a, (int)b);
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

            return new Vector2Int((int)a, (int)b);
        }
    }

    public sealed class Vector3IntConverter : NdjsonConverter<Vector3Int>
    {
        private static readonly byte[] N0 = NdjsonGeneratedSupport.EncodeName("x");
        private static readonly byte[] N1 = NdjsonGeneratedSupport.EncodeName("y");
        private static readonly byte[] N2 = NdjsonGeneratedSupport.EncodeName("z");

        public override void Write(ref JsonWriter writer, in Vector3Int value, NdjsonOptions options)
        {
            writer.WriteStartObject();
            writer.WritePropertyName(N0);
            writer.WriteNumber((float)value.x, options.NonFiniteHandling);
            writer.WritePropertyName(N1);
            writer.WriteNumber((float)value.y, options.NonFiniteHandling);
            writer.WritePropertyName(N2);
            writer.WriteNumber((float)value.z, options.NonFiniteHandling);
            writer.WriteEndObject();
        }

        public override Vector3Int Read(ref JsonReader reader, NdjsonOptions options)
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

                return new Vector3Int((int)a, (int)b, (int)c);
            }

            if (!reader.BeginObject())
            {
                return new Vector3Int((int)a, (int)b, (int)c);
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

            return new Vector3Int((int)a, (int)b, (int)c);
        }
    }

    public sealed class QuaternionConverter : NdjsonConverter<Quaternion>
    {
        private static readonly byte[] N0 = NdjsonGeneratedSupport.EncodeName("x");
        private static readonly byte[] N1 = NdjsonGeneratedSupport.EncodeName("y");
        private static readonly byte[] N2 = NdjsonGeneratedSupport.EncodeName("z");
        private static readonly byte[] N3 = NdjsonGeneratedSupport.EncodeName("w");

        public override void Write(ref JsonWriter writer, in Quaternion value, NdjsonOptions options)
        {
            writer.WriteStartObject();
            writer.WritePropertyName(N0);
            writer.WriteNumber((float)value.x, options.NonFiniteHandling);
            writer.WritePropertyName(N1);
            writer.WriteNumber((float)value.y, options.NonFiniteHandling);
            writer.WritePropertyName(N2);
            writer.WriteNumber((float)value.z, options.NonFiniteHandling);
            writer.WritePropertyName(N3);
            writer.WriteNumber((float)value.w, options.NonFiniteHandling);
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

    public sealed class ColorConverter : NdjsonConverter<Color>
    {
        private static readonly byte[] N0 = NdjsonGeneratedSupport.EncodeName("r");
        private static readonly byte[] N1 = NdjsonGeneratedSupport.EncodeName("g");
        private static readonly byte[] N2 = NdjsonGeneratedSupport.EncodeName("b");
        private static readonly byte[] N3 = NdjsonGeneratedSupport.EncodeName("a");

        public override void Write(ref JsonWriter writer, in Color value, NdjsonOptions options)
        {
            writer.WriteStartObject();
            writer.WritePropertyName(N0);
            writer.WriteNumber((float)value.r, options.NonFiniteHandling);
            writer.WritePropertyName(N1);
            writer.WriteNumber((float)value.g, options.NonFiniteHandling);
            writer.WritePropertyName(N2);
            writer.WriteNumber((float)value.b, options.NonFiniteHandling);
            writer.WritePropertyName(N3);
            writer.WriteNumber((float)value.a, options.NonFiniteHandling);
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

    public sealed class Color32Converter : NdjsonConverter<Color32>
    {
        private static readonly byte[] N0 = NdjsonGeneratedSupport.EncodeName("r");
        private static readonly byte[] N1 = NdjsonGeneratedSupport.EncodeName("g");
        private static readonly byte[] N2 = NdjsonGeneratedSupport.EncodeName("b");
        private static readonly byte[] N3 = NdjsonGeneratedSupport.EncodeName("a");

        public override void Write(ref JsonWriter writer, in Color32 value, NdjsonOptions options)
        {
            writer.WriteStartObject();
            writer.WritePropertyName(N0);
            writer.WriteNumber((float)value.r, options.NonFiniteHandling);
            writer.WritePropertyName(N1);
            writer.WriteNumber((float)value.g, options.NonFiniteHandling);
            writer.WritePropertyName(N2);
            writer.WriteNumber((float)value.b, options.NonFiniteHandling);
            writer.WritePropertyName(N3);
            writer.WriteNumber((float)value.a, options.NonFiniteHandling);
            writer.WriteEndObject();
        }

        public override Color32 Read(ref JsonReader reader, NdjsonOptions options)
        {
            float a = 0f;
            float b = 0f;
            float c = 0f;
            float d = 255f;

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

                return new Color32((byte)a, (byte)b, (byte)c, (byte)d);
            }

            if (!reader.BeginObject())
            {
                return new Color32((byte)a, (byte)b, (byte)c, (byte)d);
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

            return new Color32((byte)a, (byte)b, (byte)c, (byte)d);
        }
    }

    public sealed class RectConverter : NdjsonConverter<Rect>
    {
        private static readonly byte[] N0 = NdjsonGeneratedSupport.EncodeName("x");
        private static readonly byte[] N1 = NdjsonGeneratedSupport.EncodeName("y");
        private static readonly byte[] N2 = NdjsonGeneratedSupport.EncodeName("w");
        private static readonly byte[] N3 = NdjsonGeneratedSupport.EncodeName("h");

        public override void Write(ref JsonWriter writer, in Rect value, NdjsonOptions options)
        {
            writer.WriteStartObject();
            writer.WritePropertyName(N0);
            writer.WriteNumber((float)value.x, options.NonFiniteHandling);
            writer.WritePropertyName(N1);
            writer.WriteNumber((float)value.y, options.NonFiniteHandling);
            writer.WritePropertyName(N2);
            writer.WriteNumber((float)value.width, options.NonFiniteHandling);
            writer.WritePropertyName(N3);
            writer.WriteNumber((float)value.height, options.NonFiniteHandling);
            writer.WriteEndObject();
        }

        public override Rect Read(ref JsonReader reader, NdjsonOptions options)
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

                return new Rect(a, b, c, d);
            }

            if (!reader.BeginObject())
            {
                return new Rect(a, b, c, d);
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

            return new Rect(a, b, c, d);
        }
    }

    public sealed class BoundsConverter : NdjsonConverter<Bounds>
    {
        private static readonly byte[] N0 = NdjsonGeneratedSupport.EncodeName("x");
        private static readonly byte[] N1 = NdjsonGeneratedSupport.EncodeName("y");
        private static readonly byte[] N2 = NdjsonGeneratedSupport.EncodeName("z");
        private static readonly byte[] N3 = NdjsonGeneratedSupport.EncodeName("w");
        private static readonly byte[] N4 = NdjsonGeneratedSupport.EncodeName("h");
        private static readonly byte[] N5 = NdjsonGeneratedSupport.EncodeName("d");

        public override void Write(ref JsonWriter writer, in Bounds value, NdjsonOptions options)
        {
            writer.WriteStartObject();
            writer.WritePropertyName(N0);
            writer.WriteNumber((float)value.center.x, options.NonFiniteHandling);
            writer.WritePropertyName(N1);
            writer.WriteNumber((float)value.center.y, options.NonFiniteHandling);
            writer.WritePropertyName(N2);
            writer.WriteNumber((float)value.center.z, options.NonFiniteHandling);
            writer.WritePropertyName(N3);
            writer.WriteNumber((float)value.size.x, options.NonFiniteHandling);
            writer.WritePropertyName(N4);
            writer.WriteNumber((float)value.size.y, options.NonFiniteHandling);
            writer.WritePropertyName(N5);
            writer.WriteNumber((float)value.size.z, options.NonFiniteHandling);
            writer.WriteEndObject();
        }

        public override Bounds Read(ref JsonReader reader, NdjsonOptions options)
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

                return new Bounds(new Vector3(a, b, c), new Vector3(d, e, f));
            }

            if (!reader.BeginObject())
            {
                return new Bounds(new Vector3(a, b, c), new Vector3(d, e, f));
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

            return new Bounds(new Vector3(a, b, c), new Vector3(d, e, f));
        }
    }

}
#endif
