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

        [Range(0.1f, 1f)]
        [Tooltip("Bend amount at which a blade's fall direction locks in. Blades bent past this keep the way they fell; 1 = only fully flat grass locks.")]
        public float directionLockAmount = 0.85f;

        [Range(1f, 6f)]
        [Tooltip("Edge softness across the ring between each stamp's full-flatten core and its outer radius. 1 = firm (grass stays strongly bent right up to the edge); higher = softer (bend drops off quickly once past the core, so the rim is a gentle fade).")]
        public float bendEdgeSoftness = 3f;

        [Header("Recovery Spring")]
        [Tooltip("Wobbles per recovery when a blade springs back upright.")]
        public float springFrequency = 8f;

        [Tooltip("How fast the recovery oscillation dies out.")]
        public float springDamping = 3f;

        [Tooltip("Overshoot strength. 0 = no wobble, blades just rise smoothly. Keep low or blades look rubbery.")]
        public float springAmplitude = 0f;

        [Header("Wind")]
        [Tooltip("World-space wind heading in degrees (0 = +X, 90 = +Z).")]
        public float windDirectionDegrees;

        [Tooltip("How fast gusts travel across the field.")]
        public float windSpeed = 1f;

        [Tooltip("Spatial size of gust patterns. Smaller = larger gust waves.")]
        public float windNoiseScale = 0.08f;

        [Range(0f, 1f)]
        [Tooltip("Steady lean along the wind direction. 0 = blades only rock back and forth around upright; higher = blades lean over toward the wind (like a wheat field) and gusts sway around that leaning pose. This is what makes grass look like it's being pushed, not just wobbling.")]
        public float windLean = 0.3f;

        [Range(0f, 1f)]
        [Tooltip("How together neighbouring blades move. 1 = coherent — blades sway in phase so gusts read as waves travelling across the field (matches the Wind Field debug). 0 = each blade has its own random phase (busier, less like a wave).")]
        public float windCoherence = 0.5f;

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
