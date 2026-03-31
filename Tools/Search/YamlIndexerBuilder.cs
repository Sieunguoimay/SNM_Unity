#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace Snm.Tools
{
    public static class YamlIndexerBuilder
    {
        [MenuItem("Tools/Snm/YAML Index/Rebuild")]
        public static void Rebuild()
        {
            var guids =
                AssetDatabase.FindAssets("");

            int count = guids.Length;

            for (int i = 0; i < count; i++)
            {
                var guid = guids[i];
                var path =
                    AssetDatabase.GUIDToAssetPath(guid);

                EditorUtility.DisplayProgressBar(
                    "Indexing YAML",
                    path,
                    i / (float)count);

                YamlIndexDatabase.UpdateAsset(
                    guid,
                    path);
            }

            EditorUtility.ClearProgressBar();

            YamlIndexDatabase.Save();

            Debug.Log("YAML Index Built");
        }
    }
}

#endif