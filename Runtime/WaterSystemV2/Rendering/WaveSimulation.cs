using System;
using UnityEngine;
using UnityEngine.Experimental.Rendering;

namespace Snm.WaterSystemV2
{
    /// <summary>
    /// GPU wave heightfield: ping-pong wave equation with stamp disturbances
    /// and procedural rain. One cohesive class replacing V1's five
    /// (Controller/Factory/SimPass/DisplayPass/StampRenderer).
    ///
    /// Per frame: <see cref="Step"/> runs the sim iterations and rebinds
    /// _WaveTex on the surface material (the ping-pong result alternates
    /// between two textures, so this one binding is genuinely per-frame).
    /// The debug preview blit only runs when something asks for it — V1 paid
    /// for a display blit every frame even with nothing watching.
    /// </summary>
    public class WaveSimulation : IDisposable
    {
        private readonly WaterWaveSettings _settings;
        private readonly Material _surfaceMaterial;
        private readonly PingPongTexture _pingPong;
        private readonly Material _simMaterial;
        private readonly StampBuffer _stamps;

        private Shader _displayShader;
        private Material _displayMaterial;   // lazy — debug preview only
        private RenderTexture _displayRT;    // lazy — debug preview only

        private int _frameCounter;

        /// <summary>Raw simulation texture (r = height, g = previous height).</summary>
        public RenderTexture Texture => _pingPong.Current;

        public int StampsLastFrame => _stamps.LastFrameCount;

        public WaveSimulation(
            WaterWaveSettings settings,
            Shader simulationShader,
            Shader displayShader,
            Material surfaceMaterial)
        {
            _settings = settings;
            _surfaceMaterial = surfaceMaterial;
            _displayShader = displayShader;

            var desc = new RenderTextureDescriptor(settings.textureSize, settings.textureSize)
            {
                graphicsFormat = GraphicsFormat.R16G16_SFloat,
                depthBufferBits = 0,
                msaaSamples = 1,
                sRGB = false,
            };

            _pingPong = new PingPongTexture(desc);
            _stamps = new StampBuffer(settings.stampCapacity);
            _simMaterial = new Material(simulationShader) { hideFlags = HideFlags.HideAndDontSave };
        }

        /// <summary>Queues a disturbance for the next step. Coordinates in surface UV space.</summary>
        public void AddDisturbance(Vector2 uv, float uvRadius, float strength)
        {
            _stamps.Add(uv, uvRadius, strength);
        }

        public void Step(float deltaTime)
        {
            int steps = Mathf.Max(1, _settings.iterationsPerFrame);

            // Total damping per frame stays constant regardless of step count.
            _simMaterial.SetFloat(WaterShaderIds.Damping, Mathf.Pow(_settings.damping, 1f / steps));
            _simMaterial.SetFloat(WaterShaderIds.WaveSpeed, Mathf.Min(_settings.waveSpeed, 0.5f));
            SetRainUniforms(deltaTime);

            // First iteration carries stamps + rain; the rest only propagate.
            _stamps.Upload(_simMaterial, WaterShaderIds.StampTex, WaterShaderIds.StampCount);
            Blit(1);

            if (steps > 1)
            {
                _simMaterial.SetFloat(WaterShaderIds.StampCount, 0);
                _simMaterial.SetFloat(WaterShaderIds.RainEnabled, 0);
                Blit(steps - 1);
            }

            _surfaceMaterial.SetTexture(WaterShaderIds.WaveTex, _pingPong.Current);
            _frameCounter++;
        }

        public void Clear() => _pingPong.Clear();

        /// <summary>
        /// Renders the debug preview (height or normal view) on demand.
        /// Never called by the game loop — editor/debug UI only.
        /// </summary>
        public RenderTexture RenderDebugPreview(int displayMode)
        {
            if (_displayShader == null) return Texture;

            if (_displayMaterial == null)
                _displayMaterial = new Material(_displayShader) { hideFlags = HideFlags.HideAndDontSave };

            if (_displayRT == null)
            {
                _displayRT = new RenderTexture(Texture.descriptor)
                {
                    graphicsFormat = GraphicsFormat.R8G8B8A8_UNorm,
                    name = "WaterWavePreview",
                };
                _displayRT.Create();
            }

            _displayMaterial.SetFloat(WaterShaderIds.DisplayMode, displayMode);
            Graphics.Blit(Texture, _displayRT, _displayMaterial);
            return _displayRT;
        }

        private void Blit(int iterations)
        {
            for (int i = 0; i < iterations; i++)
            {
                var (src, dst) = _pingPong.GetPair();
                _simMaterial.SetTexture(WaterShaderIds.MainTex, src);
                Graphics.Blit(src, dst, _simMaterial);
            }
        }

        private void SetRainUniforms(float deltaTime)
        {
            var rain = _settings.rain;
            if (rain == null || !rain.enabled)
            {
                _simMaterial.SetFloat(WaterShaderIds.RainEnabled, 0);
                return;
            }

            // "Drops per second" → per-pixel probability this frame.
            float texSize = _settings.textureSize;
            float probability = Mathf.Clamp01(rain.density * deltaTime / (texSize * texSize));

            _simMaterial.SetFloat(WaterShaderIds.RainEnabled, 1);
            _simMaterial.SetFloat(WaterShaderIds.RainIntensity, rain.intensity);
            _simMaterial.SetFloat(WaterShaderIds.RainDensity, probability);
            _simMaterial.SetFloat(WaterShaderIds.RainFrame, _frameCounter);
        }

        public void Dispose()
        {
            _stamps.Dispose();
            _pingPong.Dispose();
            WaterObjects.Destroy(_simMaterial);
            WaterObjects.Destroy(_displayMaterial);
            if (_displayRT != null)
            {
                _displayRT.Release();
                WaterObjects.Destroy(_displayRT);
            }
        }
    }
}
