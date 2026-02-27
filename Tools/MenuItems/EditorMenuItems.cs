#if UNITY_EDITOR
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;

namespace Snm.Tools.MenuItemExtra
{

    public static class EditorMenuItems
    {
        [MenuItem("Tools/Snm/LogAllMenuItems")]
        private static void LogAllMenuItems()
        {
            var menuItems = FindAllMenuItems();
            foreach (var (type, methodInfo, menuItem) in menuItems)
            {
                UnityEngine.Debug.Log($"{type.Name} -> {menuItem.menuItem} -> {methodInfo.Name}");
            }
        }

        private static IEnumerable<(System.Type type, MethodInfo methodInfo, MenuItem)> FindAllMenuItems()
        {
            var assemblies = System.AppDomain.CurrentDomain.GetAssemblies();

            foreach (var assembly in assemblies)
            {
                foreach (var type in assembly.GetTypes())
                {
                    foreach (var method in type.GetMethods(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic))
                    {
                        MenuItem mi = null;
                        try { mi = method.GetCustomAttribute<MenuItem>(); } catch (System.Exception) { }
                        if (mi != null)
                        {
                            yield return (type, method, mi);
                        }

                    }
                }
            }
        }
    }
}
#endif