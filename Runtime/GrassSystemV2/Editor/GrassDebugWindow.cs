using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Snm.GrassSystemV2.Editor
{
    /// <summary>
    /// One-stop dashboard for GrassSystemV2: health check, live stats, type and
    /// chunk breakdowns, and live previews of the interaction canvas textures
    /// (bend / burn / freeze / tint). Open via Tools > Snm > Grass System V2.
    /// </summary>
    public class GrassDebugWindow : EditorWindow
    {
        const int MaxChunkRows = 40;
        const double HealthRefreshSeconds = 2.0;

        [MenuItem("Tools/Snm/Grass System V2")]
        static void Open() => GetWindow<GrassDebugWindow>("Grass V2");

        Vector2 _scroll;
        float _previewSize = 256f;
        bool _showBendAlpha;
        bool _foldHealth = true;
        bool _foldStats = true;
        bool _foldConfig;
        bool _foldTypes = true;
        bool _foldChunks;
        bool _foldCanvas = true;

        List<GrassHealthCheck.Issue> _healthIssues;
        double _nextHealthTime;
        readonly List<GrassChunk> _chunkRows = new();

        void OnInspectorUpdate() => Repaint(); // ~10 Hz keeps stats and textures live

        void OnGUI()
        {
            var world = GrassWorld.Instance != null
                ? GrassWorld.Instance
                : FindFirstObjectByType<GrassWorld>();

            if (world == null)
            {
                EditorGUILayout.HelpBox(
                    "No GrassWorld in the open scene. Add one (Snm > Grass System V2 > Grass World) to get started.",
                    MessageType.Info);
                return;
            }

            _scroll = EditorGUILayout.BeginScrollView(_scroll);

            DrawHeader(world);
            DrawHealth(world);
            DrawLiveStats(world);
            DrawConfig(world.Config);
            DrawTypes(world.Data);
            DrawChunks(world);
            DrawCanvas(world);

            EditorGUILayout.EndScrollView();
        }

        // ------------------------------------------------------------------

        void DrawHeader(GrassWorld world)
        {
            EditorGUILayout.BeginHorizontal();
            using (new EditorGUI.DisabledScope(true))
            {
                EditorGUILayout.ObjectField(world, typeof(GrassWorld), true);
            }
            if (GUILayout.Button("Select", GUILayout.Width(60f)))
            {
                Selection.activeObject = world.gameObject;
                EditorGUIUtility.PingObject(world.gameObject);
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            EditorGUI.BeginChangeCheck();
            world.drawDebugOverlay = GUILayout.Toggle(world.drawDebugOverlay, "Scene Overlay", "Button");
            world.drawWindField = GUILayout.Toggle(world.drawWindField, "Wind Field", "Button");
            world.showStatsPanel = GUILayout.Toggle(world.showStatsPanel, "Stats Panel", "Button");
            if (EditorGUI.EndChangeCheck()) SceneView.RepaintAll();
            if (GUILayout.Button("Rebuild World")) world.Rebuild();
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.Space(4f);
        }

        void DrawHealth(GrassWorld world)
        {
            _foldHealth = EditorGUILayout.Foldout(_foldHealth, "Health Check", true, EditorStyles.foldoutHeader);
            if (!_foldHealth) return;

            if (_healthIssues == null || EditorApplication.timeSinceStartup >= _nextHealthTime)
            {
                _healthIssues = GrassHealthCheck.Run(world);
                _nextHealthTime = EditorApplication.timeSinceStartup + HealthRefreshSeconds;
            }

            if (_healthIssues.Count == 0)
            {
                EditorGUILayout.HelpBox("All good — no configuration issues found.", MessageType.Info);
                return;
            }

            foreach (var issue in _healthIssues)
            {
                var messageType = issue.Severity switch
                {
                    GrassHealthCheck.Severity.Error => MessageType.Error,
                    GrassHealthCheck.Severity.Warning => MessageType.Warning,
                    _ => MessageType.Info,
                };
                EditorGUILayout.HelpBox(issue.Message, messageType);
                if (issue.Fix != null && GUILayout.Button(issue.FixLabel))
                {
                    issue.Fix();
                    _healthIssues = null; // re-run next repaint
                    GUIUtility.ExitGUI();
                }
            }
        }

        void DrawLiveStats(GrassWorld world)
        {
            _foldStats = EditorGUILayout.Foldout(_foldStats, "Live Stats", true, EditorStyles.foldoutHeader);
            if (!_foldStats) return;

            var stats = world.Stats;
            Row("Mode", Application.isPlaying ? "Play" : "Edit");
            Row("Tier", string.IsNullOrEmpty(stats.TierName) ? "(not running)" : stats.TierName);
            Row("Blades", $"{stats.VisibleInstances:N0} visible / {stats.TotalInstances:N0} total");
            Row("Chunks", $"{stats.VisibleChunks} visible · {stats.LoadedChunks} resident · {world.AllChunks.Count} painted");
            Row("Draw calls", stats.DrawCalls.ToString());
            Row("GPU memory", $"{stats.GpuBufferBytes / (1024f * 1024f):F1} MB");
            Row("Disturbers", GrassDisturber.ActiveDisturbers.Count.ToString());
            EditorGUILayout.Space(4f);
        }

        void DrawConfig(GrassWorldConfig config)
        {
            _foldConfig = EditorGUILayout.Foldout(_foldConfig, "Config Summary", true, EditorStyles.foldoutHeader);
            if (!_foldConfig) return;

            Row("Tier mode", $"{config.tierMode} (resolves to {(config.UseGpuDrivenTier ? "GPU-Driven" : "Simple")})");
            Row("Chunk size", $"{config.chunkSize} m");
            Row("Draw distance", $"{config.maxDrawDistance} m (thinning from {config.densityFalloffStart} m, LOD at {config.lodDistance} m)");
            Row("Visible budget", $"{config.maxVisibleInstancesPerType:N0} per type (GPU tier)");
            Row("Canvas", $"{config.canvasResolution}px over {config.canvasWorldSize} m");
            Row("Wind", $"{config.windDirectionDegrees}° · speed {config.windSpeed} · noise {config.windNoiseScale}");
            Row("Freeze thaw", config.freezeThawTime > 0f ? $"{config.freezeThawTime}s" : "permanent inside canvas");
            EditorGUILayout.Space(4f);
        }

        void DrawTypes(GrassWorldData data)
        {
            _foldTypes = EditorGUILayout.Foldout(_foldTypes, "Grass Types", true, EditorStyles.foldoutHeader);
            if (!_foldTypes) return;

            if (data == null || data.types.Length == 0)
            {
                EditorGUILayout.HelpBox("No data / no types assigned.", MessageType.Warning);
                return;
            }

            var bladeCounts = new int[data.types.Length];
            foreach (var chunk in data.Chunks)
            {
                foreach (var range in chunk.typeRanges)
                {
                    if (range.typeIndex < bladeCounts.Length) bladeCounts[range.typeIndex] += range.count;
                }
            }

            for (int i = 0; i < data.types.Length; i++)
            {
                var type = data.types[i];
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField($"[{i}]", GUILayout.Width(28f));
                using (new EditorGUI.DisabledScope(true))
                {
                    EditorGUILayout.ObjectField(type, typeof(GrassType), false);
                }
                if (type != null)
                {
                    EditorGUILayout.LabelField($"{bladeCounts[i]:N0} blades", GUILayout.Width(110f));
                    EditorGUILayout.LabelField(type.HasLod1 ? "LOD0+1" : "LOD0", GUILayout.Width(60f));
                    EditorGUILayout.LabelField(type.IsValid ? "OK" : "INVALID", GUILayout.Width(60f));
                }
                EditorGUILayout.EndHorizontal();
            }
            EditorGUILayout.Space(4f);
        }

        void DrawChunks(GrassWorld world)
        {
            _foldChunks = EditorGUILayout.Foldout(
                _foldChunks, $"Chunks ({world.AllChunks.Count})", true, EditorStyles.foldoutHeader);
            if (!_foldChunks) return;

            _chunkRows.Clear();
            _chunkRows.AddRange(world.AllChunks);
            _chunkRows.Sort((a, b) => a.DistanceToCamera.CompareTo(b.DistanceToCamera));

            int shown = Mathf.Min(_chunkRows.Count, MaxChunkRows);
            for (int i = 0; i < shown; i++)
            {
                var chunk = _chunkRows[i];
                string state = chunk.IsVisible ? "VISIBLE"
                             : chunk.InstanceBuffer != null ? "resident"
                             : "unloaded";
                EditorGUILayout.LabelField(
                    $"({chunk.Coord.x,3},{chunk.Coord.y,3})   {state,-8}   {chunk.InstanceCount:N0} blades   {chunk.DistanceToCamera:F0} m");
            }
            if (_chunkRows.Count > shown)
            {
                EditorGUILayout.LabelField($"… {_chunkRows.Count - shown} more (sorted by distance, nearest first)");
            }
            EditorGUILayout.Space(4f);
        }

        void DrawCanvas(GrassWorld world)
        {
            _foldCanvas = EditorGUILayout.Foldout(_foldCanvas, "Interaction Canvas", true, EditorStyles.foldoutHeader);
            if (!_foldCanvas) return;

            var canvas = world.Canvas;
            if (canvas == null || canvas.BendTexture == null)
            {
                EditorGUILayout.HelpBox(
                    "Canvas not running — the world is idle (no data, no types, or disabled).",
                    MessageType.Warning);
                return;
            }

            Row("Covers", $"{canvas.WorldSize} m square from ({canvas.WorldMin.x:F1}, {canvas.WorldMin.y:F1})");
            Row("Limits", "max 64 stamps per frame; effects outside the square are forgotten (cuts persist)");

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Test stamps (canvas center):", GUILayout.Width(170f));
            var center = new Vector3(
                canvas.WorldMin.x + canvas.WorldSize * 0.5f, 0f,
                canvas.WorldMin.y + canvas.WorldSize * 0.5f);
            if (GUILayout.Button("Bend")) world.StampBend(center, Vector2.right, 3f, 1f);
            if (GUILayout.Button("Burn")) world.ApplyEffect(GrassEffect.Burn, center, 3f);
            if (GUILayout.Button("Freeze")) world.ApplyEffect(GrassEffect.Freeze, center, 3f);
            if (GUILayout.Button("Tint")) world.ApplyEffect(GrassEffect.Tint, center, 3f);
            EditorGUILayout.EndHorizontal();

            _previewSize = EditorGUILayout.Slider("Preview size", _previewSize, 128f, 512f);
            _showBendAlpha = EditorGUILayout.ToggleLeft("Show bend alpha (spring energy)", _showBendAlpha);

            EditorGUILayout.BeginHorizontal();
            DrawTexturePreview("Bend — R/G push dir · B hold", canvas.BendTexture, false);
            DrawTexturePreview("Effects — R burn · G freeze · B tint", canvas.EffectsTexture, false);
            EditorGUILayout.EndHorizontal();

            if (_showBendAlpha)
            {
                EditorGUILayout.BeginHorizontal();
                DrawTexturePreview("Bend alpha — spring energy", canvas.BendTexture, true);
                EditorGUILayout.EndHorizontal();
            }
        }

        void DrawTexturePreview(string label, Texture texture, bool alphaOnly)
        {
            EditorGUILayout.BeginVertical(GUILayout.Width(_previewSize));
            EditorGUILayout.LabelField(label, EditorStyles.miniBoldLabel, GUILayout.Width(_previewSize));
            var rect = GUILayoutUtility.GetRect(
                _previewSize, _previewSize, GUILayout.ExpandWidth(false), GUILayout.ExpandHeight(false));
            EditorGUI.DrawRect(rect, new Color(0.12f, 0.12f, 0.12f));
            if (alphaOnly) EditorGUI.DrawTextureAlpha(rect, texture, ScaleMode.ScaleToFit);
            else EditorGUI.DrawPreviewTexture(rect, texture, null, ScaleMode.ScaleToFit);
            EditorGUILayout.EndVertical();
        }

        static void Row(string label, string value)
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(label, GUILayout.Width(110f));
            EditorGUILayout.LabelField(value, EditorStyles.boldLabel);
            EditorGUILayout.EndHorizontal();
        }
    }
}
