using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using NdJson.Serialization;

namespace NdJson
{
    public static class NdjsonSerializer
    {
        public static string Serialize<T>(T value)
        {
            return Serialize(value, null);
        }

        public static string Serialize<T>(T value, NdjsonOptions options)
        {
            NdjsonOptions effective = options ?? NdjsonOptions.Default;
            JsonWriter writer = JsonWriter.Create(256);
            try
            {
                effective.GetConverter<T>().Write(ref writer, in value, effective);
                return JsonEscaping.GetString(writer.WrittenSpan);
            }
            finally
            {
                writer.Release();
            }
        }

        public static byte[] SerializeToUtf8Bytes<T>(T value)
        {
            return SerializeToUtf8Bytes(value, null);
        }

        public static byte[] SerializeToUtf8Bytes<T>(T value, NdjsonOptions options)
        {
            NdjsonOptions effective = options ?? NdjsonOptions.Default;
            JsonWriter writer = JsonWriter.Create(256);
            try
            {
                effective.GetConverter<T>().Write(ref writer, in value, effective);
                return writer.ToArray();
            }
            finally
            {
                writer.Release();
            }
        }

        public static T Deserialize<T>(string json)
        {
            return Deserialize<T>(json, null);
        }

        public static T Deserialize<T>(string json, NdjsonOptions options)
        {
            if (json == null)
            {
                throw new ArgumentNullException(nameof(json));
            }

            return Deserialize<T>(new ReadOnlySpan<byte>(Encoding.UTF8.GetBytes(json)), options);
        }

        public static T Deserialize<T>(ReadOnlySpan<byte> utf8Json)
        {
            return Deserialize<T>(utf8Json, null);
        }

        public static T Deserialize<T>(ReadOnlySpan<byte> utf8Json, NdjsonOptions options)
        {
            NdjsonOptions effective = options ?? NdjsonOptions.Default;
            JsonReader reader = new JsonReader(utf8Json, effective.MaxDepth);
            reader.Advance();
            return effective.GetConverter<T>().Read(ref reader, effective);
        }

        public static void SerializeLines<T>(Stream stream, IEnumerable<T> values)
        {
            SerializeLines(stream, values, null, true);
        }

        public static void SerializeLines<T>(Stream stream, IEnumerable<T> values, NdjsonOptions options)
        {
            SerializeLines(stream, values, options, true);
        }

        public static void SerializeLines<T>(Stream stream, IEnumerable<T> values, NdjsonOptions options, bool leaveOpen)
        {
            if (values == null)
            {
                throw new ArgumentNullException(nameof(values));
            }

            using (NdjsonWriter writer = new NdjsonWriter(stream, options, leaveOpen))
            {
                foreach (T value in values)
                {
                    writer.Write(value);
                }
            }
        }

        public static string SerializeLines<T>(IEnumerable<T> values)
        {
            return SerializeLines(values, null);
        }

        public static string SerializeLines<T>(IEnumerable<T> values, NdjsonOptions options)
        {
            return Encoding.UTF8.GetString(SerializeLinesToUtf8Bytes(values, options));
        }

        public static byte[] SerializeLinesToUtf8Bytes<T>(IEnumerable<T> values)
        {
            return SerializeLinesToUtf8Bytes(values, null);
        }

        public static byte[] SerializeLinesToUtf8Bytes<T>(IEnumerable<T> values, NdjsonOptions options)
        {
            if (values == null)
            {
                throw new ArgumentNullException(nameof(values));
            }

            using (MemoryStream stream = new MemoryStream())
            {
                SerializeLines(stream, values, options, true);
                return stream.ToArray();
            }
        }

        public static IEnumerable<T> DeserializeLines<T>(Stream stream)
        {
            return DeserializeLines<T>(stream, null, false);
        }

        public static IEnumerable<T> DeserializeLines<T>(Stream stream, NdjsonOptions options)
        {
            return DeserializeLines<T>(stream, options, false);
        }

        public static IEnumerable<T> DeserializeLines<T>(Stream stream, NdjsonOptions options, bool leaveOpen)
        {
            if (stream == null)
            {
                throw new ArgumentNullException(nameof(stream));
            }

            return DeserializeLinesIterator<T>(stream, options, leaveOpen);
        }

