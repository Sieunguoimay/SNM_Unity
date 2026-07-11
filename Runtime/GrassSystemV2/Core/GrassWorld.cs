using System.Collections.Generic;
using UnityEngine;

namespace Snm.GrassSystemV2
{
    /// <summary>
    /// The single entry point of GrassSystemV2. One per scene.
    ///
    /// Owns the chunk set, the interaction canvas and the active render tier;
    /// updates everything from its own LateUpdate (no hidden updater objects).
    /// Gameplay talks to grass exclusively through this class:
    ///
    ///     GrassWorld.Instance.Cut(position, radius);
    ///     GrassWorld.Instance.ApplyEffect(GrassEffect.Burn, position, radius, 1f);
    ///     // trample: just add a GrassDisturber component to any moving object
    ///
    /// Setup: add this component, assign a GrassWorldData asset (the inspector
    /// can create one), paint with the Grass Painter tool. Done.
    /// </summary>
    [ExecuteAlways]
    [AddComponentMenu("Snm/Grass System V2/Grass World")]
    public class GrassWorld : MonoBehaviour
    {
        [Tooltip("Painted grass of this world. Create via the inspector button or the asset menu.")]
        [SerializeField] GrassWorldData data;

        [SerializeField] GrassWorldConfig config = new();

        [Tooltip("Culling compute shader (GrassV2Cull.compute). Auto-assigned in the editor.")]
        [SerializeField] ComputeShader cullShader;

        [Tooltip("Camera driving culling and the interaction canvas. Empty = Camera.main.")]
        [SerializeField] Camera cameraOverride;

        [Tooltip("Clear cut flags when the world (re)starts. Keep ON unless cuts should survive scene reloads.")]
        [SerializeField] bool clearCutsOnEnable = true;

        [Header("Debug")]
        [Tooltip("Draw chunk states and the canvas region as gizmos in the Scene view.")]
        public bool drawDebugOverlay;

        [Tooltip("Show the runtime stats panel (play mode).")]
        public bool showStatsPanel;

        [Tooltip("Draw a live arrow field showing procedural wind direction/strength over the ground.")]
        public bool drawWindField;

        public static GrassWorld Instance { get; private set; }

        public GrassWorldData Data => data;
        public GrassWorldConfig Config => config;
        public GrassStats Stats => _stats;
        public GrassInteractionCanvas Canvas => _canvas;
        public IReadOnlyList<GrassChunk> AllChunks => _chunks;

        static class Ids
        {
            public static readonly int WindGlobal = Shader.PropertyToID("_GrassWindGlobal");
            public static readonly int WindGlobal2 = Shader.PropertyToID("_GrassWindGlobal2");
            public static readonly int TintColor = Shader.PropertyToID("_GrassTintColor");
        }

        readonly List<GrassChunk> _chunks = new();
        readonly List<GrassChunk> _visibleChunks = new();
        readonly Plane[] _frustumPlanes = new Plane[6];

        GrassTypeMaterials _materials;
        IGrassRenderTier _tier;
        GrassInteractionCanvas _canvas;
        GrassStats _stats;
        Camera _cachedCamera;
        double _lastEditorTime;
        float _maxBladeHeight; // grass top = chunk root Y + this; drives the disturber height test

        void OnEnable()
        {
            if (Instance != null && Instance != this)
            {
                Debug.LogWarning($"[GrassV2] More than one GrassWorld active ('{Instance.name}' and '{name}'). " +
                                 "Only one per scene is supported; this one stays idle.", this);
                return;
            }
            Instance = this;

            if (data == null || data.types.Length == 0) return; // health check guides the user

            if (clearCutsOnEnable) data.ClearCutFlags();

            _materials = new GrassTypeMaterials(data.types, config);
            _canvas = new GrassInteractionCanvas(config);

            bool gpuDriven = config.UseGpuDrivenTier && cullShader != null;
            if (config.tierMode == GrassRenderTierMode.ForceGpuDriven && cullShader == null)
            {
                Debug.LogWarning("[GrassV2] GPU-driven tier forced but no cull compute shader assigned. " +
                                 "Falling back to Simple tier.", this);
            }
            _tier = gpuDriven
                ? new GrassGpuDrivenTier(data, _materials, cullShader, config)
                : new GrassSimpleTier(data, _materials);

            float maxBladeHeight = 0f;
            foreach (var type in data.types)
            {
                if (type != null) maxBladeHeight = Mathf.Max(maxBladeHeight, type.BladeHeight * type.scaleRange.y);
            }
            _maxBladeHeight = maxBladeHeight;
            foreach (var record in data.Chunks)
            {
                _chunks.Add(new GrassChunk(record, data.chunkSize, maxBladeHeight + 0.5f));
            }

#if UNITY_EDITOR
            UnityEditor.EditorApplication.update += EditorTick;
            _lastEditorTime = UnityEditor.EditorApplication.timeSinceStartup;
#endif
        }

