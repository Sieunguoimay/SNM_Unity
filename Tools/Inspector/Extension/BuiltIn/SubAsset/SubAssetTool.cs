#if UNITY_EDITOR
using System.Collections.Generic;
using System.Linq;
using UnityEditor;

namespace Snm.Tools.InspectorExtensions
{
    public class SubAssetTool
    {
        public static IEnumerable<UnityEngine.Object> GetSubAssets(UnityEngine.Object mainAsset)
        {
            var path = AssetDatabase.GetAssetPath(mainAsset);
            try
            {
                var allAssets = AssetDatabase.LoadAllAssetsAtPath(path);
                return allAssets.Where(a => a != mainAsset && a is not UnityEngine.GameObject && a is not UnityEngine.Component);
            }
            catch
            {
                return Enumerable.Empty<UnityEngine.Object>();
            }
        }

        public void RemoveSubAsset(UnityEngine.Object target)
        {
            AssetDatabase.RemoveObjectFromAsset(target);
            AssetDatabase.SaveAssets();
        }
    }
}

#endif