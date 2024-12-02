using SNM.Structures;
using UnityEngine;

namespace SNM.Lifecycle
{
    public class LifecycleObjectSpawner<T> : BasicSpawner<T, AutoRemoveList<T>>, IDynamicLifecycleManager
        where T : MonoBehaviour, ILifecycle
    {
        public LifecycleObjectSpawner()
        {
            SpawnedObjects.OnItemAdded += List_OnItemAdded;
            SpawnedObjects.OnItemRemoved += List_OnItemRemoved;
        }

        void IDynamicLifecycleManager.Initialize(ILifecycle lifecycle)
        {
            Initialize(lifecycle);
        }
        
        private void Initialize(ILifecycle lifecycle)
        {
            lifecycle.Initialize(this);
        }

        void IDynamicLifecycleManager.Dispose(ILifecycle lifecycle)
        {
            Dispose(lifecycle);
        }

        private void Dispose(ILifecycle lifecycle)
        {
            lifecycle.Dispose();
        }

        private void List_OnItemAdded(IBasicList<T> list, T lifecycle)
        {
            Initialize(lifecycle);
        }

        private void List_OnItemRemoved(IBasicList<T> list, T lifecycle)
        {
            Dispose(lifecycle);
        }
    }
}
