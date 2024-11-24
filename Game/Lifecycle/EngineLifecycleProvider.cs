using System.Linq;
using UnityEngine;

namespace SNM.Lifecycle
{
    /// <summary>
    /// This class provides the unity lifecycle events to the Lifecycle system.
    /// </summary>
    public class EngineLifecycleProvider : MonoBehaviour
    {
        [ObjectSelector(typeof(ILifecycle))]
        [SerializeField] private Object[] lifecycleObjects;
        private ILifecycle[] _lifecycles;

        private void Awake()
        {
            _lifecycles = lifecycleObjects.OfType<ILifecycle>().ToArray();
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

        private void TryInitializeLifecycleManager()
        {
            foreach (var lifecycle in _lifecycles)
            {
                lifecycle.Initialize();
            }

            foreach (var lifecycle in _lifecycles)
            {
                lifecycle.AfterInitialize();
            }
        }

        private void TryDisposeLifecycleManager()
        {
            foreach (var lifecycle in _lifecycles)
            {
                lifecycle.BeforeDispose();
            }
            foreach (var lifecycle in _lifecycles)
            {
                lifecycle.Dispose();
            }
        }
    }
}
