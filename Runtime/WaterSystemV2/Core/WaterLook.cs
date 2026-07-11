using System;
using UnityEngine;

namespace Snm.WaterSystemV2
{
    /// <summary>
    /// Everything about how the water LOOKS. Pure data — bound to the surface
    /// material exactly once by <see cref="WaterMaterialBinder"/> and re-bound
    /// only when edited (OnValidate marks it dirty). Nothing in here runs
    /// per frame. Defaults match WaterSystem V1.
    /// </summary>
    [Serializable]
    public class WaterLook
    {
        public DepthSettings depth = new();
        public CausticsSettings caustics = new();
        public FoamSettings foam = new();
        public ShorelineSettings shoreline = new();
        public SparkleSettings sparkle = new();
        public ScrollNormalSettings scrollNormal = new();
        public SpecularSettings specular = new();
        public RefractionSettings refraction = new();

        [Serializable]
        public class DepthSettings
        {
            public Color shallowColor = Color.white;
            public Color deepColor = Color.black;

            [Range(0f, 2f)]
            public float absorption = 0.4f;
        }

        [Serializable]
        public class CausticsSettings
        {
            public bool enabled = true;
            public Texture2D texture;

            [Range(0f, 5f)]
            public float strength = 1f;

            [Range(0.01f, 1f)]
            public float scale = 0.1f;

            [Range(0f, 0.5f)]
            public float speed = 0.05f;

            [Range(0f, 0.01f)]
            public float split = 0.003f;

            [Tooltip("Chromatic aberration split (6 texture samples). Disable for 2 samples on mobile.")]
            public bool chromaticSplit = true;
        }

        [Serializable]
        public class FoamSettings
        {
            public bool enabled = true;
            public Texture2D texture;

            [Range(0f, 3f)]
            public float strength = 1f;

            [Tooltip("Water shallower than this (world units) grows edge foam.")]
            [Range(0f, 5f)]
            public float depthThreshold = 0.5f;

            [Range(0.01f, 2f)]
            public float scale = 0.5f;

            [Range(0f, 0.5f)]
            public float speed = 0.05f;
        }

        [Serializable]
        public class ShorelineSettings
        {
            [Tooltip("Needs a baked shore mesh (Bake button on WaterBody). Auto-disabled while none is assigned.")]
            public bool enabled;

            [Range(1, 5)]
            public int waveCount = 3;

            [Range(0f, 2f)]
            public float speed = 0.5f;

            [Range(0f, 3f)]
            public float foamStrength = 1f;

            [Range(0.1f, 5f)]
            public float foamScale = 1f;
        }

        [Serializable]
        public class SparkleSettings
        {
            public bool enabled;

            [Range(0f, 5f)]
            public float intensity = 1f;

            [Range(1f, 100f)]
            public float density = 30f;

            [Range(0f, 2f)]
            public float speed = 0.5f;
        }

        [Serializable]
        public class ScrollNormalSettings
        {
            public bool enabled;
            public Texture2D normalMap;

            [Range(0f, 2f)]
            public float strength = 0.5f;

            [Range(0.01f, 2f)]
            public float scale = 0.2f;

            [Tooltip("Scroll direction and speed of the first layer.")]
            public Vector2 speed1 = new(0.03f, 0.02f);

            [Tooltip("Scroll direction and speed of the second layer.")]
            public Vector2 speed2 = new(-0.02f, 0.03f);
        }

        [Serializable]
        public class SpecularSettings
        {
            public bool enabled = true;
        }

        [Serializable]
        public class RefractionSettings
        {
            [Tooltip("How far the wave normal bends the background image (screen UV units).")]
            [Range(0f, 0.1f)]
            public float strength = 0.02f;
        }
    }
}
