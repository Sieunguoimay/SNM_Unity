using System.Collections.Generic;
using Snm.Runtime.Unity;
using UnityEngine;

namespace Snm.Framework.NodeHierarchy
{
    public class SpawningPool : MonoBehaviour
    {
        private static bool _isDestroyed;
        private static SpawningPool _instance;
        private readonly HashSet<ObjectPool<Object>> prefabPools = new();

        public static SpawningPool Instance
        {
            get
            {
                if (_isDestroyed) return null;
                if (_instance == null)
                {
                    _instance = UnityEngineUtility.CreateGameObjectWithComponent<SpawningPool>();
                }
                return _instance;
            }
        }

        private void OnDestroy()
        {
            _isDestroyed = true;
        }

        public T Spawn<T>(T prefab, Transform container) where T : Object
        {
            ObjectPool<Object> pool = null;

            foreach (var d in prefabPools)
            {
                if (d.Prefab == prefab)
                {
                    pool = d;
                    break;
                }
            }

            if (pool == null)
            {
                pool = new ObjectPool<Object>(prefab, 1, container);
                prefabPools.Add(pool);
            }

            return pool.Get() as T;
        }

        public void Despawn<T>(T obj) where T : Object
        {
            ObjectPool<Object> pool = null;

            foreach (var d in prefabPools)
            {
                if (d.ActiveObjects.Contains(obj))
                {
                    pool = d;
                    break;
                }
            }

            if (pool != null)
            {
                pool.ReturnToPool(obj);
            }
            else
            {
                Debug.LogError($"This object {obj.name} does not spawned by {name}", obj);
            }
        }
    }
}