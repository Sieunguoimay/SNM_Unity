#if UNITY_EDITOR
using UnityEditor;

namespace Snm.Tools
{
    public class YamlIndexPostprocessor : AssetPostprocessor
    {
        static void OnPostprocessAllAssets(
            string[] imported,
            string[] deleted,
            string[] moved,
            string[] movedFrom)
        {
            foreach (var p in imported)
            {
                var guid =
                    AssetDatabase.AssetPathToGUID(p);

                YamlIndexDatabase
                    .UpdateAsset(guid, p);
            }

            foreach (var p in deleted)
            {
                var guid =
                    AssetDatabase.AssetPathToGUID(p);

                YamlIndexDatabase
                    .Remove(guid);
            }

            YamlIndexDatabase.Save();
        }
    }
}

#endif