        void OnDisable()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.update -= EditorTick;
#endif
            if (Instance == this) Instance = null;

            foreach (var chunk in _chunks) chunk.Dispose();
            _chunks.Clear();
            _visibleChunks.Clear();

            _tier?.Dispose();
            _tier = null;
            _canvas?.Dispose();
            _canvas = null;
            _materials?.Dispose();
            _materials = null;
        }

        void LateUpdate()
        {
            if (_tier == null) return;

            var camera = ResolveCamera();
            if (camera == null) return;

            float deltaTime = Application.isPlaying ? Time.deltaTime : EditorDeltaTime();

            // --- Chunk pass: cheap CPU work at chunk granularity only ---
            GeometryUtility.CalculateFrustumPlanes(camera, _frustumPlanes);
            var cameraPosition = camera.transform.position;
            float unloadDistance = config.maxDrawDistance + config.chunkSize * 2f; // hysteresis vs. load
            _visibleChunks.Clear();

            foreach (var chunk in _chunks)
            {
                chunk.DistanceToCamera = Mathf.Sqrt(
                    GrassGridMath.SqrDistanceToChunkXZ(cameraPosition, chunk.Coord, data.chunkSize));

                chunk.IsVisible = chunk.DistanceToCamera <= config.maxDrawDistance
                               && GeometryUtility.TestPlanesAABB(_frustumPlanes, chunk.WorldBounds);

                if (chunk.IsVisible)
                {
                    chunk.Upload(); // no-op when already resident
                    _visibleChunks.Add(chunk);
                }
                else if (chunk.InstanceBuffer != null && chunk.DistanceToCamera > unloadDistance)
                {
                    chunk.Dispose();
                }
            }

            // --- Interaction: disturbers stamp the bend canvas ---
            foreach (var disturber in GrassDisturber.ActiveDisturbers)
            {
                disturber.TrackMovement();
                var position = disturber.transform.position;

                // Sphere disturber: it reaches the grass when its bottom (center
                // minus radius) dips to or below the grass tops. Fully automatic —
                // grass height comes from the blades, vertical size from the radius.
                float grassTopY = SampleGroundY(position) + _maxBladeHeight;
                if (position.y - disturber.outerRadius > grassTopY) continue;

                var direction = disturber.MoveDirection;
                // The canvas works in a normalized 0..1 core fraction; convert
                // the disturber's two absolute radii here.
                float coreFraction = disturber.outerRadius > 0.0001f
                    ? Mathf.Clamp01(disturber.fullFlattenRadius / disturber.outerRadius)
                    : 0f;
                _canvas.QueueBend(position, new Vector2(direction.x, direction.z),
                    disturber.outerRadius, disturber.strength, coreFraction);
            }
            _canvas.Update(deltaTime, GetCameraFocus(camera));

            // --- Globals + draw ---
            float windRadians = config.windDirectionDegrees * Mathf.Deg2Rad;
            Shader.SetGlobalVector(Ids.WindGlobal, new Vector4(
                Mathf.Cos(windRadians), Mathf.Sin(windRadians), config.windSpeed, config.windNoiseScale));
            Shader.SetGlobalVector(Ids.WindGlobal2, new Vector4(
                config.windLean, config.windCoherence, 0f, 0f));
            Shader.SetGlobalColor(Ids.TintColor, config.tintColor);

            _stats = new GrassStats { TierName = _tier.Name };
            var context = new GrassFrameContext
            {
                Camera = camera,
                CameraPosition = cameraPosition,
                FrustumPlanes = _frustumPlanes,
                Config = config,
            };
            _tier.Render(_visibleChunks, context, ref _stats);

            if (drawDebugOverlay) GrassDebugOverlay.DrawBendField(this);
            if (drawWindField) GrassDebugOverlay.DrawWindField(this);

            CollectStats();
        }

        // ------------------------------------------------------------------
        // Public gameplay API
        // ------------------------------------------------------------------

        /// <summary>
        /// Permanently cuts all blades within a radius (attack swings, explosions,
        /// mowing). Persists in instance flags — unlike canvas effects it survives
        /// the camera moving far away.
        /// </summary>
        public void Cut(Vector3 worldPosition, float radius)
        {
            if (data == null) return;

            var touchedRecords = data.MarkCut(worldPosition, radius);
            if (touchedRecords.Count == 0) return;

            foreach (var chunk in _chunks)
            {
                if (chunk.InstanceBuffer != null && touchedRecords.Contains(chunk.Record))
                {
                    chunk.Refresh(); // partial world change -> partial re-upload, loaded chunks only
                }
            }
        }

        /// <summary>Stamps an area effect (burn / freeze / tint) onto the effects canvas.</summary>
        public void ApplyEffect(GrassEffect effect, Vector3 worldPosition, float radius, float strength = 1f)
        {
            _canvas?.QueueEffect(effect, worldPosition, radius, strength);
        }

