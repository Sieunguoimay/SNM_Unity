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
        static readonly Color DisturberRadiusColor = new(0.3f, 0.9f, 0.3f, 0.8f);
        static readonly Color DisturberCoreColor = new(1f, 0.55f, 0.1f, 0.9f);
        static readonly Color DisturberDirectionColor = new(0.2f, 0.9f, 1f, 1f);

        static class Ids
        {
            public static readonly int DirectionLock = Shader.PropertyToID("_DirectionLock");
            public static readonly int CellCount = Shader.PropertyToID("_CellCount");
            public static readonly int WindRect = Shader.PropertyToID("_WindRect");
        }

        static Mesh _bendFieldQuad;
        static Material _bendFieldMaterial;
        static Material _windFieldMaterial;

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

        /// <summary>
        /// Arrow-field projection of the bend map onto the canvas area, drawn
        /// as a real transparent mesh (call once per frame from LateUpdate).
        /// Arrow = fall direction · green→red = bend energy · white = direction
        /// locked (past directionLockAmount).
        /// </summary>
        public static void DrawBendField(GrassWorld world)
        {
            var canvas = world.Canvas;
            if (canvas == null || canvas.BendTexture == null) return;

            if (_bendFieldMaterial == null)
            {
                var shader = Shader.Find("Hidden/Snm/GrassV2BendDebug");
                if (shader == null) return; // shader folder incomplete; nothing to draw
                _bendFieldMaterial = new Material(shader) { hideFlags = HideFlags.HideAndDontSave };
            }
            if (_bendFieldQuad == null) _bendFieldQuad = BuildUnitQuadXZ();

            _bendFieldMaterial.SetFloat(Ids.DirectionLock, world.Config.directionLockAmount);
            _bendFieldMaterial.SetFloat(Ids.CellCount, 48f);

            var min = canvas.WorldMin;
            float size = canvas.WorldSize;
            var matrix = Matrix4x4.TRS(
                new Vector3(min.x, SampleGroundHeight(world, min, size) + 0.05f, min.y),
                Quaternion.identity,
                new Vector3(size, 1f, size));
            Graphics.DrawMesh(_bendFieldQuad, matrix, _bendFieldMaterial, 0);
        }

        // Height of the grass roots under the canvas (average of overlapping
        // chunks), so the debug plane hugs the ground instead of the
        // GrassWorld object's own transform.
        static float SampleGroundHeight(GrassWorld world, Vector2 canvasMin, float canvasSize)
        {
            var canvasMax = canvasMin + new Vector2(canvasSize, canvasSize);
            float sum = 0f;
            int count = 0;
            foreach (var chunk in world.AllChunks)
            {
                var bounds = chunk.WorldBounds;
                if (bounds.max.x < canvasMin.x || bounds.min.x > canvasMax.x ||
                    bounds.max.z < canvasMin.y || bounds.min.z > canvasMax.y) continue;
                sum += (chunk.Record.minY + chunk.Record.maxY) * 0.5f;
                count++;
            }
            return count > 0 ? sum / count : world.transform.position.y;
        }

        /// <summary>
        /// Live procedural-wind arrow field over a patch around the camera focus,
        /// drawn as a real transparent mesh (call once per frame from LateUpdate).
        /// Arrows point the way the wind blows and animate as gusts travel, so the
        /// Wind Direction / Speed / Noise Scale config is visible at a glance.
        /// </summary>
        public static void DrawWindField(GrassWorld world)
        {
            var canvas = world.Canvas;
            if (canvas == null || canvas.BendTexture == null) return; // world idle

            if (_windFieldMaterial == null)
            {
                var shader = Shader.Find("Hidden/Snm/GrassV2WindDebug");
                if (shader == null) return;
                _windFieldMaterial = new Material(shader) { hideFlags = HideFlags.HideAndDontSave };
            }
            if (_bendFieldQuad == null) _bendFieldQuad = BuildUnitQuadXZ();

            // Reuse the canvas footprint as the debug patch (already camera-centered).
            var min = canvas.WorldMin;
            float size = canvas.WorldSize;
            _windFieldMaterial.SetVector(Ids.WindRect, new Vector4(min.x, min.y, size, size));
            _windFieldMaterial.SetFloat(Ids.CellCount, 32f);

            var matrix = Matrix4x4.TRS(
                new Vector3(min.x, SampleGroundHeight(world, min, size) + 0.06f, min.y),
                Quaternion.identity,
                new Vector3(size, 1f, size));
            Graphics.DrawMesh(_bendFieldQuad, matrix, _windFieldMaterial, 0);
        }

        /// <summary>Outer + full-flatten circles and travel-direction arrow for every active disturber.</summary>
        public static void DrawDisturberGizmos(GrassWorld world)
        {
            foreach (var disturber in GrassDisturber.ActiveDisturbers)
            {
                DrawDisturberGizmo(disturber);
            }
        }

        public static void DrawDisturberGizmo(GrassDisturber disturber)
        {
            var position = disturber.transform.position;
            float outerRadius = disturber.outerRadius;
            float coreRadius = Mathf.Clamp(disturber.fullFlattenRadius, 0f, outerRadius);

            Gizmos.color = DisturberRadiusColor;
            DrawCircleXZ(position, outerRadius);

            if (coreRadius > 0f)
            {
                Gizmos.color = DisturberCoreColor;
                DrawCircleXZ(position, coreRadius);
            }

            var direction = disturber.MoveDirection;
            direction.y = 0f;
            if (direction.sqrMagnitude > 0.0001f)
            {
                direction.Normalize();
                Gizmos.color = DisturberDirectionColor;
                var tip = position + direction * (outerRadius * 1.3f);
                Gizmos.DrawLine(position, tip);
                var headLeft = Quaternion.Euler(0f, 150f, 0f) * direction;
                var headRight = Quaternion.Euler(0f, -150f, 0f) * direction;
                Gizmos.DrawLine(tip, tip + headLeft * (outerRadius * 0.3f));
                Gizmos.DrawLine(tip, tip + headRight * (outerRadius * 0.3f));
            }
        }

        static void DrawCircleXZ(Vector3 center, float radius)
        {
            const int segments = 24;
            var previous = center + new Vector3(radius, 0f, 0f);
            for (int i = 1; i <= segments; i++)
            {
                float angle = i / (float)segments * Mathf.PI * 2f;
                var next = center + new Vector3(Mathf.Cos(angle) * radius, 0f, Mathf.Sin(angle) * radius);
                Gizmos.DrawLine(previous, next);
                previous = next;
            }
        }

        // Unit quad on the XZ plane, origin at min corner, UVs matching the
        // canvas mapping (u along +X, v along +Z).
        static Mesh BuildUnitQuadXZ()
        {
            var mesh = new Mesh { name = "GrassV2 BendDebug Quad", hideFlags = HideFlags.HideAndDontSave };
            mesh.vertices = new[]
            {
                new Vector3(0f, 0f, 0f),
                new Vector3(1f, 0f, 0f),
                new Vector3(0f, 0f, 1f),
                new Vector3(1f, 0f, 1f),
            };
            mesh.uv = new[]
            {
                new Vector2(0f, 0f),
                new Vector2(1f, 0f),
                new Vector2(0f, 1f),
                new Vector2(1f, 1f),
            };
            mesh.triangles = new[] { 0, 2, 1, 1, 2, 3 };
            mesh.RecalculateBounds();
            return mesh;
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
