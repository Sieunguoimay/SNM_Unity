using SNM.Structures;
using UnityEngine;

namespace SNM.Lifecycle
{
    public class LifecycleObjectSpawner<T> : BasicSpawner<T, AutoRemoveList<T>>
        where T : MonoBehaviour, ILifecycle
    {
        public LifecycleObjectSpawner()
        {
            SpawnedObjects.OnItemAdded += List_OnItemAdded;
            SpawnedObjects.OnItemRemoved += List_OnItemRemoved;
        }

        private void List_OnItemAdded(IBasicList<T> list, T lifecycle)
        {
            lifecycle.Initialize();
            lifecycle.AfterInitialize();
        }

        private void List_OnItemRemoved(IBasicList<T> list, T lifecycle)
        {
            lifecycle.BeforeDispose();
            lifecycle.Dispose();
        }
    }
}
