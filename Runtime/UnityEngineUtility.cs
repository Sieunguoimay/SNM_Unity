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
                name = string.IsNullOrEmpty(name) ? $"[{nameof(TComponent)}]" : name,
                hideFlags = HideFlags.DontSave
            };

            if (go.TryGetComponent<TComponent>(out var c)) return c;

            return go.AddComponent<TComponent>();
        }

        public static void DestroyGameObject(GameObject gameObject)
        {
            if (Application.IsPlaying(gameObject))
            {
                Object.Destroy(gameObject);
            }
            else
            {
                Object.DestroyImmediate(gameObject);
            }
        }
    }
}