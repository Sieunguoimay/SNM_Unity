using System;
using UnityEngine;

namespace Snm.Runtime.Foundation
{
    /// <summary>
    /// Abstracts access to the "main" camera so subsystems do not have to call
    /// <see cref="UnityEngine.Camera.main"/> directly. Implementations are responsible
    /// for caching (Camera.main is alloc-heavy) and for invalidating the cache when
    /// the active camera changes (scene reload, camera rig rebuild, MainCamera tag
    /// moving between objects).
    /// </summary>
    public interface IMainCameraProvider
    {
        /// <summary>
        /// The current main camera, or <c>null</c> when no camera is tagged
        /// <c>MainCamera</c>. Cheap to call every frame.
        /// </summary>
        Camera Current { get; }

        /// <summary>
        /// Fired whenever <see cref="Current"/> changes (including transitions to/from
        /// <c>null</c>). The argument is the new camera (may be <c>null</c>).
        /// </summary>
        event Action<Camera> CameraChanged;
    }
}
