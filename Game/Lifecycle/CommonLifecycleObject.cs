using System.Collections.Generic;
using System.Linq;
using SNM.Structures;
using UnityEngine;

namespace SNM.Lifecycle
{
    /// <summary>
    /// This object provides automatic subscription with lifecycle system.
    /// Otherwise you got to manually do it.
    /// </summary>
    public class CommonLifecycleObject : MonoBehaviour,
        ILifecycle, IBatchLifecycle, IAutoDisposeLifecycle,
        IDynamicLifecycleManager,
        ISelfRemoveItem, IListTrackingItem<IAutoRemoveList>//This allow you to Dispose - RemoveSelf() and Destroy this object without knowing the containing lists
    {
        [SerializeField] private CommonLifecycleObject[] directSubObjects;

        private readonly List<IAutoRemoveList> trackedLists = new();
        private readonly List<ILifecycle> dynamicLifecycles = new();
        private IBatchLifecycle[] _directSubObjects;

        private ILifecycleManager _manager;

        void ILifecycle.Initialize(ILifecycleManager manager)
        {
            _manager = manager;
            _directSubObjects = directSubObjects.OfType<IBatchLifecycle>().ToArray();
            OnInitialize();
        }

        void IBatchLifecycle.AfterInitialize()
        {
            OnAfterInitialize();
        }

        void IBatchLifecycle.BeforeDispose()
        {
            OnBeforeDispose();
        }

        void ILifecycle.Dispose()
        {
            OnDispose();
        }

        protected virtual void OnInitialize()
        {
            foreach (var o in _directSubObjects) o.Initialize(this);
        }
        protected virtual void OnAfterInitialize()
        {
            foreach (var o in _directSubObjects) o.AfterInitialize();
        }
        protected virtual void OnBeforeDispose()
        {
            foreach (var o in _directSubObjects) o.BeforeDispose();
        }
        protected virtual void OnDispose()
        {
            foreach (var o in _directSubObjects) o.Dispose();
        }

        void ISelfRemoveItem.RemoveSelf()
        {
            foreach (var l in trackedLists)
            {
                l.AutoRemove(this);
            }
        }

        void IListTrackingItem<IAutoRemoveList>.AddList(IAutoRemoveList list)
        {
            trackedLists.Add(list);
        }

        void IListTrackingItem<IAutoRemoveList>.RemoveList(IAutoRemoveList list)
        {
            trackedLists.Remove(list);
        }

        void IDynamicLifecycleManager.Initialize(ILifecycle lifecycle)
        {
            dynamicLifecycles.Add(lifecycle);
        }

        void IDynamicLifecycleManager.Dispose(ILifecycle lifecycle)
        {
            dynamicLifecycles.Remove(lifecycle);
        }

        void IAutoDisposeLifecycle.RequestDispose()
        {
            if (_manager is IDynamicLifecycleManager m)
            {
                m.Dispose(this);
            }
        }
    }
}
