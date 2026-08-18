using System;
using System.Collections.Generic;
using System.IO;

namespace NdJson
{
    public static class NdjsonFile
    {
        public static void WriteAll<T>(string path, IEnumerable<T> values)
        {
            WriteAll(path, values, null, false);
        }

        public static void WriteAll<T>(string path, IEnumerable<T> values, NdjsonOptions options)
        {
            WriteAll(path, values, options, false);
        }

        public static void WriteAll<T>(string path, IEnumerable<T> values, NdjsonOptions options, bool append)
        {
            if (path == null)
            {
                throw new ArgumentNullException(nameof(path));
            }

            NdjsonOptions effective = options ?? NdjsonOptions.Default;
            using (FileStream stream = new FileStream(path, append ? FileMode.Append : FileMode.Create, FileAccess.Write, FileShare.Read, effective.BufferSize, FileOptions.SequentialScan))
            {
                NdjsonSerializer.SerializeLines(stream, values, effective, true);
            }
        }

        public static void AppendAll<T>(string path, IEnumerable<T> values)
        {
            WriteAll(path, values, null, true);
        }

        public static void AppendAll<T>(string path, IEnumerable<T> values, NdjsonOptions options)
        {
            WriteAll(path, values, options, true);
        }

        public static void Append<T>(string path, T value)
        {
            Append(path, value, null);
        }

        public static void Append<T>(string path, T value, NdjsonOptions options)
        {
            NdjsonOptions effective = options ?? NdjsonOptions.Default;
            using (FileStream stream = new FileStream(path, FileMode.Append, FileAccess.Write, FileShare.Read, 4096, FileOptions.SequentialScan))
            using (NdjsonWriter writer = new NdjsonWriter(stream, effective, true))
            {
                writer.Write(value);
            }
        }

        public static IEnumerable<T> ReadLines<T>(string path)
        {
            return ReadLines<T>(path, null);
        }

        public static IEnumerable<T> ReadLines<T>(string path, NdjsonOptions options)
        {
            if (path == null)
            {
                throw new ArgumentNullException(nameof(path));
            }

            NdjsonOptions effective = options ?? NdjsonOptions.Default;
            FileStream stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite, effective.BufferSize, FileOptions.SequentialScan);
            return NdjsonSerializer.DeserializeLines<T>(stream, effective, false);
        }

        public static List<T> ReadAll<T>(string path)
        {
            return new List<T>(ReadLines<T>(path, null));
        }

        public static List<T> ReadAll<T>(string path, NdjsonOptions options)
        {
            return new List<T>(ReadLines<T>(path, options));
        }
    }
}
