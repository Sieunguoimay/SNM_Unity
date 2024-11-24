using UnityEngine;

namespace SNM.Lifecycle
{
    public interface IBasicSpawner<T>
    {
        T Spawn(T prefab, Transform parent);
        void Despawn(T obj);
    }
}
