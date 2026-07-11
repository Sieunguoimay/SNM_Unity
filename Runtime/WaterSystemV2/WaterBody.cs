using System.Collections.Generic;
using UnityEngine;

namespace Snm.WaterSystemV2
{
    /// <summary>
    /// One component = one body of water. Drop it in a scene and it works:
    /// mesh, material, waves, reflection, buoyancy and interaction all set
    /// themselves up from the embedded settings below — no config assets, no
    /// factories, no DI, no helper GameObjects.
    ///
    /// Composition: the static look is bound to the material once (and again
    /// only when edited); the only per-frame work is the handful of systems
    /// that are genuinely dynamic — <see cref="WaveSimulation"/> (Update),
    /// <see cref="WakeTracker"/> (Update), <see cref="BuoyancySolver"/>
    /// (FixedUpdate) and <see cref="PlanarReflection"/> (LateUpdate).
    ///
    /// Runs in edit mode ([ExecuteAlways]) so the water looks right without
    /// pressing Play; the dynamic systems themselves are play-mode only.
    /// </summary>
    [ExecuteAlways]
    [RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
    [AddComponentMenu("Snm/Water System V2/Water Body")]
    public class WaterBody : MonoBehaviour
    {
        [Tooltip("Camera the reflection mirrors. Leave empty to use Camera.main when entering play mode.")]
        [SerializeField] private Camera sourceCamera;

        [SerializeField] private WaterSurfaceSettings surface = new();
        [SerializeField] private WaterLook look = new();
        [SerializeField] private WaterWaveSettings waves = new();
        [SerializeField] private WaterReflectionSettings reflection = new();
        [SerializeField] private WaterBuoyancySettings buoyancy = new();
        [SerializeField] private WaterShoreBakeSettings shoreBake = new();

        [Tooltip("Auto-assigned when the component is added (Reset). Only touch these when experimenting with shader variants.")]
        [SerializeField] private Shader surfaceShader;
        [SerializeField] private Shader waveSimulationShader;
        [SerializeField] private Shader waveDisplayShader;

        [Tooltip("Replaces the water color with a diagnostic view. Wave Height / Normals need play mode; Shore Distance shows the baked UV1.")]
        [SerializeField] private WaterDebugView debugView = WaterDebugView.Off;

        // ── static registry ─────────────────────────────────────────────────
        private static readonly List<WaterBody> _active = new();
        public static IReadOnlyList<WaterBody> Active => _active;

        /// <summary>The water body whose rectangle contains the position, or null.</summary>
        public static WaterBody FindAt(Vector3 worldPosition)
        {
            for (int i = 0; i < _active.Count; i++)
                if (_active[i]._canvas.Contains(worldPosition))
                    return _active[i];
            return null;
        }

        // ── runtime state ───────────────────────────────────────────────────
        private readonly WaterCanvas _canvas = new();
        private MeshFilter _meshFilter;
        private MeshRenderer _meshRenderer;
        private Mesh _autoQuad;
        private Material _material;
        private bool _ownsMaterial;

        private WaveSimulation _waveSim;
        private WakeTracker _wakeTracker;
        private BuoyancySolver _buoyancy;
        private PlanarReflection _reflection;

        private bool _lookDirty;
        private bool _structureDirty;
        private int _fingerprint;
        private Vector3 _lastPosition;
        private Quaternion _lastRotation;

        // ── public surface (used by gameplay + editor tooling) ──────────────
        public WaterCanvas Canvas => _canvas;
        public Material Material => _material;
        public WaterLook Look => look;
        public WaterWaveSettings Waves => waves;
        public WaterSurfaceSettings Surface => surface;
        public WaterShoreBakeSettings ShoreBake => shoreBake;
        public Camera SourceCamera => sourceCamera;

        public WaveSimulation WaveSim => _waveSim;
        public RenderTexture ReflectionTexture => _reflection?.Texture;
        public bool ShorelineAvailable =>
            surface.meshSource == WaterSurfaceSettings.MeshSource.Baked && surface.bakedMesh != null;

        public WaterBodyStats Stats => new()
        {
            WavesRunning = _waveSim != null,
            ReflectionRunning = _reflection != null,
            RegisteredDisturbers = WaterInteraction.Disturbers.Count,
            RegisteredFloaters = WaterInteraction.Floaters.Count,
            ActiveDisturbers = _wakeTracker?.ActiveCount ?? 0,
            SubmergedFloaters = _buoyancy?.SubmergedCount ?? 0,
            StampsLastFrame = _waveSim?.StampsLastFrame ?? 0,
            ReflectionRendersPer60Frames = _reflection?.RendersLastWindow ?? 0,
        };

        /// <summary>One-off splash (explosion, scripted event) at a world position.</summary>
        public void AddDisturbance(Vector3 worldPosition, float worldRadius, float strength)
            => _wakeTracker?.Emit(worldPosition, worldRadius, strength);

        /// <summary>Flattens the wave simulation.</summary>
        public void ClearWaves() => _waveSim?.Clear();

        /// <summary>Call after editing settings from code to push them to the material.</summary>
        public void SetLookDirty() => _lookDirty = true;

        /// <summary>Rebuilds mesh + material from the surface settings (used by the editor after a bake).</summary>
        public void RefreshSurface()
        {
            if (!isActiveAndEnabled) return;
            TearDownSurface();
            BuildSurface();
            ApplyLook();
        }

        // ── lifecycle ───────────────────────────────────────────────────────

        private void Reset()
        {
            surfaceShader = Shader.Find(WaterShaderIds.SurfaceShaderName);
            waveSimulationShader = Shader.Find(WaterShaderIds.SimulationShaderName);
            waveDisplayShader = Shader.Find(WaterShaderIds.DisplayShaderName);
        }

        private void OnEnable()
        {
            _active.Add(this);

            if (surfaceShader == null)
                surfaceShader = Shader.Find(WaterShaderIds.SurfaceShaderName);

            BuildSurface();
            SyncCanvas(force: true);
            ApplyLook();

            if (Application.isPlaying)
                BuildDynamics();
        }

        private void OnDisable()
        {
            TearDownDynamics();
            TearDownSurface();
            _active.Remove(this);
        }

        private void OnValidate()
        {
            surface.size.x = Mathf.Max(0.1f, surface.size.x);
            surface.size.y = Mathf.Max(0.1f, surface.size.y);
            waves.textureSize = Mathf.Clamp(Mathf.ClosestPowerOfTwo(waves.textureSize), 64, 2048);
            reflection.textureWidth = Mathf.Clamp(reflection.textureWidth, 16, 4096);

            _lookDirty = true;
            _structureDirty = _fingerprint != ComputeFingerprint();

#if UNITY_EDITOR
            // Edit mode gets no reliable Update tick — push the change now,
            // one editor tick later (material writes inside OnValidate itself
            // can warn).
            if (!Application.isPlaying)
            {
                UnityEditor.EditorApplication.delayCall += () =>
                {
                    if (this == null || Application.isPlaying) return;
                    SyncCanvas();
                    ApplyLookIfDirty();
                };
            }
#endif
        }

        private void Update()
        {
            if (!Application.isPlaying)
            {
                // Edit mode: keep the visual in sync while tweaking.
                SyncCanvas();
                ApplyLookIfDirty();
                return;
            }

            if (_structureDirty)
            {
                TearDownDynamics();
                BuildDynamics();
                _structureDirty = false;
            }

            ApplyLookIfDirty();
            _wakeTracker?.Tick();
            _waveSim?.Step(Time.deltaTime);
        }

        private void FixedUpdate()
        {
            if (!Application.isPlaying) return;
            _buoyancy?.Tick();
        }

        private void LateUpdate()
        {
            if (!Application.isPlaying) return;
            SyncCanvas();
            _reflection?.Tick();
        }

        // ── setup / teardown ────────────────────────────────────────────────

        private void BuildSurface()
        {
            _meshFilter = GetComponent<MeshFilter>();
            _meshRenderer = GetComponent<MeshRenderer>();

            var mesh = surface.ResolveMesh(autoQuad: null);
            if (mesh == null)
            {
                if (surface.meshSource != WaterSurfaceSettings.MeshSource.AutoQuad)
                    Debug.LogWarning($"[WaterSystemV2] '{name}': {surface.meshSource} mesh is missing — falling back to a flat quad.", this);

                if (_autoQuad == null)
                    _autoQuad = SurfaceMeshBuilder.CreateQuad(surface.size);
                mesh = _autoQuad;
            }
            _meshFilter.sharedMesh = mesh;

            _ownsMaterial = surface.materialOverride == null;
            if (_ownsMaterial)
            {
                if (surfaceShader == null)
                {
                    Debug.LogError($"[WaterSystemV2] '{name}': water surface shader not found. Is the shader file present and named '{WaterShaderIds.SurfaceShaderName}'?", this);
                    return;
                }
                _material = new Material(surfaceShader)
                {
                    name = $"WaterSurface ({name})",
                    hideFlags = HideFlags.HideAndDontSave,
                };
            }
            else
            {
                _material = surface.materialOverride;
            }

            _meshRenderer.sharedMaterial = _material;
        }

        private void TearDownSurface()
        {
            if (_ownsMaterial)
                WaterObjects.Destroy(_material);
            _material = null;

            WaterObjects.Destroy(_autoQuad);
            _autoQuad = null;
        }

        private void BuildDynamics()
        {
            if (_material == null) return;

            if (waves.enabled)
            {
                if (waveSimulationShader == null)
                    waveSimulationShader = Shader.Find(WaterShaderIds.SimulationShaderName);
                if (waveDisplayShader == null)
                    waveDisplayShader = Shader.Find(WaterShaderIds.DisplayShaderName);

                if (waveSimulationShader != null)
                {
                    _waveSim = new WaveSimulation(waves, waveSimulationShader, waveDisplayShader, _material);

                    if (waves.disturber.enabled)
                        _wakeTracker = new WakeTracker(_canvas, _waveSim, waves.disturber);
                }
                else
                {
                    Debug.LogError($"[WaterSystemV2] '{name}': wave simulation shader not found — waves disabled.", this);
                }
            }

            if (reflection.enabled)
            {
                var cam = sourceCamera != null ? sourceCamera : Camera.main;
                if (cam != null)
                {
                    sourceCamera = cam;
                    _reflection = new PlanarReflection(cam, _canvas, _material, reflection);
                }
                else
                {
                    Debug.LogWarning($"[WaterSystemV2] '{name}': no camera for reflection (assign one or tag a camera MainCamera).", this);
                }
            }
            SetReflectionKeyword(_reflection != null);

            if (buoyancy.enabled)
                _buoyancy = new BuoyancySolver(_canvas, buoyancy);

            _fingerprint = ComputeFingerprint();
        }

        private void TearDownDynamics()
        {
            _wakeTracker?.Dispose();
            _wakeTracker = null;

            _waveSim?.Dispose();
            _waveSim = null;

            _reflection?.Dispose();
            _reflection = null;

            _buoyancy?.Dispose();
            _buoyancy = null;

            if (_material != null)
                SetReflectionKeyword(false);
        }

        // ── helpers ─────────────────────────────────────────────────────────

        private void SyncCanvas(bool force = false)
        {
            transform.GetPositionAndRotation(out var pos, out var rot);
            if (!force && pos == _lastPosition && rot == _lastRotation
                && _canvas.Size == surface.size)
                return;

            _lastPosition = pos;
            _lastRotation = rot;
            _canvas.Position = pos;
            _canvas.Rotation = rot;
            _canvas.Size = surface.size;
        }

        private void ApplyLookIfDirty()
        {
            if (!_lookDirty) return;
            ApplyLook();
        }

        private void ApplyLook()
        {
            _lookDirty = false;
            if (_material == null) return;

            WaterMaterialBinder.Apply(_material, look, waves, ShorelineAvailable);
            WaterMaterialBinder.ApplyDebugView(_material, debugView);

            // Keep the reflection keyword owned by the dynamic state, not the look.
            if (Application.isPlaying)
                SetReflectionKeyword(_reflection != null);
            else
                SetReflectionKeyword(false);
        }

        private void SetReflectionKeyword(bool on)
        {
            if (_material == null) return;
            if (on) _material.EnableKeyword(WaterShaderIds.KeywordReflection);
            else _material.DisableKeyword(WaterShaderIds.KeywordReflection);
        }

        /// <summary>Settings whose change requires rebuilding the dynamic systems.</summary>
        private int ComputeFingerprint()
        {
            unchecked
            {
                int h = 17;
                h = h * 31 + (waves.enabled ? 1 : 0);
                h = h * 31 + waves.textureSize;
                h = h * 31 + waves.stampCapacity;
                h = h * 31 + (waves.disturber.enabled ? 1 : 0);
                h = h * 31 + (reflection.enabled ? 1 : 0);
                h = h * 31 + reflection.textureWidth;
                h = h * 31 + (buoyancy.enabled ? 1 : 0);
                return h;
            }
        }

#if UNITY_EDITOR
        private void OnDrawGizmos()
        {
            Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.color = new Color(0f, 0.8f, 1f, 0.9f);
            Gizmos.DrawWireCube(Vector3.zero, new Vector3(surface.size.x, 0f, surface.size.y));
        }
#endif
    }

    /// <summary>Live diagnostics shown in the WaterBody inspector.</summary>
    public struct WaterBodyStats
    {
        public bool WavesRunning;
        public bool ReflectionRunning;
        public int RegisteredDisturbers;
        public int RegisteredFloaters;
        public int ActiveDisturbers;
        public int SubmergedFloaters;
        public int StampsLastFrame;
        public int ReflectionRendersPer60Frames;
    }
}
