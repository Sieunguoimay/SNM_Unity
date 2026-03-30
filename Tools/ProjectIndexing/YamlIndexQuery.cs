#if UNITY_EDITOR
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace Snm.Tools
{
    public static class YamlIndexQuery
    {
        static readonly Regex GuidPattern =
            new(@"guid:\s*([0-9a-f]{32})",
                RegexOptions.Compiled);

        /// <summary>
        /// Extract all GUIDs referenced in the YAML lines
        /// of an index entry (excludes the entry's own GUID).
        /// </summary>
        public static HashSet<string> ExtractReferencedGuids(
            YamlAssetIndex.Entry entry)
        {
            var guids = new HashSet<string>();

            if (entry.lines == null)
                return guids;

            foreach (var line in entry.lines)
            {
                var m = GuidPattern.Match(line);
                while (m.Success)
                {
                    var g = m.Groups[1].Value;
                    if (g != entry.guid)
                        guids.Add(g);
                    m = m.NextMatch();
                }
            }

            return guids;
        }

        /// <summary>
        /// Build a map: referencedGuid → set of entries that reference it.
        /// </summary>
        public static Dictionary<string, List<YamlAssetIndex.Entry>>
            BuildReverseReferenceMap()
        {
            YamlIndexDatabase.EnsureIndexReady();

            var map =
                new Dictionary<string, List<YamlAssetIndex.Entry>>();

            foreach (var entry in YamlIndexDatabase.Entries)
            {
                var refs = ExtractReferencedGuids(entry);

                foreach (var refGuid in refs)
                {
                    if (!map.TryGetValue(refGuid, out var list))
                    {
                        list = new List<YamlAssetIndex.Entry>();
                        map[refGuid] = list;
                    }

                    list.Add(entry);
                }
            }

            return map;
        }

        /// <summary>
        /// Check if an asset path looks like a MonoBehaviour script.
        /// </summary>
        public static bool IsScript(string path)
        {
            return path.EndsWith(".cs")
                || path.EndsWith(".js");
        }
    }
}
#endif
