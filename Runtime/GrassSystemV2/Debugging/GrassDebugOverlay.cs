using UnityEngine;

namespace Snm.GrassSystemV2
{
    /// <summary>
    /// Visual debugging for GrassSystemV2 — everything is shown on the field,
    /// not as log lines. Toggled from the GrassWorld inspector:
    ///
    ///   Chunk gizmos — green = visible/drawn, yellow = resident but culled,
    ///                  gray = unloaded (no GPU memory), with instance counts.
    ///   Stats panel  — small IMGUI box with live counters (play mode).
    /// </summary>
    public static class GrassDebugOverlay
    {
        static readonly Color VisibleColor = new(0.25f, 0.9f, 0.3f, 0.9f);
        static readonly Color LoadedColor = new(0.95f, 0.85f, 0.2f, 0.7f);
        static readonly Color UnloadedColor = new(0.5f, 0.5f, 0.5f, 0.35f);
        static readonly Color CanvasColor = new(0.3f, 0.6f, 1f, 0.9f);

        public static void DrawChunkGizmos(GrassWorld world)
        {
            if (world.Data == null) return;

            foreach (var chunk in world.AllChunks)
            {
                Gizmos.color = chunk.IsVisible ? VisibleColor
                             : chunk.InstanceBuffer != null ? LoadedColor
                             : UnloadedColor;
                Gizmos.DrawWireCube(chunk.WorldBounds.center, chunk.WorldBounds.size);

#if UNITY_EDITOR
                if (chunk.IsVisible)
                {
                    UnityEditor.Handles.color = VisibleColor;
                    UnityEditor.Handles.Label(
                        chunk.WorldBounds.center,
                        $"{chunk.Coord.x},{chunk.Coord.y}\n{chunk.InstanceCount}");
                }
#endif
            }

            // Interaction canvas coverage — anything outside this square
            // cannot bend or receive effects (cuts still work everywhere).
            var canvas = world.Canvas;
            if (canvas != null)
            {
                Gizmos.color = CanvasColor;
                var min = canvas.WorldMin;
                float size = canvas.WorldSize;
                var center = new Vector3(min.x + size * 0.5f, 0.05f, min.y + size * 0.5f);
                Gizmos.DrawWireCube(center, new Vector3(size, 0.1f, size));
            }
        }

        public static void DrawStatsPanel(GrassStats stats)
        {
            const float width = 260f;
            const float height = 148f;
            var rect = new Rect(10f, 10f, width, height);

            GUI.Box(rect, GUIContent.none);
            GUILayout.BeginArea(new Rect(rect.x + 10f, rect.y + 8f, width - 20f, height - 16f));
            GUILayout.Label($"<b>Grass V2</b> — tier: {stats.TierName}", RichLabel);
            GUILayout.Label($"Blades   {stats.VisibleInstances:N0} / {stats.TotalInstances:N0} visible", RichLabel);
            GUILayout.Label($"Chunks   {stats.VisibleChunks} visible · {stats.LoadedChunks} resident", RichLabel);
            GUILayout.Label($"Draws    {stats.DrawCalls}", RichLabel);
            GUILayout.Label($"GPU mem  {stats.GpuBufferBytes / (1024f * 1024f):F1} MB", RichLabel);
            GUILayout.EndArea();
        }

        static GUIStyle _richLabel;
        static GUIStyle RichLabel
        {
            get
            {
                _richLabel ??= new GUIStyle(GUI.skin.label) { richText = true, fontSize = 12 };
                return _richLabel;
            }
        }
    }
}
