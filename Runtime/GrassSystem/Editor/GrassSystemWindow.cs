#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace Snm.GrassSystem.Editor
{
    public class GrassSystemWindow : EditorWindow
    {
        GrassSystem _system;
        Vector2 _scroll;
        bool _showDisturbers = true;

        [MenuItem("Tools/Snm/Grass System")]
        static void Open() => GetWindow<GrassSystemWindow>("Grass System");

        void OnEnable()
        {
            EditorApplication.update += Repaint;
        }

        void OnDisable()
        {
            EditorApplication.update -= Repaint;
        }

        void OnGUI()
        {
            _system = FindSystem();

            if (_system == null)
            {
                EditorGUILayout.HelpBox("No GrassSystem found in scene.", MessageType.Info);
                return;
            }

            _scroll = EditorGUILayout.BeginScrollView(_scroll);

            DrawSystemInfo();
            DrawWindInfo();
            DrawTrampleInfo();
            DrawDisturbers();

            EditorGUILayout.EndScrollView();
        }

        void DrawSystemInfo()
        {
            EditorGUILayout.LabelField("System", EditorStyles.boldLabel);
            var cfg = _system.Config;
            EditorGUILayout.LabelField("Grid Size", $"{cfg.gridSize.x} x {cfg.gridSize.y}");
            EditorGUILayout.LabelField("Cell Spacing", $"{cfg.cellSpacing.x} x {cfg.cellSpacing.y}");
            EditorGUILayout.LabelField("Instances", _system.InstanceCount.ToString());

            var canvas = _system.Canvas;
            if (canvas != null)
            {
                var min = canvas.WorldMin;
                var size = canvas.Size;
                EditorGUILayout.LabelField("Canvas", $"({min.x:F1}, {min.y:F1}) size ({size.x:F1}, {size.y:F1})");
            }

            if (cfg.grassMesh != null)
                EditorGUILayout.LabelField("Mesh", $"{cfg.grassMesh.name} ({cfg.grassMesh.vertexCount} verts)");
            if (cfg.grassMaterial != null)
                EditorGUILayout.LabelField("Material", cfg.grassMaterial.name);

            EditorGUILayout.Space();
        }

        void DrawWindInfo()
        {
            EditorGUILayout.LabelField("Wind", EditorStyles.boldLabel);
            var cfg = _system.Config;
            EditorGUILayout.LabelField("Strength", cfg.windStrength.ToString("F2"));
            EditorGUILayout.LabelField("Scroll Speed", cfg.windScrollSpeed.ToString("F3"));
            EditorGUILayout.LabelField("Map Scale", $"{cfg.windMapScale.x} x {cfg.windMapScale.y}");

            if (cfg.windMap != null)
            {
                EditorGUILayout.LabelField("DUDV Map");
                var rect = GUILayoutUtility.GetRect(128, 128, GUILayout.ExpandWidth(false));
                EditorGUI.DrawPreviewTexture(rect, cfg.windMap);
            }

            EditorGUILayout.Space();
        }

        void DrawTrampleInfo()
        {
            EditorGUILayout.LabelField("Trample", EditorStyles.boldLabel);
            var cfg = _system.Config;
            EditorGUILayout.LabelField("Enabled", cfg.trampleEnabled.ToString());
            EditorGUILayout.LabelField("Fade Speed", cfg.trampleFadeSpeed.ToString("F2"));
            EditorGUILayout.LabelField("Resolution", cfg.trampleResolution.ToString());

            if (Application.isPlaying && _system.Trample != null)
            {
                var rt = _system.Trample.OutputTexture;
                if (rt != null)
                {
                    EditorGUILayout.LabelField("Live Trample RT");
                    var rect = GUILayoutUtility.GetRect(128, 128, GUILayout.ExpandWidth(false));
                    EditorGUI.DrawPreviewTexture(rect, rt);
                }
            }

            EditorGUILayout.Space();
        }

        void DrawDisturbers()
        {
            _showDisturbers = EditorGUILayout.Foldout(_showDisturbers, "Disturbers", true);
            if (!_showDisturbers) return;

            if (!Application.isPlaying || _system.Trample == null)
            {
                EditorGUILayout.LabelField("(Play mode only)");
                return;
            }

            var snapshots = _system.Trample.GetDisturberSnapshots();
            EditorGUILayout.LabelField("Count", snapshots.Length.ToString());

            EditorGUI.indentLevel++;
            for (int i = 0; i < snapshots.Length; i++)
            {
                var s = snapshots[i];
                EditorGUILayout.LabelField($"[{i}]",
                    $"pos=({s.Position.x:F1},{s.Position.y:F1},{s.Position.z:F1}) " +
                    $"dir=({s.Direction.x:F2},{s.Direction.z:F2}) " +
                    $"r={s.Radius:F2} " +
                    $"{(s.InCanvas ? "IN" : "OUT")}");
            }
            EditorGUI.indentLevel--;
        }

        static GrassSystem FindSystem()
        {
            return Object.FindAnyObjectByType<GrassSystem>();
        }
    }
}
#endif
