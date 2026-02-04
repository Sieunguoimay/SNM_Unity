using UnityEngine;

namespace Snm.Runtime.Unity
{
    public class UnityEngineUtility
    {
        public static TComponent CreateGameObjectWithComponent<TComponent>(string name = null)
            where TComponent : Component
        {
            var go = new GameObject
            {
                name = string.IsNullOrEmpty(name) ? $"[{typeof(TComponent).Name}]" : name,
                hideFlags = HideFlags.DontSave
            };

            if (go.TryGetComponent<TComponent>(out var c)) return c;

            return go.AddComponent<TComponent>();
        }

        public static void DestroyObject(Object obj)
        {
            if (Application.IsPlaying(obj))
            {
                Object.Destroy(obj);
            }
            else
            {
                Object.DestroyImmediate(obj);
            }
        }
    }
}