        /// <summary>Stamps a one-off bend (shockwaves, landings) without a GrassDisturber.</summary>
        public void StampBend(Vector3 worldPosition, Vector2 direction, float radius, float strength = 1f)
        {
            _canvas?.QueueBend(worldPosition, direction, radius, strength);
        }

        /// <summary>
        /// Rebuilds runtime chunks from the data asset. Called by the editor
        /// tools after painting / filling / undo so changes show instantly.
        /// Cheap: GPU buffers re-upload lazily as chunks become visible.
        /// </summary>
        public void RebuildChunks()
        {
            if (data == null || _tier == null) return;

            foreach (var chunk in _chunks) chunk.Dispose();
            _chunks.Clear();
            _visibleChunks.Clear();

            float maxBladeHeight = 0f;
            foreach (var type in data.types)
            {
                if (type != null) maxBladeHeight = Mathf.Max(maxBladeHeight, type.BladeHeight * type.scaleRange.y);
            }
            _maxBladeHeight = maxBladeHeight;
            foreach (var record in data.Chunks)
            {
                _chunks.Add(new GrassChunk(record, data.chunkSize, maxBladeHeight + 0.5f));
            }
        }

        /// <summary>Full restart — used when data or types change structurally.</summary>
        public void Rebuild()
        {
            OnDisable();
            OnEnable();
        }

        // ------------------------------------------------------------------

        Camera ResolveCamera()
        {
#if UNITY_EDITOR
            // Edit mode: cull against the Scene view so painting far from the
            // game camera still shows grass where you are actually looking.
            if (!Application.isPlaying)
            {
                var sceneView = UnityEditor.SceneView.lastActiveSceneView;
                if (sceneView != null && sceneView.camera != null) return sceneView.camera;
            }
#endif
            if (cameraOverride != null) return cameraOverride;
            if (_cachedCamera == null) _cachedCamera = Camera.main;
            return _cachedCamera;
        }

        /// <summary>Point on the ground plane the camera is looking at — canvas center.</summary>
        static Vector3 GetCameraFocus(Camera camera)
        {
            var cameraTransform = camera.transform;
            var forward = cameraTransform.forward;
            if (forward.y < -0.05f)
            {
                float distance = Mathf.Min(-cameraTransform.position.y / forward.y, 100f);
                return cameraTransform.position + forward * distance;
            }
            return cameraTransform.position + forward * 10f;
        }

        /// <summary>Grass root height near a position — chunk vertical bounds are close enough.</summary>
        float SampleGroundY(Vector3 position)
        {
            var record = data.FindChunk(GrassGridMath.WorldToChunk(position, data.chunkSize));
            return record?.maxY ?? 0f;
        }

        void CollectStats()
        {
            _stats.VisibleChunks = _visibleChunks.Count;
            foreach (var chunk in _chunks)
            {
                _stats.TotalInstances += chunk.InstanceCount;
                if (chunk.InstanceBuffer != null)
                {
                    _stats.LoadedChunks++;
                    _stats.GpuBufferBytes += (long)chunk.InstanceCount * GrassInstance.Stride;
                }
            }
            if (_tier is GrassGpuDrivenTier gpuTier) _stats.GpuBufferBytes += gpuTier.GpuBufferBytes;
        }

        float EditorDeltaTime()
        {
#if UNITY_EDITOR
            double now = UnityEditor.EditorApplication.timeSinceStartup;
            float delta = Mathf.Clamp((float)(now - _lastEditorTime), 0.001f, 0.1f);
            _lastEditorTime = now;
            return delta;
#else
            return Time.deltaTime;
#endif
        }

#if UNITY_EDITOR
        // Keeps the player loop (and therefore LateUpdate + draws) ticking in
        // edit mode, so painting shows live grass without entering play mode.
        void EditorTick()
        {
            if (!Application.isPlaying) UnityEditor.EditorApplication.QueuePlayerLoopUpdate();
        }

        void OnValidate()
        {
            if (cullShader == null)
            {
                var guids = UnityEditor.AssetDatabase.FindAssets("GrassV2Cull t:ComputeShader");
                if (guids.Length > 0)
                {
                    cullShader = UnityEditor.AssetDatabase.LoadAssetAtPath<ComputeShader>(
                        UnityEditor.AssetDatabase.GUIDToAssetPath(guids[0]));
                }
            }
        }
#endif

        void OnGUI()
        {
            if (showStatsPanel) GrassDebugOverlay.DrawStatsPanel(_stats);
        }

        void OnDrawGizmos()
        {
            if (!drawDebugOverlay) return;
            GrassDebugOverlay.DrawChunkGizmos(this);
            GrassDebugOverlay.DrawDisturberGizmos(this);
        }
    }
}
