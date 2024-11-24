using System.Collections.Generic;
using SNM.Structures;
using UnityEngine;

namespace SNM.Lifecycle
{
    /// <summary>
    /// This object provides automatic subscription with lifecycle system.
    /// Otherwise you got to manually do it.
    /// </summary>
    public class LifecycleObject : MonoBehaviour, ILifecycle,
        ISelfRemoveItem, IListTrackingItem<IAutoRemoveList>//This allow you to Dispose - RemoveSelf() and Destroy this object without knowing the containing lists
    {
        [SerializeField] private LifecycleObject[] directSubObjects;

        private readonly List<IAutoRemoveList> trackedLists = new();
        private IReadOnlyList<ILifecycle> _directSubObjects;

        void ILifecycle.Initialize()
        {
            _directSubObjects = directSubObjects;
            OnInitialize();
        }

        void ILifecycle.AfterInitialize()
        {
            OnAfterInitialize();
        }

        void ILifecycle.BeforeDispose()
        {
            OnBeforeDispose();
        }

        void ILifecycle.Dispose()
        {
            OnDispose();
        }

        protected virtual void OnInitialize()
        {
            foreach (var o in _directSubObjects) o.Initialize();
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
    }
}
