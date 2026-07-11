using System.IO;
using UnityEditor;
using UnityEngine;

namespace Snm.WaterSystemV2.Editor
{
    /// <summary>
    /// Inspector-as-diagnostics for <see cref="WaterBody"/>: a live status
    /// panel (bake state, camera, running systems, per-frame stats), the
    /// Scan/Bake shoreline workflow, and wave/reflection texture previews.
    /// Everything you need to answer "why does my water look wrong?" without
    /// leaving the inspector.
    /// </summary>
    [CustomEditor(typeof(WaterBody))]
    public class WaterBodyEditor : UnityEditor.Editor
    {
        private const string BakeOutputFolder = "Assets/Generated/WaterMeshes";

        private bool _showPreviews = true;

        private void OnEnable()
        {
            Debug.Log($"[WBE-DIAG] Editor.OnEnable  playing={Application.isPlaying}  target={(target ? target.name : "NULL")}");
        }

        private void OnDisable()
        {
            Debug.Log($"[WBE-DIAG] Editor.OnDisable playing={Application.isPlaying}  target={(target ? target.name : "NULL")}");
        }

        // NOTE(diag): Application.isPlaying here made the IMGUI body render
        // blank in play mode (Unity 6 IMGUIContainer-in-UIToolkit repaint bug).
        // Forcing false to confirm the cause; live stats then update on hover.
        public override bool RequiresConstantRepaint() => false;

        public override void OnInspectorGUI()
        {
            var water = (WaterBody)target;

            EditorGUILayout.LabelField($"WaterBodyEditor ✓ (playing={Application.isPlaying})", EditorStyles.miniBoldLabel);

            try
            {
                DrawStatusPanel(water);
                EditorGUILayout.Space();

                DrawDefaultInspector();

                EditorGUILayout.Space();
                DrawShoreBakeSection(water);

                if (Application.isPlaying)
                {
                    EditorGUILayout.Space();
                    DrawRuntimeSection(water);
                }
            }
            catch (System.Exception e) when (!(e is ExitGUIException))
            {
                // A diagnostics inspector must never blank out. Surface the
                // cause instead of leaving an empty component body, and log it
                // once per repaint so the real exception is finally visible.
                EditorGUILayout.HelpBox("Inspector draw error: " + e.Message, MessageType.Error);
                if (Event.current.type == EventType.Repaint)
                    Debug.LogException(e, water);
            }
        }

        // ── status panel ───────────────────────────────────────────────────

        private void DrawStatusPanel(WaterBody water)
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("Status", EditorStyles.boldLabel);

            // Shoreline / bake state
            bool wantsShoreline = water.Look.shoreline.enabled;
            if (water.ShorelineAvailable)
            {
                StatusLine(true, $"Shore mesh baked ({water.Surface.bakedMesh.vertexCount} verts).");
            }
            else if (wantsShoreline)
            {
                StatusLine(false, "Shoreline is ON but no baked mesh is active — it stays hidden until you bake (safe fallback, not an error).");
            }
            else
            {
                StatusLine(true, "No baked shore mesh (shoreline off — fine for open water).");
            }

            // Camera
            var cam = water.SourceCamera != null ? water.SourceCamera : Camera.main;
            if (cam != null)
                StatusLine(true, $"Reflection camera source: {cam.name}.");
            else
                StatusLine(false, "No camera found (assign Source Camera or tag one MainCamera) — reflection will not run.");

            // Shaders
            if (Shader.Find(WaterShaderIds.SurfaceShaderName) == null)
                StatusLine(false, $"Surface shader '{WaterShaderIds.SurfaceShaderName}' not found — did the Shaders folder move?");

            if (Application.isPlaying)
            {
                var stats = water.Stats;
                EditorGUILayout.LabelField(
                    $"Waves {(stats.WavesRunning ? "on" : "off")} · " +
                    $"Reflection {(stats.ReflectionRunning ? "on" : "off")} · " +
                    $"Disturbers {stats.ActiveDisturbers}/{stats.RegisteredDisturbers} · " +
                    $"Floaters {stats.SubmergedFloaters}/{stats.RegisteredFloaters}");
                EditorGUILayout.LabelField(
                    $"Stamps last frame: {stats.StampsLastFrame} · " +
                    $"Reflection renders / 60 frames: {stats.ReflectionRendersPer60Frames}");
            }

            EditorGUILayout.EndVertical();
        }

        private static void StatusLine(bool ok, string message)
        {
            EditorGUILayout.LabelField((ok ? "✓  " : "✗  ") + message, EditorStyles.wordWrappedLabel);
        }

        // ── shoreline bake ─────────────────────────────────────────────────

        private void DrawShoreBakeSection(WaterBody water)
        {
            EditorGUILayout.LabelField("Shoreline Bake", EditorStyles.boldLabel);

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Scan Terrain Objects"))
                    ScanTerrainObjects(water);

                using (new EditorGUI.DisabledScope(water.ShoreBake.terrainObjects.Count == 0))
                {
                    if (GUILayout.Button("Bake Shore Mesh"))
                        BakeShoreMesh(water);
                }
            }