        private static IEnumerable<T> DeserializeLinesIterator<T>(Stream stream, NdjsonOptions options, bool leaveOpen)
        {
            using (NdjsonReader reader = new NdjsonReader(stream, options, leaveOpen))
            {
                while (true)
                {
                    T value;
                    if (!reader.TryRead(out value))
                    {
                        yield break;
                    }

                    yield return value;
                }
            }
        }

        public static IEnumerable<T> DeserializeLines<T>(string ndjson)
        {
            return DeserializeLines<T>(ndjson, null);
        }

        public static IEnumerable<T> DeserializeLines<T>(string ndjson, NdjsonOptions options)
        {
            if (ndjson == null)
            {
                throw new ArgumentNullException(nameof(ndjson));
            }

            return DeserializeLines<T>(Encoding.UTF8.GetBytes(ndjson), options);
        }

        public static IEnumerable<T> DeserializeLines<T>(byte[] utf8Ndjson)
        {
            return DeserializeLines<T>(utf8Ndjson, null);
        }

        public static IEnumerable<T> DeserializeLines<T>(byte[] utf8Ndjson, NdjsonOptions options)
        {
            if (utf8Ndjson == null)
            {
                throw new ArgumentNullException(nameof(utf8Ndjson));
            }

            return DeserializeBufferIterator<T>(utf8Ndjson, options ?? NdjsonOptions.Default);
        }

        private static IEnumerable<T> DeserializeBufferIterator<T>(byte[] data, NdjsonOptions options)
        {
            NdjsonConverter<T> converter = options.GetConverter<T>();
            int position = 0;
            long lineNumber = 0;

            if (data.Length >= 3 && data[0] == 0xEF && data[1] == 0xBB && data[2] == 0xBF)
            {
                position = 3;
            }

            while (position <= data.Length)
            {
                if (position == data.Length)
                {
                    yield break;
                }

                int relative = IndexOfNewLine(data, position);
                int length = relative < 0 ? data.Length - position : relative;
                int offset = position;
                position += length + (relative < 0 ? 0 : 1);
                lineNumber++;

                if (length > 0 && data[offset + length - 1] == JsonConstants.CarriageReturn)
                {
                    length--;
                }

                T value;
                if (NdjsonLineParser.TryParse(data, offset, length, lineNumber, converter, options, out value))
                {
                    yield return value;
                }

                if (relative < 0)
                {
                    yield break;
                }
            }
        }

        private static int IndexOfNewLine(byte[] data, int start)
        {
            return new ReadOnlySpan<byte>(data, start, data.Length - start).IndexOf(JsonConstants.LineFeed);
        }

        public static List<T> DeserializeLinesToList<T>(string ndjson)
        {
            return new List<T>(DeserializeLines<T>(ndjson, null));
        }

        public static List<T> DeserializeLinesToList<T>(string ndjson, NdjsonOptions options)
        {
            return new List<T>(DeserializeLines<T>(ndjson, options));
        }

        public static async Task SerializeLinesAsync<T>(Stream stream, IEnumerable<T> values, NdjsonOptions options, bool leaveOpen, CancellationToken cancellationToken)
        {
            if (values == null)
            {
                throw new ArgumentNullException(nameof(values));
            }

            NdjsonWriter writer = new NdjsonWriter(stream, options, true);
            try
            {
                foreach (T value in values)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    writer.Write(value);
                }

                await writer.FlushAsync(cancellationToken).ConfigureAwait(false);
            }
            finally
            {
                writer.Dispose();
                if (!leaveOpen)
                {
                    stream.Dispose();
                }
            }
        }

        public static Task SerializeLinesAsync<T>(Stream stream, IEnumerable<T> values)
        {
            return SerializeLinesAsync(stream, values, null, true, CancellationToken.None);
        }

        public static Task SerializeLinesAsync<T>(Stream stream, IEnumerable<T> values, NdjsonOptions options)
        {
            return SerializeLinesAsync(stream, values, options, true, CancellationToken.None);
        }
    }
}
