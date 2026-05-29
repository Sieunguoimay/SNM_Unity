#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using System.IO;
using System.Linq;
using System.Collections.Generic;

namespace Snm.Tools
{
    [InitializeOnLoad]
    public static class YamlIndexDatabase
    {
        static string IndexPath =>
            Path.Combine("Library", "SnmYamlIndex.json");

        static Dictionary<string, YamlAssetIndex.Entry>
            guidToEntry = new();

        static Dictionary<string, List<YamlAssetIndex.Entry>>
            reverseMap;

        public static IReadOnlyCollection<
            YamlAssetIndex.Entry> Entries
            => guidToEntry.Values;

        public static bool HasIndex =>
            guidToEntry.Count > 0 &&
            System.IO.File.Exists(IndexPath);

        static YamlIndexDatabase()
        {
            Load();
        }

        // ==========================

        public static void Load()
        {
            guidToEntry.Clear();
            reverseMap = null;

            if (!File.Exists(IndexPath))
                return;

            var json =
                File.ReadAllText(IndexPath);

            if (string.IsNullOrWhiteSpace(json))
                return;

            var db =
                JsonUtility.FromJson<YamlAssetIndex>(json);

            foreach (var e in db.entries)
                guidToEntry[e.guid] = e;
        }

        public static void Save()
        {
            var db = new YamlAssetIndex
            {
                entries =
                    guidToEntry.Values.ToList()
            };

            File.WriteAllText(
                IndexPath,
                JsonUtility.ToJson(db));
        }

        public static void EnsureIndexReady()
        {
            if (HasIndex)
                return;

            Debug.Log("YAML Index missing → rebuilding...");

            YamlIndexerBuilder.Rebuild();
            Load();
        }
        // ==========================

        public static void UpdateAsset(
            string guid,
            string path)
        {
            if (!IsYaml(path))
                return;

            var full =
                Path.Combine(
                    Application.dataPath,
                    path["Assets/".Length..]);

            if (!File.Exists(full))
                return;

            var entry =
                new YamlAssetIndex.Entry
                {
                    guid = guid,
                    path = path,
                    lines = File.ReadAllLines(full)
                };

            guidToEntry[guid] = entry;
            reverseMap = null;
        }

        public static void Remove(string guid)
        {
            if (guidToEntry.Remove(guid))
                reverseMap = null;
        }

        public static IReadOnlyList<YamlAssetIndex.Entry>
            GetIncomingReferences(string guid)
        {
            if (string.IsNullOrEmpty(guid))
                return System.Array.Empty<YamlAssetIndex.Entry>();

            reverseMap ??=
                YamlIndexQuery.BuildReverseReferenceMap();

            return reverseMap.TryGetValue(guid, out var list)
                ? list
                : (IReadOnlyList<YamlAssetIndex.Entry>)
                    System.Array.Empty<YamlAssetIndex.Entry>();
        }

        static bool IsYaml(string path)
        {
            return path.EndsWith(".prefab")
                || path.EndsWith(".unity")
                || path.EndsWith(".asset")
                || path.EndsWith(".mat")
                || path.EndsWith(".anim")
                || path.EndsWith(".controller");
        }
    }
}

#endif