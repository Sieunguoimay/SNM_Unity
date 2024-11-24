using System.Linq;
using SNM.Structures;
using UnityEngine;

namespace SNM.Lifecycle
{
    public class BasicSpawner<T, TList> : IBasicSpawner<T>
        where T : MonoBehaviour
        where TList : IBasicList<T>, new()
    {
        private readonly TList spawnedObjects = new();
        protected IBasicList<T> SpawnedObjects => spawnedObjects;

        T IBasicSpawner<T>.Spawn(T prefab, Transform parent)
        {
            var obj = Object.Instantiate(prefab, parent);
            spawnedObjects.Add(obj);
            return obj;
        }

        void IBasicSpawner<T>.Despawn(T obj)
        {
            if (spawnedObjects.Items.Contains(obj))
            {
                spawnedObjects.Remove(obj);
                Object.Destroy(obj);
            }
        }
    }
}
