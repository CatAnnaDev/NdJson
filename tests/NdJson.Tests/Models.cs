using System;
using System.Collections.Generic;
using NdJson;
using NdJson.Serialization;

[assembly: NdjsonSerializable(typeof(NdJson.Tests.PlainPoco))]

namespace NdJson.Tests
{
    public sealed class PlainPoco
    {
        public int A { get; set; }

        public string B { get; set; }
    }

    [NdjsonSerializable]
    public readonly struct Money
    {
        public Money(string currency, decimal amount)
        {
            Currency = currency;
            Amount = amount;
        }

        public string Currency { get; }

        public decimal Amount { get; }
    }

    [NdjsonSerializable]
    public sealed class WithComputed
    {
        public WithComputed()
        {
        }

        public WithComputed(int value)
        {
            Value = value;
        }

        public int Value { get; set; }

        public string Label
        {
            get { return "v" + Value; }
        }
    }

    [NdjsonSerializable(NamingPolicy = NdjsonNamingPolicy.CamelCase)]
    public sealed class Player
    {
        public int Id { get; set; }

        public string Name { get; set; }

        public float Health { get; set; }

        public bool Alive { get; set; }

        public List<string> Tags { get; set; }

        public Vector3Data Position { get; set; }
    }

    [NdjsonSerializable]
    public struct Vector3Data
    {
        public float X { get; set; }

        public float Y { get; set; }

        public float Z { get; set; }
    }

    public enum Rarity
    {
        Common = 0,
        Rare = 1,
        Epic = 2,
        Legendary = 3
    }

    [NdjsonEnumString(NamingPolicy = NdjsonNamingPolicy.SnakeCaseLower)]
    public enum SpawnState
    {
        NotSpawned = 0,
        SpawningNow = 1,
        [NdjsonEnumMember("done")]
        FullySpawned = 2
    }

    [Flags]
    public enum Permissions
    {
        None = 0,
        Read = 1,
        Write = 2,
        Execute = 4
    }

    [NdjsonSerializable(NamingPolicy = NdjsonNamingPolicy.SnakeCaseLower)]
    public sealed class Item
    {
        public int Id;

        public string Label;

        public Rarity Rarity { get; set; }

        public SpawnState State { get; set; }

        [NdjsonEnumString]
        public Rarity DisplayRarity { get; set; }

        public Permissions Access { get; set; }

        public int? Charges { get; set; }

        public double? Weight { get; set; }

        [NdjsonProperty("uid")]
        public Guid UniqueId { get; set; }

        [NdjsonIgnore]
        public string Runtime { get; set; }

        [NdjsonIgnore(NdjsonIgnoreCondition.WhenWritingNull)]
        public string Note { get; set; }

        [NdjsonIgnore(NdjsonIgnoreCondition.WhenWritingDefault)]
        public int Stack { get; set; }
    }

    [NdjsonSerializable]
    public sealed class TimeSample
    {
        public DateTime Iso { get; set; }

        [NdjsonDateFormat(NdjsonDateFormat.UnixMilliseconds)]
        public DateTime Epoch { get; set; }

        [NdjsonDateFormat(NdjsonDateFormat.Ticks)]
        public DateTime Exact { get; set; }

        public DateTimeOffset Offset { get; set; }

        public TimeSpan Duration { get; set; }
    }

    [NdjsonSerializable]
    public sealed class Container
    {
        public int[] Numbers { get; set; }

        public List<Vector3Data> Points { get; set; }

        public Dictionary<string, int> Counters { get; set; }

        public Dictionary<string, Vector3Data> Anchors { get; set; }

        public List<List<int>> Grid { get; set; }

        public HashSet<string> Unique { get; set; }

        public byte[] Blob { get; set; }

        public NdjsonValue Free { get; set; }

        public Uri Endpoint { get; set; }
    }

    [NdjsonSerializable]
    public sealed class Immutable
    {
        public Immutable(int id, string name)
        {
            Id = id;
            Name = name;
        }

        public int Id { get; }

        public string Name { get; }

        public string Extra { get; set; }
    }

    [NdjsonSerializable]
    public sealed class WithInit
    {
        public int Id { get; init; }

        public string Name { get; init; }

        public int Counter { get; set; } = 42;
    }

    [NdjsonSerializable]
    public sealed record RecordSample(int Id, string Name, double Score);

    [NdjsonSerializable]
    public sealed class RequiredSample
    {
        [NdjsonRequired]
        public string Key { get; set; }

        public int Value { get; set; }
    }

    [NdjsonSerializable]
    public sealed class ExtensionSample
    {
        public int Id { get; set; }

        [NdjsonExtensionData]
        public Dictionary<string, NdjsonValue> Extra { get; set; }
    }

    [NdjsonSerializable]
    [NdjsonPolymorphic("kind")]
    [NdjsonDerived(typeof(CircleShape), "circle")]
    [NdjsonDerived(typeof(RectShape), "rect")]
    public abstract class Shape
    {
        public string Name { get; set; }
    }

    [NdjsonSerializable]
    public sealed class CircleShape : Shape
    {
        public double Radius { get; set; }
    }

    [NdjsonSerializable]
    public sealed class RectShape : Shape
    {
        public double Width { get; set; }

        public double Height { get; set; }
    }

    public sealed class ReflectedOnly
    {
        public int Id { get; set; }

        public string Name { get; set; }

        public List<int> Values { get; set; }

        public Rarity Rarity { get; set; }
    }

    public sealed class ReflectedRecordLike
    {
        public ReflectedRecordLike(string name, int count)
        {
            Name = name;
            Count = count;
        }

        public string Name { get; private set; }

        public int Count { get; private set; }
    }

    public sealed class CelsiusConverter : NdjsonConverter<double>
    {
        public override void Write(ref JsonWriter writer, in double value, NdjsonOptions options)
        {
            writer.WriteString(value.ToString("0.0", System.Globalization.CultureInfo.InvariantCulture) + "C");
        }

        public override double Read(ref JsonReader reader, NdjsonOptions options)
        {
            string text = reader.GetString();
            return double.Parse(text.TrimEnd('C'), System.Globalization.CultureInfo.InvariantCulture);
        }
    }

    [NdjsonSerializable]
    public sealed class Sensor
    {
        public string Id { get; set; }

        [NdjsonConverter(typeof(CelsiusConverter))]
        public double Temperature { get; set; }
    }

    [NdjsonSerializable(NamingPolicy = NdjsonNamingPolicy.CamelCase)]
    public sealed class LogLine
    {
        public DateTime Timestamp { get; set; }

        public string Level { get; set; }

        public string Message { get; set; }

        public int Code { get; set; }
    }
}
