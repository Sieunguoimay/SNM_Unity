using System.Collections.Generic;
using UnityEngine;

namespace FruitCollectorGame
{

    public class LifecycleObject : MonoBehaviour, ILifecycle
    {
        [SerializeField] private LifecycleObject[] directSubObjects;

        public IReadOnlyList<ILifecycle> DirectSubObjects => directSubObjects;

        void ILifecycle.SetupInternal()
        {
            OnSetupInternal();
        }

        void ILifecycle.SetupDependencies()
        {
            OnSetupDependencies();
        }

        void ILifecycle.TearDownDependencies()
        {
            OnTearDownDependencies();
        }

        void ILifecycle.DestroyInternal()
        {
            OnDestroyInternal();
        }

        protected virtual void OnSetupInternal()
        {
            foreach (var o in DirectSubObjects) o.SetupInternal();
        }
        protected virtual void OnSetupDependencies()
        {
            foreach (var o in DirectSubObjects) o.SetupDependencies();
        }
        protected virtual void OnTearDownDependencies()
        {
            foreach (var o in DirectSubObjects) o.TearDownDependencies();
        }
        protected virtual void OnDestroyInternal()
        {
            foreach (var o in DirectSubObjects) o.DestroyInternal();
        }
    }
}
