#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEditor;

namespace Snm.Tools
{
    public static class MonoScriptFinder
    {
        static readonly Dictionary<string, MonoScript> s_cacheByTypeFullName = new();

        public static MonoScript GetMonoScriptForType(Type type)
        {
            if (type == null) return null;
            var key = type.FullName ?? type.Name;
            if (s_cacheByTypeFullName.TryGetValue(key, out var cached) && cached != null) return cached;

            var guids = AssetDatabase.FindAssets("t:MonoScript");
            for (int i = 0; i < guids.Length; i++)
            {
                var path = AssetDatabase.GUIDToAssetPath(guids[i]);
                var script = AssetDatabase.LoadAssetAtPath<MonoScript>(path);
                if (script == null) continue;

                var scriptClass = script.GetClass();
                if (scriptClass == null)
                {
                    var text = script.text;
                    if (!string.IsNullOrEmpty(text) && text.Contains(type.Name))
                    {
                        s_cacheByTypeFullName[key] = script;
                        return script;
                    }
                    continue;
                }

                if (scriptClass == type)
                {
                    s_cacheByTypeFullName[key] = script;
                    return script;
                }

                if (scriptClass.FullName == type.FullName)
                {
                    s_cacheByTypeFullName[key] = script;
                    return script;
                }
            }

            return null;
        }

        public static void ClearCache() => s_cacheByTypeFullName.Clear();
    }

}
#endif