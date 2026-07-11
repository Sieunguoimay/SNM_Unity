using UnityEditor;
using UnityEditor.EditorTools;
using UnityEngine;

namespace Snm.GrassSystemV2.Editor
{
    /// <summary>
    /// GrassWorld inspector: health checks with one-click fixes on top,
    /// then the normal fields, then live stats and maintenance buttons.
    /// </summary>
    [CustomEditor(typeof(GrassWorld))]
    public class GrassWorldEditor : UnityEditor.Editor
    {
        void OnEnable()
        {
            Undo.undoRedoPerformed += GrassPaintOps.RefreshWorld;
        }

        void OnDisable()
        {
            Undo.undoRedoPerformed -= GrassPaintOps.RefreshWorld;
        }

        public override bool RequiresConstantRepaint() => Application.isPlaying;

        public override void OnInspectorGUI()
        {
            var world = (GrassWorld)target;

            DrawHealthChecks(world);
            EditorGUILayout.Space();

            if (GUILayout.Button("Open Grass Painter", GUILayout.Height(28f)))
            {
                ToolManager.SetActiveTool<GrassPainterTool>();
            }

            DrawWindPresets(world);

            EditorGUILayout.Space();
            DrawDefaultInspector();
            EditorGUILayout.Space();

            DrawStats(world);
            DrawMaintenance(world);
        }

        static void DrawWindPresets(GrassWorld world)
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Wind preset", EditorStyles.miniBoldLabel);
            EditorGUILayout.BeginHorizontal();
            foreach (var preset in GrassWindPresets.All)
            {
                if (GUILayout.Button(preset.Name))
                {
                    Undo.RecordObject(world, $"Apply wind preset '{preset.Name}'");
                    GrassWindPresets.Apply(world.Config, preset);
                    EditorUtility.SetDirty(world);
                    SceneView.RepaintAll();
                }
            }
            EditorGUILayout.EndHorizontal();
        }

        static void DrawHealthChecks(GrassWorld world)
        {
            var issues = GrassHealthCheck.Run(world);
            if (issues.Count == 0)
            {
                EditorGUILayout.HelpBox("All checks passed.", MessageType.None);
                return;
            }

            foreach (var issue in issues)
            {
                var messageType = issue.Severity switch
                {
                    GrassHealthCheck.Severity.Error => MessageType.Error,
                    GrassHealthCheck.Severity.Warning => MessageType.Warning,
                    _ => MessageType.Info,
                };

                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.HelpBox(issue.Message, messageType);
                if (issue.Fix != null && GUILayout.Button(issue.FixLabel, GUILayout.Width(130f), GUILayout.Height(38f)))
                {
                    issue.Fix();
                }
                EditorGUILayout.EndHorizontal();
            }
        }

        static void DrawStats(GrassWorld world)
        {
            if (world.Data == null) return;

            EditorGUILayout.LabelField("Content", EditorStyles.boldLabel);
            EditorGUILayout.LabelField($"Painted blades: {world.Data.TotalInstanceCount:N0}   ·   Chunks: {world.Data.Chunks.Count}");

            if (Application.isPlaying)
            {
                var stats = world.Stats;
                EditorGUILayout.LabelField(
                    $"Live ({stats.TierName}): {stats.VisibleInstances:N0} visible · " +
                    $"{stats.VisibleChunks} chunks · {stats.DrawCalls} draws · " +
                    $"{stats.GpuBufferBytes / (1024f * 1024f):F1} MB GPU");
            }
        }

        static void DrawMaintenance(GrassWorld world)
        {
            if (world.Data == null) return;

            EditorGUILayout.Space();
            EditorGUILayout.BeginHorizontal();

            if (GUILayout.Button("Clear ALL grass"))
            {
                if (EditorUtility.DisplayDialog(
                        "Clear all grass",
                        $"Delete every painted blade ({world.Data.TotalInstanceCount:N0}) in '{world.Data.name}'?",
                        "Delete", "Cancel"))
                {
                    GrassPaintOps.WithUndo(world.Data, "Clear all grass",
                        () => world.Data.RemoveInRadius(Vector3.zero, float.MaxValue));
                }
            }

            if (GUILayout.Button("Rebuild"))
            {
                world.Rebuild();
                SceneView.RepaintAll();
            }

            EditorGUILayout.EndHorizontal();
        }
    }
}
