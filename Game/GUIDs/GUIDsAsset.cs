using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace GUIDs
{
    public class GUIDsAsset : ScriptableObject
    {
        [SerializeField] private MonoScript guidsScript;
        [SerializeField] private List<string> guids;

        [ContextMenu("AddNew")]
        private void AddNew()
        {
            guids.Add(GenerateGUID());
        }

        [ContextMenu("Export")]
        private void Export()
        {
            var path = AssetDatabase.GetAssetPath(this).Replace(".asset", ".cs");
            var absolutePath = path.Replace("Assets", Application.dataPath);
            var cs = string.Join("\n", CreateCSLines());

            File.WriteAllText(absolutePath, cs);

            AssetDatabase.Refresh();

            Debug.Log($"{absolutePath}");
        }

        private IEnumerable<string> CreateCSLines()
        {
            var tab = "    ";
            yield return $"public static class {name}{{";
            foreach (var guid in guids)
            {
                yield return $"{tab}public static string _{guid} = \"{guid}\";";
            }
            yield return $"}}";
        }

        private static string GenerateGUID()
        {
            var ticks = new DateTime(2016, 1, 1).Ticks;
            var ans = DateTime.Now.Ticks - ticks;
            var uniqueId = ans.ToString("x");
            return uniqueId;
        }
    }
}