using System.Collections.Generic;
using NdJson;
using NdJson.Unity;
using UnityEngine;

namespace NdJson.Samples.Unity
{
    [NdjsonSerializable(NamingPolicy = NdjsonNamingPolicy.CamelCase)]
    public sealed class SaveEntry
    {
        public string EntityId { get; set; }

        public Vector3 Position { get; set; }

        public Quaternion Rotation { get; set; }

        public Color Tint { get; set; }

        public float Health { get; set; }

        public List<string> Inventory { get; set; }
    }

    public sealed class NdjsonUnityExample : MonoBehaviour
    {
        private const string SaveFile = "world.ndjson";

        private void Start()
        {
            SaveWorld();
            LoadWorld();
        }

        private void SaveWorld()
        {
            List<SaveEntry> entries = new List<SaveEntry>();

            foreach (Transform child in transform)
            {
                entries.Add(new SaveEntry
                {
                    EntityId = child.name,
                    Position = child.position,
                    Rotation = child.rotation,
                    Tint = Color.white,
                    Health = 100f,
                    Inventory = new List<string> { "torch", "rope" }
                });
            }

            NdjsonUnity.SaveToPersistent(SaveFile, entries);
            Debug.Log("Sauvegarde de " + entries.Count + " entites dans " + NdjsonUnity.PersistentPath(SaveFile));
        }

        private void LoadWorld()
        {
            List<SaveEntry> entries = NdjsonUnity.LoadFromPersistent<SaveEntry>(SaveFile);

            foreach (SaveEntry entry in entries)
            {
                Transform child = transform.Find(entry.EntityId);
                if (child == null)
                {
                    continue;
                }

                child.SetPositionAndRotation(entry.Position, entry.Rotation);
            }

            Debug.Log("Chargement de " + entries.Count + " entites");
        }

        public void AppendEvent(string message)
        {
            NdjsonUnity.AppendToPersistent("events.ndjson", new SaveEntry
            {
                EntityId = message,
                Position = transform.position,
                Health = 1f
            });
        }
    }
}
