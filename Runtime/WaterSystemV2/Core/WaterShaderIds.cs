using UnityEngine;

namespace Snm.WaterSystemV2
{
    /// <summary>
    /// Single source of truth for every shader property and keyword the water
    /// system touches. If a name here drifts from the shaders in Shaders/,
    /// that is a bug — nothing else in the module is allowed to hold a
    /// property string.
    /// </summary>
    public static class WaterShaderIds
    {
        // ── surface shader: depth ──────────────────────────────────────────
        public static readonly int ShallowColor = Shader.PropertyToID("_ShallowColor");
        public static readonly int DeepColor = Shader.PropertyToID("_DeepColor");
        public static readonly int Absorption = Shader.PropertyToID("_Absorption");

        // ── surface shader: refraction ─────────────────────────────────────
        public static readonly int RefractionStrength = Shader.PropertyToID("_RefractionStrength");

        // ── surface shader: caustics ───────────────────────────────────────
        public static readonly int CausticsTex = Shader.PropertyToID("_CausticsTex");
        public static readonly int CausticStrength = Shader.PropertyToID("_CausticStrength");
        public static readonly int CausticScale = Shader.PropertyToID("_CausticScale");
        public static readonly int CausticSpeed = Shader.PropertyToID("_CausticSpeed");
        public static readonly int CausticSplit = Shader.PropertyToID("_CausticSplit");

        // ── surface shader: waves ──────────────────────────────────────────
        public static readonly int WaveTex = Shader.PropertyToID("_WaveTex");
        public static readonly int WaveNormalStrength = Shader.PropertyToID("_WaveNormalStrength");

        // ── surface shader: foam ───────────────────────────────────────────
        public static readonly int FoamTex = Shader.PropertyToID("_FoamTex");
        public static readonly int FoamStrength = Shader.PropertyToID("_FoamStrength");
        public static readonly int FoamDepthThreshold = Shader.PropertyToID("_FoamDepthThreshold");
        public static readonly int FoamScale = Shader.PropertyToID("_FoamScale");
        public static readonly int FoamSpeed = Shader.PropertyToID("_FoamSpeed");

        // ── surface shader: shoreline ──────────────────────────────────────
        public static readonly int ShorelineWaveCount = Shader.PropertyToID("_ShorelineWaveCount");
        public static readonly int ShorelineSpeed = Shader.PropertyToID("_ShorelineSpeed");
        public static readonly int ShorelineFoamStrength = Shader.PropertyToID("_ShorelineFoamStrength");
        public static readonly int ShorelineFoamScale = Shader.PropertyToID("_ShorelineFoamScale");

        // ── surface shader: sparkle ────────────────────────────────────────
        public static readonly int SparkleIntensity = Shader.PropertyToID("_SparkleIntensity");
        public static readonly int SparkleDensity = Shader.PropertyToID("_SparkleDensity");
        public static readonly int SparkleSpeed = Shader.PropertyToID("_SparkleSpeed");

        // ── surface shader: scrolling normal ───────────────────────────────
        public static readonly int ScrollNormalMap = Shader.PropertyToID("_ScrollNormalMap");
        public static readonly int ScrollNormalStrength = Shader.PropertyToID("_ScrollNormalStrength");
        public static readonly int ScrollNormalScale = Shader.PropertyToID("_ScrollNormalScale");
        public static readonly int ScrollNormalSpeed1 = Shader.PropertyToID("_ScrollNormalSpeed1");
        public static readonly int ScrollNormalSpeed2 = Shader.PropertyToID("_ScrollNormalSpeed2");

        // ── surface shader: reflection ─────────────────────────────────────
        public static readonly int ReflectionTex = Shader.PropertyToID("_ReflectionTex");
        public static readonly int ReflectionVP = Shader.PropertyToID("_ReflectionVP");

        // ── surface shader: debug ──────────────────────────────────────────
        public static readonly int DebugView = Shader.PropertyToID("_DebugView");

        // ── wave simulation shader ─────────────────────────────────────────
        public static readonly int MainTex = Shader.PropertyToID("_MainTex");
        public static readonly int Damping = Shader.PropertyToID("_Damping");
        public static readonly int WaveSpeed = Shader.PropertyToID("_WaveSpeed");
        public static readonly int StampTex = Shader.PropertyToID("_StampTex");
        public static readonly int StampCount = Shader.PropertyToID("_StampCount");
        public static readonly int RainEnabled = Shader.PropertyToID("_RainEnabled");
        public static readonly int RainIntensity = Shader.PropertyToID("_RainIntensity");
        public static readonly int RainDensity = Shader.PropertyToID("_RainDensity");
        public static readonly int RainFrame = Shader.PropertyToID("_RainFrame");

        // ── wave display (debug preview) shader ────────────────────────────
        public static readonly int DisplayMode = Shader.PropertyToID("_DisplayMode");

        // ── surface shader keywords ────────────────────────────────────────
        public const string KeywordCaustics = "_CAUSTICS_ON";
        public const string KeywordCausticsChromatic = "_CAUSTICS_CHROMATIC";
        public const string KeywordReflection = "_REFLECTION_ON";
        public const string KeywordSpecular = "_SPECULAR_ON";
        public const string KeywordFoam = "_FOAM_ON";
        public const string KeywordShoreline = "_SHORELINE_ON";
        public const string KeywordSparkle = "_SPARKLE_ON";
        public const string KeywordScrollNormal = "_SCROLL_NORMAL_ON";

        // ── shader asset names (used by Reset/editor auto-assign) ──────────
        public const string SurfaceShaderName = "Snm/WaterSystemV2/WaterSurface";
        public const string SimulationShaderName = "Hidden/Snm/WaterSystemV2/WaveSimulation";
        public const string DisplayShaderName = "Hidden/Snm/WaterSystemV2/WaveDisplay";
    }
}
