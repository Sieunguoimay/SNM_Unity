using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace SNM.Lifecycle
{
    /// <summary>
    /// This class provides the unity lifecycle events to the Lifecycle system.
    /// </summary>
    public class EngineLifecycleProvider : MonoBehaviour, ILifecycleManager
    {
        [ObjectSelector(typeof(IBatchLifecycle))]
        [SerializeField] private Object[] preAttachedLifecycleObjects;

        private readonly List<IBatchLifecycle> lifecycles = new();
        private bool _isInitialized = false;

        private void Awake()
        {
            lifecycles.AddRange(preAttachedLifecycleObjects.OfType<IBatchLifecycle>());
        }

        //Initialize on enable is fine! No difference.
        private void OnEnable()
        {
            TryInitializeLifecycleManager();
        }

        private void OnApplicationFocusChanged(bool focus)
        {
            if (focus)
            {
                TryInitializeLifecycleManager();
            }
            else
            {
                TryDisposeLifecycleManager();
            }
        }

        private void OnDisable()
        {
            TryDisposeLifecycleManager();
        }

        private void OnDestroy()
        {
            lifecycles.Clear();
        }

        private void TryInitializeLifecycleManager()
        {
            foreach (var lifecycle in lifecycles)
            {
                lifecycle.Initialize(this);
            }

            foreach (var lifecycle in lifecycles)
            {
                lifecycle.AfterInitialize();
            }

            _isInitialized = true;
        }

        private void TryDisposeLifecycleManager()
        {
            foreach (var lifecycle in lifecycles)
            {
                lifecycle.BeforeDispose();
            }
            foreach (var lifecycle in lifecycles)
            {
                lifecycle.Dispose();
            }
            _isInitialized = false;
        }
    }
}
