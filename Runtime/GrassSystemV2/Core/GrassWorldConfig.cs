using System;
using UnityEngine;

namespace Snm.GrassSystemV2
{
    public enum GrassRenderTierMode
    {
        [Tooltip("GPU-driven on platforms with compute shaders, Simple elsewhere.")]
        Auto = 0,
        [Tooltip("Force compute-shader culling + indirect draws (PC/console/modern mobile).")]
        ForceGpuDriven = 1,
        [Tooltip("Force the CPU chunk-cull + prefix-draw path (safe everywhere).")]
        ForceSimple = 2,
    }

    /// <summary>
    /// All tuning for a <see cref="GrassWorld"/>, embedded in the component so a
    /// scene needs exactly one object and zero extra assets to configure.
    /// </summary>
    [Serializable]
    public class GrassWorldConfig
    {
        [Header("Chunks")]
        [Tooltip("Edge length of one square chunk in meters. Changing this requires repainting (data is stored per chunk).")]
        public float chunkSize = 16f;

        [Header("Distances")]
        [Tooltip("Grass beyond this distance from the camera is not drawn at all.")]
        public float maxDrawDistance = 60f;

        [Tooltip("Distance where blades switch from LOD0 to LOD1 (when the type has a LOD1 mesh).")]
        public float lodDistance = 25f;

        [Tooltip("Distance where density starts thinning toward zero at Max Draw Distance.")]
        public float densityFalloffStart = 30f;

        [Header("Render Tier")]
        public GrassRenderTierMode tierMode = GrassRenderTierMode.Auto;

        [Tooltip("Per-type cap of visible instances for the GPU-driven tier's compacted buffers.")]
        public int maxVisibleInstancesPerType = 262144;

        [Header("Interaction Canvas")]
        [Tooltip("Resolution of the sliding interaction render targets.")]
        public int canvasResolution = 512;

        [Tooltip("World-space size (meters) covered by the interaction canvas, centered on the camera focus.")]
        public float canvasWorldSize = 48f;

        [Tooltip("How fast bend recovers after a disturber leaves (higher = faster).")]
        public float bendFadeSpeed = 1.5f;

        [Tooltip("Seconds a bend stamp keeps full strength before starting to fade.")]
        public float bendHoldTime = 0.1f;

        [Header("Recovery Spring")]
        [Tooltip("Wobbles per recovery when a blade springs back upright.")]
        public float springFrequency = 8f;

        [Tooltip("How fast the recovery oscillation dies out.")]
        public float springDamping = 3f;

        [Tooltip("Overshoot strength. Keep low or blades look rubbery.")]
        public float springAmplitude = 0.15f;

        [Header("Wind")]
        [Tooltip("World-space wind heading in degrees (0 = +X, 90 = +Z).")]
        public float windDirectionDegrees;

        [Tooltip("How fast gusts travel across the field.")]
        public float windSpeed = 1f;

        [Tooltip("Spatial size of gust patterns. Smaller = larger gust waves.")]
        public float windNoiseScale = 0.08f;

        [Header("Effects")]
        [Tooltip("Seconds for the freeze effect to fully thaw. 0 = permanent while inside the canvas.")]
        public float freezeThawTime = 6f;

        [Tooltip("World tint color used by the Tint effect stamp.")]
        public Color tintColor = new(0.9f, 0.4f, 0.9f, 1f);

        /// <summary>Resolved tier for the current platform.</summary>
        public bool UseGpuDrivenTier => tierMode switch
        {
            GrassRenderTierMode.ForceGpuDriven => true,
            GrassRenderTierMode.ForceSimple => false,
            _ => SystemInfo.supportsComputeShaders,
        };
    }
}
