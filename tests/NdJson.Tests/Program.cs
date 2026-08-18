using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using NdJson;

namespace NdJson.Tests
{
    public static class Program
    {
        public static int Main(string[] args)
        {
            if (args.Length > 0 && args[0] == "bench")
            {
                Benchmark();
                return 0;
            }

            if (args.Length > 0 && args[0] == "compare")
            {
                CompareBenchmark.Run(args.Length > 1 ? int.Parse(args[1]) : 200000);
                return 0;
            }

            JsonLowLevelTests.RunAll();
            GeneratedTests.RunAll();
            ReflectionAndDomTests.RunAll();
            StreamTests.RunAll();

            return Check.Summary();
        }

        private static void Benchmark()
        {
            Console.WriteLine("Debit brut, meilleur temps sur 8 executions (les premieres sont ralenties par la compilation JIT par paliers).");
            Console.WriteLine();

            const int Count = 200000;
            List<LogLine> lines = new List<LogLine>(Count);
            DateTime start = new DateTime(2024, 5, 17, 0, 0, 0, DateTimeKind.Utc);
            for (int i = 0; i < Count; i++)
            {
                lines.Add(new LogLine
                {
                    Timestamp = start.AddSeconds(i),
                    Level = (i % 3) == 0 ? "info" : "warn",
                    Message = "evenement numero " + i + " sur le service de test",
                    Code = i
                });
            }

            byte[] payload = null;
            for (int round = 0; round < 8; round++)
            {
                GC.Collect();
                GC.WaitForPendingFinalizers();
                long allocationsBefore = GC.GetTotalAllocatedBytes(true);
                Stopwatch watch = Stopwatch.StartNew();
                using (MemoryStream stream = new MemoryStream(1 << 24))
                {
                    NdjsonSerializer.SerializeLines(stream, lines, null, true);
                    payload = stream.ToArray();
                }

                watch.Stop();
                long allocated = GC.GetTotalAllocatedBytes(true) - allocationsBefore;
                Report("ecriture", Count, payload.Length, watch, allocated);
            }

            for (int round = 0; round < 8; round++)
            {
                GC.Collect();
                GC.WaitForPendingFinalizers();
                long allocationsBefore = GC.GetTotalAllocatedBytes(true);
                Stopwatch watch = Stopwatch.StartNew();
                long checksum = 0;
                using (MemoryStream stream = new MemoryStream(payload))
                {
                    foreach (LogLine line in NdjsonSerializer.DeserializeLines<LogLine>(stream, null, true))
                    {
                        checksum += line.Code + line.Message.Length;
                    }
                }

                watch.Stop();
                long allocated = GC.GetTotalAllocatedBytes(true) - allocationsBefore;
                Report("lecture", Count, payload.Length, watch, allocated);
                GC.KeepAlive(checksum);
            }
        }

        private static readonly System.Collections.Generic.Dictionary<string, double> BestSeconds =
            new System.Collections.Generic.Dictionary<string, double>();

        private static void Report(string label, int count, int bytes, Stopwatch watch, long allocated)
        {
            double seconds = watch.Elapsed.TotalSeconds;
            double previous;
            if (BestSeconds.TryGetValue(label, out previous) && previous <= seconds)
            {
                return;
            }

            BestSeconds[label] = seconds;
            Console.WriteLine(
                label +
                " : " + count.ToString("N0") + " lignes en " + (seconds * 1000.0).ToString("F1") + " ms" +
                " (" + (count / seconds / 1000000.0).ToString("F2") + " M lignes/s, " +
                (bytes / seconds / (1024.0 * 1024.0)).ToString("F0") + " Mo/s, " +
                (allocated / 1024.0 / 1024.0).ToString("F1") + " Mo alloues)");
        }
    }
}
