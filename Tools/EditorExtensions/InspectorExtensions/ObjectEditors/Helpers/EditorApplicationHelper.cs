#if UNITY_EDITOR
using System.Reflection;
using UnityEditor;
using UnityEngine;

public static class EditorApplicationHelper
{
    public static bool GetEnabledWithContext(string menuItemPath, UnityEngine.Object target = null)
    {
        var type = typeof(UnityEditor.Menu);
        var method_GetEnabledWithContext = type.GetMethod("GetEnabledWithContext", BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Public);
        var result_GetEnabledWithContext = (bool)method_GetEnabledWithContext.Invoke(null, new object[] { menuItemPath, new UnityEngine.Object[] { target } });

        return result_GetEnabledWithContext;
    }

    public static void ExecuteMenuItem(string menuItemPath, UnityEngine.Object target = null)
    {
        var type = typeof(EditorApplication);
        if (GetEnabledWithContext(menuItemPath, target))
        {
            var method = type.GetMethod("ExecuteMenuItemWithTemporaryContext", BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Public);
            method.Invoke(null, new object[] { menuItemPath, new UnityEngine.Object[] { target } });
        }
        else
        {
            Debug.LogError("Not found menu item: " + menuItemPath);
        }
    }
}
#endif
