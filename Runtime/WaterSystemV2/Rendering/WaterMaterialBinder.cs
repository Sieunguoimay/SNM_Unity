using UnityEngine;

namespace Snm.WaterSystemV2
{
    /// <summary>
    /// Pushes a <see cref="WaterLook"/> into the surface material. Called once
    /// on setup and again only when the look was edited — never per frame
    /// (V1 re-bound everything every frame). Also the only place keywords are
    /// toggled, so look features can be switched live in play mode.
    /// </summary>
    public static class WaterMaterialBinder
    {
        /// <param name="shorelineAvailable">
        /// False when no baked shore mesh is active — shoreline is then forced
        /// off instead of rendering garbage from missing UV1 data (V1's
        /// "whole surface thinks it is shore" trap).
        /// </param>
        public static void Apply(Material m, WaterLook look, WaterWaveSettings waves, bool shorelineAvailable)
        {
            // ── depth / refraction ─────────────────────────────────────────
            m.SetColor(WaterShaderIds.ShallowColor, look.depth.shallowColor);
            m.SetColor(WaterShaderIds.DeepColor, look.depth.deepColor);
            m.SetFloat(WaterShaderIds.Absorption, look.depth.absorption);
            m.SetFloat(WaterShaderIds.RefractionStrength, look.refraction.strength);

            // ── caustics ───────────────────────────────────────────────────
            SetKeyword(m, WaterShaderIds.KeywordCaustics, look.caustics.enabled);
            SetKeyword(m, WaterShaderIds.KeywordCausticsChromatic, look.caustics.enabled && look.caustics.chromaticSplit);
            if (look.caustics.texture != null)
                m.SetTexture(WaterShaderIds.CausticsTex, look.caustics.texture);
            m.SetFloat(WaterShaderIds.CausticStrength, look.caustics.strength);
            m.SetFloat(WaterShaderIds.CausticScale, look.caustics.scale);
            m.SetFloat(WaterShaderIds.CausticSpeed, look.caustics.speed);
            m.SetFloat(WaterShaderIds.CausticSplit, look.caustics.split);

            // ── foam ───────────────────────────────────────────────────────
            SetKeyword(m, WaterShaderIds.KeywordFoam, look.foam.enabled);
            if (look.foam.texture != null)
                m.SetTexture(WaterShaderIds.FoamTex, look.foam.texture);
            m.SetFloat(WaterShaderIds.FoamStrength, look.foam.strength);
            m.SetFloat(WaterShaderIds.FoamDepthThreshold, look.foam.depthThreshold);
            m.SetFloat(WaterShaderIds.FoamScale, look.foam.scale);
            m.SetFloat(WaterShaderIds.FoamSpeed, look.foam.speed);

            // ── shoreline ──────────────────────────────────────────────────
            SetKeyword(m, WaterShaderIds.KeywordShoreline, look.shoreline.enabled && shorelineAvailable);
            m.SetInt(WaterShaderIds.ShorelineWaveCount, look.shoreline.waveCount);
            m.SetFloat(WaterShaderIds.ShorelineSpeed, look.shoreline.speed);
            m.SetFloat(WaterShaderIds.ShorelineFoamStrength, look.shoreline.foamStrength);
            m.SetFloat(WaterShaderIds.ShorelineFoamScale, look.shoreline.foamScale);

            // ── sparkle ────────────────────────────────────────────────────
            SetKeyword(m, WaterShaderIds.KeywordSparkle, look.sparkle.enabled);
            m.SetFloat(WaterShaderIds.SparkleIntensity, look.sparkle.intensity);
            m.SetFloat(WaterShaderIds.SparkleDensity, look.sparkle.density);
            m.SetFloat(WaterShaderIds.SparkleSpeed, look.sparkle.speed);

            // ── scrolling normal ───────────────────────────────────────────
            SetKeyword(m, WaterShaderIds.KeywordScrollNormal, look.scrollNormal.enabled);
            if (look.scrollNormal.normalMap != null)
                m.SetTexture(WaterShaderIds.ScrollNormalMap, look.scrollNormal.normalMap);
            m.SetFloat(WaterShaderIds.ScrollNormalStrength, look.scrollNormal.strength);
            m.SetFloat(WaterShaderIds.ScrollNormalScale, look.scrollNormal.scale);
            m.SetVector(WaterShaderIds.ScrollNormalSpeed1, look.scrollNormal.speed1);
            m.SetVector(WaterShaderIds.ScrollNormalSpeed2, look.scrollNormal.speed2);

            // ── specular ───────────────────────────────────────────────────
            SetKeyword(m, WaterShaderIds.KeywordSpecular, look.specular.enabled);

            // ── waves (static part; _WaveTex is bound by WaveSimulation) ───
            m.SetFloat(WaterShaderIds.WaveNormalStrength, waves.enabled ? waves.normalStrength : 0f);
        }

        public static void ApplyDebugView(Material m, WaterDebugView view)
        {
            m.SetFloat(WaterShaderIds.DebugView, (float)view);
        }

        private static void SetKeyword(Material m, string keyword, bool on)
        {
            if (on) m.EnableKeyword(keyword);
            else m.DisableKeyword(keyword);
        }
    }

    /// <summary>Fullscreen replacement views for debugging, applied on the surface shader.</summary>
    public enum WaterDebugView
    {
        Off = 0,
        WaveHeight = 1,
        Normals = 2,
        ShoreDistance = 3,
    }
}
