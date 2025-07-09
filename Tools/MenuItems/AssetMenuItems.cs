#if UNITY_EDITOR
using UnityEditor;

namespace Snm.Tools.MenuItemExtra
{
    public static class AssetMenuItems
    {
        [MenuItem("Assets/Tools/LogAssetUsages")]
        private static void LogAssetUsages()
        {
            AssetUsageHelper.LogAssetUsages(Selection.activeObject, AssetUsageHelper.GetAllDependents(Selection.activeObject, AssetUsageHelper.GetAllAssetPaths()));
        }

        [MenuItem("Assets/Tools/LogGUID")]
        private static void LogGUID()
        {
            if (Selection.activeObject != null)
            {
                UnityEngine.Debug.Log(AssetDatabase.TryGetGUIDAndLocalFileIdentifier(Selection.activeObject, out var guid, out long l) ? $"{guid} - {l}" : "NULL", Selection.activeObject);
            }
        }
    }
}
#endif