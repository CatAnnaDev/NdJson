using System.Collections.Generic;
using Godot;
using NdJson;
using NdJson.GodotIntegration;

namespace NdJson.Samples.GodotSample
{
    [NdjsonSerializable(NamingPolicy = NdjsonNamingPolicy.SnakeCaseLower)]
    public sealed class SpawnRecord
    {
        public string Scene { get; set; }

        public Vector3 Position { get; set; }

        public Quaternion Rotation { get; set; }

        public Color Modulate { get; set; }

        public int Level { get; set; }

        public Dictionary<string, int> Stats { get; set; }
    }

    public partial class NdjsonGodotExample : Node
    {
        private const string CatalogPath = "res://data/spawns.ndjson";
        private const string SavePath = "user://save/run.ndjson";

        public override void _Ready()
        {
            LoadCatalog();
            SaveRun();
        }

        private void LoadCatalog()
        {
            foreach (SpawnRecord record in NdjsonGodot.ReadLines<SpawnRecord>(CatalogPath))
            {
                PackedScene scene = GD.Load<PackedScene>(record.Scene);
                if (scene == null)
                {
                    continue;
                }

                Node3D instance = scene.Instantiate<Node3D>();
                instance.Position = record.Position;
                instance.Quaternion = record.Rotation;
                AddChild(instance);
            }
        }

        private void SaveRun()
        {
            List<SpawnRecord> records = new List<SpawnRecord>();

            foreach (Node child in GetChildren())
            {
                Node3D node = child as Node3D;
                if (node == null)
                {
                    continue;
                }

                records.Add(new SpawnRecord
                {
                    Scene = node.SceneFilePath,
                    Position = node.Position,
                    Rotation = node.Quaternion,
                    Modulate = Colors.White,
                    Level = 1,
                    Stats = new Dictionary<string, int> { { "hp", 100 }, { "mp", 30 } }
                });
            }

            NdjsonGodot.WriteAll(SavePath, records);
            GD.Print("Sauvegarde de ", records.Count, " entites dans ", SavePath);
        }

        public void LogEvent(string kind, Vector3 where)
        {
            NdjsonGodot.Append("user://save/events.ndjson", new SpawnRecord
            {
                Scene = kind,
                Position = where,
                Level = 0
            });
        }
    }
}
