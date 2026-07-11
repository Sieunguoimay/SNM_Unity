using System;
using UnityEngine;

namespace Snm.WaterSystemV2
{
    /// <summary>
    /// Planar reflection tuning. Reflection renders an extra camera pass, so
    /// it is the most expensive feature — the interval throttle (a hidden
    /// magic number in V1) is exposed here.
    /// </summary>
    [Serializable]
    public class WaterReflectionSettings
    {
        public bool enabled = true;

        [Tooltip("Width of the reflection texture. Height follows the camera aspect. Structural — changing it rebuilds the reflection.")]
        public int textureWidth = 512;

        [Tooltip("When the camera is still, re-render the reflection only every N frames. 1 = every frame.")]
        [Range(1, 16)]
        public int frameInterval = 4;
    }
}
