using System;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Snm.Runtime.Foundation
{
    /// <summary>
    /// Default <see cref="IMainCameraProvider"/> implementation. Caches
    /// <see cref="UnityEngine.Camera.main"/> (which is alloc-heavy) and refreshes the
    /// cache when the active scene changes or when the cached camera is destroyed.
    ///
    /// A process-wide <see cref="Default"/> instance is exposed as a pragmatic
    /// fallback for MonoBehaviours that cannot easily receive an injected provider.
    /// Prefer constructing your own instance and injecting it where DI is available.
    /// </summary>
    public sealed class MainCameraProvider : IMainCameraProvider, IDisposable
    {
        static MainCameraProvider _default;

        /// <summary>
        /// Process-wide fallback instance. Lazily created on first access; never
        /// disposed. Intended for MonoBehaviours that have no DI seam.
        /// </summary>
        public static MainCameraProvider Default => _default ??= new MainCameraProvider();

        Camera _cached;
        bool _subscribed;

        public event Action<Camera> CameraChanged;

        public MainCameraProvider()
        {
            SceneManager.activeSceneChanged += OnActiveSceneChanged;
            _subscribed = true;
            RefreshCache(notify: false);
        }

        public Camera Current
        {
            get
            {
                // Validate cache against destruction & main-camera-tag migration.
                // Camera.main internally walks tagged cameras; we still want to keep
                // that cost off the hot path, so we only refresh when the cache is
                // invalid (null / destroyed / no longer tagged MainCamera).
                if (_cached == null || !_cached.gameObject.activeInHierarchy || _cached.tag != "MainCamera")
                    RefreshCache(notify: true);

                return _cached;
            }
        }

        /// <summary>
        /// Force a re-resolve of <see cref="UnityEngine.Camera.main"/>. Call this if
        /// you know the main camera was swapped without triggering a scene change
        /// (e.g. after rebuilding a camera rig).
        /// </summary>
        public void Invalidate()
        {
            RefreshCache(notify: true);
        }

        void OnActiveSceneChanged(Scene previous, Scene next)
        {
            RefreshCache(notify: true);
        }

        void RefreshCache(bool notify)
        {
            var fresh = Camera.main;
            if (fresh == _cached) return;

            _cached = fresh;
            if (notify) CameraChanged?.Invoke(_cached);
        }

        public void Dispose()
        {
            if (!_subscribed) return;
            SceneManager.activeSceneChanged -= OnActiveSceneChanged;
            _subscribed = false;
            CameraChanged = null;
        }
    }
}