            if (water.ShoreBake.terrainObjects.Count == 0)
                EditorGUILayout.HelpBox("Scan finds every mesh overlapping the water rectangle and fills the Terrain Objects list. Review the list, then bake.", MessageType.Info);
        }

        /// <summary>Fills the terrain list with every mesh whose bounds cross the water rect.</summary>
        private void ScanTerrainObjects(WaterBody water)
        {
            var canvas = water.Canvas;
            var size = water.Surface.size;
            var list = water.ShoreBake.terrainObjects;

            Undo.RecordObject(water, "Scan Terrain Objects");
            list.Clear();

            foreach (var meshFilter in FindObjectsByType<MeshFilter>(FindObjectsSortMode.None))
            {
                if (meshFilter.sharedMesh == null) continue;
                if (meshFilter.GetComponentInParent<WaterBody>() != null) continue;

                var bounds = meshFilter.TryGetComponent<Renderer>(out var renderer)
                    ? renderer.bounds
                    : new Bounds(meshFilter.transform.position, Vector3.one);

                float radius = Mathf.Max(bounds.extents.x, bounds.extents.z);
                if (!canvas.Overlaps(bounds.center, radius)) continue;

                // Only meshes that vertically straddle the water plane can form a shore.
                if (bounds.min.y > canvas.SurfaceY || bounds.max.y < canvas.SurfaceY) continue;

                if (!list.Contains(meshFilter.gameObject))
                    list.Add(meshFilter.gameObject);
            }

            EditorUtility.SetDirty(water);
            Debug.Log($"[WaterSystemV2] Scan found {list.Count} terrain object(s) crossing the water plane.", water);
        }

        private void BakeShoreMesh(WaterBody water)
        {
            var bake = water.ShoreBake;
            var result = WaterShoreBaker.Generate(new WaterShoreBaker.Input
            {
                WaterTransform = water.transform,
                TerrainObjects = bake.terrainObjects,
                WaterSize = water.Surface.size,
                GridCellSize = bake.gridCellSize,
                MaxShoreDistance = bake.maxShoreDistance,
                AlongShoreTiling = bake.alongShoreTiling,
            });

            if (result.Mesh == null || result.VertexCount == 0)
            {
                EditorUtility.DisplayDialog("Bake Shore Mesh", $"Bake produced no mesh.\n\n{result.Log}", "OK");
                return;
            }

            var mesh = SaveMeshAsset(result.Mesh, BuildAssetPath(water));

            Undo.RecordObject(water, "Bake Shore Mesh");
            water.Surface.bakedMesh = mesh;
            water.Surface.meshSource = WaterSurfaceSettings.MeshSource.Baked;
            EditorUtility.SetDirty(water);

            water.RefreshSurface();
            Debug.Log($"[WaterSystemV2] Shore mesh baked → {AssetDatabase.GetAssetPath(mesh)}\n{result.Log}", water);
        }

        private static string BuildAssetPath(WaterBody water)
        {
            string sceneName = water.gameObject.scene.IsValid() && !string.IsNullOrEmpty(water.gameObject.scene.name)
                ? water.gameObject.scene.name
                : "Scene";
            return $"{BakeOutputFolder}/{sceneName}_{water.name}_V2.asset";
        }

        /// <summary>Writes the mesh into the asset at path, preserving the existing asset's GUID/references.</summary>
        private static Mesh SaveMeshAsset(Mesh mesh, string path)
        {
            if (!Directory.Exists(BakeOutputFolder))
            {
                Directory.CreateDirectory(BakeOutputFolder);
                AssetDatabase.Refresh();
            }

            var existing = AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if (existing != null)
            {
                existing.Clear();
                EditorUtility.CopySerialized(mesh, existing);
                AssetDatabase.SaveAssets();
                return existing;
            }

            AssetDatabase.CreateAsset(mesh, path);
            AssetDatabase.SaveAssets();
            return mesh;
        }

        // ── runtime tools ──────────────────────────────────────────────────

        private void DrawRuntimeSection(WaterBody water)
        {
            EditorGUILayout.LabelField("Runtime", EditorStyles.boldLabel);

            using (new EditorGUILayout.HorizontalScope())
            {
                using (new EditorGUI.DisabledScope(water.WaveSim == null))
                {
                    if (GUILayout.Button("Clear Waves"))
                        water.ClearWaves();

                    if (GUILayout.Button("Test Splash"))
                        water.AddDisturbance(water.transform.position, 0.5f, 1f);
                }
            }

            _showPreviews = EditorGUILayout.Foldout(_showPreviews, "Texture Previews", toggleOnLabelClick: true);
            if (!_showPreviews) return;

            if (water.WaveSim != null)
                DrawPreview("Wave heightfield", water.WaveSim.RenderDebugPreview(0));

            if (water.ReflectionTexture != null)
                DrawPreview("Reflection", water.ReflectionTexture);
        }

        private static void DrawPreview(string label, Texture texture)
        {
            EditorGUILayout.LabelField(label);
            float aspect = texture.height / (float)texture.width;
            var rect = GUILayoutUtility.GetRect(128, 128 * aspect, GUILayout.MaxWidth(256), GUILayout.MaxHeight(256 * aspect));
            EditorGUI.DrawPreviewTexture(rect, texture, null, ScaleMode.ScaleToFit);
        }
    }
}
