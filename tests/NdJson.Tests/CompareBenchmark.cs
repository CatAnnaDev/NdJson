using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using NdJson;

namespace NdJson.Tests
{
    public sealed class StjLogLine
    {
        [JsonPropertyName("timestamp")]
        public DateTime Timestamp { get; set; }

        [JsonPropertyName("level")]
        public string Level { get; set; }

        [JsonPropertyName("message")]
        public string Message { get; set; }

        [JsonPropertyName("code")]
        public int Code { get; set; }
    }

    [JsonSerializable(typeof(StjLogLine))]
    internal sealed partial class StjContext : JsonSerializerContext
    {
    }

    public static class CompareBenchmark
    {
        private static readonly byte[] NewLine = new byte[] { (byte)'\n' };

        public static void Run(int count)
        {
            List<LogLine> mine = new List<LogLine>(count);
            List<StjLogLine> theirs = new List<StjLogLine>(count);
            DateTime start = new DateTime(2024, 5, 17, 0, 0, 0, DateTimeKind.Utc);

            for (int i = 0; i < count; i++)
            {
                DateTime stamp = start.AddSeconds(i);
                string level = (i % 3) == 0 ? "info" : "warn";
                string message = "evenement numero " + i + " sur le service de test";
                mine.Add(new LogLine { Timestamp = stamp, Level = level, Message = message, Code = i });
                theirs.Add(new StjLogLine { Timestamp = stamp, Level = level, Message = message, Code = i });
            }

            Console.WriteLine("Comparaison sur " + count.ToString("N0") + " lignes NDJSON");
            Console.WriteLine();

            byte[] ndjsonPayload = null;
            byte[] stjPayload = null;

            for (int round = 0; round < 8; round++)
            {
                bool report = round >= 2;

                Measure("NdJson   ecriture", report, () =>
                {
                    using (MemoryStream stream = new MemoryStream(1 << 25))
                    {
                        NdjsonSerializer.SerializeLines(stream, mine, null, true);
                        ndjsonPayload = stream.GetBuffer();
                        return stream.Length;
                    }
                });

                Measure("STJ      ecriture", report, () =>
                {
                    using (MemoryStream stream = new MemoryStream(1 << 25))
                    {
                        using (Utf8JsonWriter writer = new Utf8JsonWriter(stream, new JsonWriterOptions { SkipValidation = true }))
                        {
                            for (int i = 0; i < theirs.Count; i++)
                            {
                                JsonSerializer.Serialize(writer, theirs[i], typeof(StjLogLine), StjContext.Default);
                                writer.Flush();
                                stream.WriteByte(NewLine[0]);
                                writer.Reset(stream);
                            }
                        }

                        stjPayload = stream.ToArray();
                        return stream.Length;
                    }
                });
            }

            Flush();

            byte[] payload = NdjsonSerializer.SerializeLinesToUtf8Bytes(mine);
            Console.WriteLine();
            Console.WriteLine("Taille NdJson : " + payload.Length.ToString("N0") + " octets, taille STJ : " + stjPayload.Length.ToString("N0") + " octets");
            Console.WriteLine();

            for (int round = 0; round < 8; round++)
            {
                bool report = round >= 2;

                Measure("NdJson   lecture ", report, () =>
                {
                    long sum = 0;
                    using (MemoryStream stream = new MemoryStream(payload))
                    {
                        foreach (LogLine line in NdjsonSerializer.DeserializeLines<LogLine>(stream, null, true))
                        {
                            sum += line.Code + line.Message.Length;
                        }
                    }

                    return sum;
                });

                Measure("STJ      lecture ", report, () =>
                {
                    long sum = 0;
                    int position = 0;
                    while (position < payload.Length)
                    {
                        int index = Array.IndexOf(payload, (byte)'\n', position);
                        if (index < 0)
                        {
                            index = payload.Length;
                        }

                        ReadOnlySpan<byte> line = new ReadOnlySpan<byte>(payload, position, index - position);
                        if (line.Length > 0)
                        {
                            StjLogLine value = (StjLogLine)JsonSerializer.Deserialize(line, typeof(StjLogLine), StjContext.Default);
                            sum += value.Code + value.Message.Length;
                        }

                        position = index + 1;
                    }

                    return sum;
                });
            }

            Flush();
            GC.KeepAlive(ndjsonPayload);
        }

        private static readonly Dictionary<string, double> Best = new Dictionary<string, double>();
        private static readonly Dictionary<string, long> Allocations = new Dictionary<string, long>();
        private static readonly List<string> Order = new List<string>();

        private static void Measure(string label, bool report, Func<long> action)
        {
            GC.Collect();
            GC.WaitForPendingFinalizers();
            long before = GC.GetTotalAllocatedBytes(true);
            Stopwatch watch = Stopwatch.StartNew();
            long result = action();
            watch.Stop();
            long allocated = GC.GetTotalAllocatedBytes(true) - before;

            if (report)
            {
                double elapsed = watch.Elapsed.TotalMilliseconds;
                double previous;
                if (!Best.TryGetValue(label, out previous))
                {
                    Order.Add(label);
                    previous = double.MaxValue;
                }

                if (elapsed < previous)
                {
                    Best[label] = elapsed;
                }

                Allocations[label] = allocated;
            }

            GC.KeepAlive(result);
        }

        private static void Flush()
        {
            foreach (string label in Order)
            {
                Console.WriteLine(
                    label + " : " + Best[label].ToString("F1").PadLeft(7) + " ms   " +
                    (Allocations[label] / 1024.0 / 1024.0).ToString("F1").PadLeft(7) + " Mo alloues");
            }

            Order.Clear();
            Best.Clear();
            Allocations.Clear();
        }
    }
}
