using System;
using UnityEngine;

namespace Snm.Runtime.DebugDraw
{
    /// <summary>
    /// Runtime debug drawing — static entry point.
    ///
    /// Every method returns a handle (or array of handles).
    /// The shape or label lives until you call handle.Dispose().
    ///
    /// SHAPES — two usage patterns:
    ///
    ///   1. Dispose + recreate each frame (all params change):
    ///        void FixedUpdate() {
    ///            _arrow?.Dispose();
    ///            _arrow = active ? DebugDraw.Arrow(pos, dir, color) : null;
    ///        }
    ///        void OnDestroy() => _arrow?.Dispose();
    ///
    ///   2. Create once, update each frame (position/color change):
    ///        void OnEnable()  => _ring  = DebugDraw.Ring(center, Vector3.up, radius);
    ///        void Update()    => _ring.SetPositionAndRotation(center, rot);
    ///        void OnDisable() => _ring?.Dispose();
    ///
    /// LABELS:
    ///   _label = DebugDraw.Label(transform, () => $"speed: {rb.velocity.magnitude:F1}");
    ///   _panel = DebugDraw.Panel(transform);
    ///   _panel.Add("health", () => hp, () => maxHp, showBar: true);
    /// </summary>
    public static class DebugDraw
    {
        public static bool Enabled
        {
            get => DebugDrawManager.Enabled;
            set => DebugDrawManager.Enabled = value;
        }

        // ── Shapes ────────────────────────────────────────────────────────────

        private static ShapeDrawer Shapes
        {
            get
            {
                if (DebugDrawManager.Shapes == null) _ = DebugDrawManager.Instance;
                return DebugDrawManager.Shapes;
            }
        }

        private static LabelDrawer Labels
        {
            get
            {
                if (DebugDrawManager.Labels == null) _ = DebugDrawManager.Instance;
                return DebugDrawManager.Labels;
            }
        }

        public static DrawHandle Arrow(Vector3 origin, Vector3 direction,
            Color? color = null, float width = 0, float headLength = 0.2f, float headWidth = 0.1f)
        {
            if (!DebugDrawManager.Enabled) return null;
            return Shapes?.Arrow(origin, direction, color, width, headLength, headWidth);
        }

        public static DrawHandle Line(Vector3 start, Vector3 end, Color? color = null, float width = 0)
        {
            if (!DebugDrawManager.Enabled) return null;
            return Shapes?.Line(start, end, color, width);
        }

        public static DrawHandle Ray(Vector3 origin, Vector3 direction, Color? color = null, float width = 0)
        {
            if (!DebugDrawManager.Enabled) return null;
            return Shapes?.Line(origin, origin + direction, color, width);
        }

        public static DrawHandle Sphere(Vector3 center, float radius, Color? color = null)
        {
            if (!DebugDrawManager.Enabled) return null;
            return Shapes?.Sphere(center, radius, color);
        }

        public static DrawHandle Box(Vector3 center, Vector3 size, Color? color = null)
        {
            if (!DebugDrawManager.Enabled) return null;
            return Shapes?.Box(center, size, color);
        }

        public static DrawHandle Box(Bounds bounds, Color? color = null)
        {
            if (!DebugDrawManager.Enabled) return null;
            return Shapes?.Box(bounds.center, bounds.size, color);
        }

        public static DrawHandle Circle(Vector3 center, Vector3 normal, float radius, Color? color = null)
        {
            if (!DebugDrawManager.Enabled) return null;
            return Shapes?.Circle(center, normal, radius, color);
        }

        public static DrawHandle Ring(Vector3 center, Vector3 normal, float radius,
            Color? color = null, float thickness = 0)
        {
            if (!DebugDrawManager.Enabled) return null;
            return Shapes?.Ring(center, normal, radius, color, thickness);
        }

        /// <param name="angleDeg">Half-angle of the cone in degrees.</param>
        public static DrawHandle Cone(Vector3 origin, Vector3 direction,
            float angleDeg, float length, Color? color = null)
        {
            if (!DebugDrawManager.Enabled) return null;
            return Shapes?.Cone(origin, direction, angleDeg, length, color);
        }

        /// <summary>Draws all 12 edges of a camera frustum. Returns 12 line handles.</summary>
        public static DrawHandle[] Frustum(Camera camera, Color? color = null)
        {
            if (!DebugDrawManager.Enabled) return null;
            return Shapes?.Frustum(camera, color);
        }

        // ── Labels ────────────────────────────────────────────────────────────

        /// <summary>Live-updating label that follows a Transform. Text re-evaluated every frame.</summary>
        public static LabelHandle Label(Transform target, Func<string> textGetter,
            Vector3? offset = null, Color? color = null)
        {
            if (!DebugDrawManager.Enabled) return null;
            return Labels?.Show(textGetter, target, offset, color, autoUpdate: true);
        }

        /// <summary>Static text label that follows a Transform.</summary>
        public static LabelHandle Label(Transform target, string text,
            Vector3? offset = null, Color? color = null)
        {
            if (!DebugDrawManager.Enabled) return null;
            return Labels?.Show(() => text, target, offset, color, autoUpdate: false);
        }

        /// <summary>Static text label at a fixed world position.</summary>
        public static LabelHandle Label(Vector3 worldPos, string text, Color? color = null)
        {
            if (!DebugDrawManager.Enabled) return null;
            return Labels?.Show(() => text, worldPos, null, color, autoUpdate: false);
        }

        /// <summary>
        /// Creates a stacked label panel attached to a Transform.
        /// Add stats with panel.Add(...). Dispose the panel to release all labels at once.
        /// </summary>
        public static StatPanel Panel(Transform target, Vector3? baseOffset = null, float spacing = 0.5f)
        {
            if (!DebugDrawManager.Enabled) return null;
            return Labels?.CreatePanel(target, baseOffset ?? DebugDrawManager.Config.labelOffset, spacing);
        }
    }
}
