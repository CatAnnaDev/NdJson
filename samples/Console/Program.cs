using System;
using System.Collections.Generic;
using System.IO;
using NdJson;

namespace NdJson.Sample
{
    [NdjsonSerializable(NamingPolicy = NdjsonNamingPolicy.SnakeCaseLower)]
    public sealed class Trade
    {
        [NdjsonProperty("ts")]
        public DateTime Timestamp { get; set; }

        public string Symbol { get; set; }

        public Side Side { get; set; }

        public decimal Price { get; set; }

        public int Quantity { get; set; }

        [NdjsonIgnore(NdjsonIgnoreCondition.WhenWritingNull)]
        public string Venue { get; set; }

        [NdjsonExtensionData]
        public Dictionary<string, NdjsonValue> Extra { get; set; }
    }

    [NdjsonEnumString]
    public enum Side
    {
        Buy,
        Sell
    }

    public static class Program
    {
        public static void Main()
        {
            string path = Path.Combine(Path.GetTempPath(), "ndjson-sample.ndjson");

            List<Trade> trades = new List<Trade>
            {
                new Trade
                {
                    Timestamp = new DateTime(2024, 5, 17, 9, 30, 0, DateTimeKind.Utc),
                    Symbol = "ACME",
                    Side = Side.Buy,
                    Price = 12.34m,
                    Quantity = 100,
                    Venue = "XPAR"
                },
                new Trade
                {
                    Timestamp = new DateTime(2024, 5, 17, 9, 30, 1, DateTimeKind.Utc),
                    Symbol = "ACME",
                    Side = Side.Sell,
                    Price = 12.36m,
                    Quantity = 40
                }
            };

            Console.WriteLine("--- une ligne ---");
            Console.WriteLine(NdjsonSerializer.Serialize(trades[0]));

            Console.WriteLine();
            Console.WriteLine("--- fichier ndjson ---");
            NdjsonFile.WriteAll(path, trades);
            NdjsonFile.Append(path, new Trade
            {
                Timestamp = new DateTime(2024, 5, 17, 9, 30, 2, DateTimeKind.Utc),
                Symbol = "GLOB",
                Side = Side.Buy,
                Price = 7.5m,
                Quantity = 10
            });
            Console.Write(File.ReadAllText(path));

            Console.WriteLine();
            Console.WriteLine("--- lecture en streaming ---");
            decimal notional = 0m;
            foreach (Trade trade in NdjsonFile.ReadLines<Trade>(path))
            {
                notional += trade.Price * trade.Quantity;
                Console.WriteLine(trade.Symbol + " " + trade.Side + " " + trade.Quantity + " @ " + trade.Price);
            }

            Console.WriteLine("notionnel total : " + notional);

            Console.WriteLine();
            Console.WriteLine("--- champs inconnus captures ---");
            Trade withExtra = NdjsonSerializer.Deserialize<Trade>(
                "{\"ts\":\"2024-05-17T09:31:00Z\",\"symbol\":\"ACME\",\"side\":\"Buy\",\"price\":1,\"quantity\":1,\"broker\":\"XYZ\",\"tags\":[1,2]}");
            Console.WriteLine("broker = " + withExtra.Extra["broker"].GetString());
            Console.WriteLine("tags[1] = " + withExtra.Extra["tags"][1].GetInt32());
            Console.WriteLine("reecriture : " + NdjsonSerializer.Serialize(withExtra));

            Console.WriteLine();
            Console.WriteLine("--- lignes abimees tolerees ---");
            NdjsonOptions tolerant = new NdjsonOptions
            {
                SkipMalformedLines = true,
                MalformedLineHandler = error => Console.WriteLine("ligne " + error.LineNumber + " ignoree : " + error.Error.Message)
            };

            string mixed = "{\"symbol\":\"A\",\"quantity\":1}\ncoupee au milieu...\n{\"symbol\":\"B\",\"quantity\":2}\n";
            foreach (Trade trade in NdjsonSerializer.DeserializeLines<Trade>(mixed, tolerant))
            {
                Console.WriteLine("recupere : " + trade.Symbol);
            }

            File.Delete(path);
        }
    }
}
