using System;
using UnityEngine;

namespace Snm.GrassSystem
{
    [Serializable]
    public class GrassSystemConfig
    {
        [Header("Grid")]
        public Vector2Int gridSize = new(50, 50);
        public Vector2 cellSpacing = new(0.5f, 0.5f);

        [Header("Placement")]
        [Tooltip("Optional painted texture controlling grass placement. Each pixel = one grid cell. When assigned, gridSize is derived from texture dimensions.")]
        public Texture2D placementMap;
        [Range(0f, 1f)]
        public float densityThreshold = 0.1f;
        public float minScale = 0.8f;
        public float maxScale = 1.2f;

        [Header("Rendering (single-layer fallback)")]
        public Mesh grassMesh;
        public Material grassMaterial;

        [Tooltip("Height of the grass blade mesh in world units. Used by the shader to normalize vertex height for bending. Auto-derived from mesh bounds.")]
        public float bladeHeight = 1f;

        [Tooltip("Height of the grass surface for interaction detection (trample, proximity). Can differ from blade height to fine-tune when disturbers trigger bending.")]
        public float interactionHeight = 1f;

        [Header("Layers (multi-mesh)")]
        [Tooltip("When populated, each layer renders a separate grass mesh. Each layer reads a different channel from the placement map for density. Leave empty to use single grassMesh/grassMaterial.")]
        public GrassLayerConfig[] layers;

        public FrustumCullingConfig frustumCulling = new();
        public AmbientOcclusionConfig ambientOcclusion = new();
        public ColorVariationConfig colorVariation = new();
        public WindConfig wind = new();
        public TrampleConfig trample = new();

        public bool HasLayers => layers != null && layers.Length > 0;
    }

    [Serializable]
    public class GrassLayerConfig
    {
        public string name;
        public Mesh mesh;
        public Material material;

        [Tooltip("Which placement map channel controls this layer's density. 0=R, 1=G, 2=B, 3=A.")]
        [Range(0, 3)]
        public int densityChannel;

        [Range(0f, 1f)]
        public float densityThreshold = 0.1f;

        public float minScale = 0.8f;
        public float maxScale = 1.2f;

        [Tooltip("Offset added to yaw hash so blades in different layers don't share identical rotations.")]
        public float yawRandomSeed;
    }

    [Serializable]
    public class FrustumCullingConfig
    {
        public bool enabled = true;
        [Tooltip("Extra margin (world units) around each blade for frustum test. Prevents popping from wind sway.")]
        public float margin = 1f;
    }

    [Serializable]
    public class TrampleConfig
    {
        public bool enabled = true;
        public Shader trampleShader;
        public float trampleFadeSpeed = 0.1f;
        public float trampleHoldTime = 0.5f;
        public float disturbMinOffset = 0.01f;
        [Range(0f, 2f)]
        [Tooltip("How far above the grass surface (world units) a disturber can still bend grass.")]
        public float proximityTolerance = 0.5f;

        [Header("Recovery Spring")]
        [Tooltip("Enable damped spring oscillation when grass recovers from trample.")]
        public bool springEnabled = false;
        [Range(1f, 20f)]
        [Tooltip("How many oscillations during recovery. Higher = faster wobble.")]
        public float springFrequency = 8f;
        [Range(0.5f, 10f)]
        [Tooltip("How quickly the oscillation dies out. Higher = settles faster.")]
        public float springDamping = 3f;
        [Range(0f, 0.5f)]
        [Tooltip("Strength of the spring overshoot. Too high looks rubbery.")]
        public float springAmplitude = 0.15f;
    }

    [Serializable]
    public class AmbientOcclusionConfig
    {
        public bool enabled = false;
        [Range(0f, 1f)]
        [Tooltip("How dark the base of the blade gets. 0 = no AO, 1 = fully black at root.")]
        public float strength = 0.3f;
        [Range(0.5f, 5f)]
        [Tooltip("Controls the falloff curve. Higher = AO concentrated closer to the root.")]
        public float power = 2f;
    }

    [Serializable]
    public class ColorVariationConfig
    {
        public bool enabled = false;
        [Tooltip("One end of the per-blade color variation range. Multiplied into the base tint.")]
        public Color colorA = Color.white;
        [Tooltip("Other end of the per-blade color variation range. Multiplied into the base tint.")]
        public Color colorB = Color.white;
    }

    [Serializable]
    public class WindConfig
    {
        public bool enabled = false;
        public Texture2D windMap;
        public Vector2 windMapScale = new(10, 10);
        public float windScrollSpeed = 0.01f;
        public float windStrength = 1f;

        [Range(0f, 0.5f)]
        [Tooltip("Per-blade phase offset so blades sway out of sync.")]
        public float swayVariation = 0.1f;

        [Range(0f, 1f)]
        [Tooltip("Per-blade amplitude variation. 0 = uniform, 1 = full variation (0.7–1.3x).")]
        public float amplitudeVariation = 0.5f;
    }

}
