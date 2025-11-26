using UnityEngine;

namespace Snm.Visual
{
    public static class DebugDraw
    {
        private static GameObject _container;
        private static Material _lineMaterial;

        // -------------------------
        //  Core helper
        // -------------------------
        private static void EnsureContainer()
        {
            if (_container != null) return;

            _container = new GameObject("[DebugDraw]");
            Object.DontDestroyOnLoad(_container);

            // Simple, always-available shader
            _lineMaterial = new Material(Shader.Find("Sprites/Default"));
        }

        private static void CreateLine(
            Vector3[] points,
            Color color,
            float duration,
            float width,
            string name = "DebugLine")
        {
            if (points == null || points.Length < 2) return;

            EnsureContainer();

            GameObject lineObj = new GameObject(name);
            lineObj.transform.SetParent(_container.transform);

            LineRenderer lr = lineObj.AddComponent<LineRenderer>();
            lr.material = _lineMaterial;
            lr.positionCount = points.Length;
            lr.SetPositions(points);
            lr.startColor = color;
            lr.endColor = color;
            lr.startWidth = width;
            lr.endWidth = width;
            lr.useWorldSpace = true;
            lr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            lr.receiveShadows = false;
            lr.loop = false;

            if (duration > 0f)
                Object.Destroy(lineObj, duration);
            else
                Object.Destroy(lineObj, Time.deltaTime); // one frame, like Debug.DrawLine
        }

        // -------------------------
        //  PUBLIC API
        // -------------------------

        /// <summary>
        /// Draw a simple line using LineRenderer.
        /// </summary>
        public static void DrawLine(
            Vector3 start,
            Vector3 end,
            Color color,
            float duration = 0f,
            float width = 0.02f)
        {
            CreateLine(new[] { start, end }, color, duration, width, "DebugLine");
        }

        /// <summary>
        /// Draw a line with an arrow head at the end.
        /// </summary>
        public static void DrawArrow(
            Vector3 start,
            Vector3 end,
            Color color,
            float duration = 0f,
            float width = 0.02f,
            float headLength = 0.25f,
            float headAngle = 20f)
        {
            Vector3 dir = end - start;
            if (dir.sqrMagnitude < 0.0001f)
                return;

            dir.Normalize();

            // Shaft
            DrawLine(start, end, color, duration, width);

            // Build an orientation for the arrowhead
            // We need some vector not parallel to dir
            Vector3 side = Vector3.Cross(dir, Vector3.up);
            if (side.sqrMagnitude < 0.0001f)
                side = Vector3.Cross(dir, Vector3.right);
            side.Normalize();

            // Rotate -dir around "side" to get two head directions
            Quaternion rot1 = Quaternion.AngleAxis(headAngle, side);
            Quaternion rot2 = Quaternion.AngleAxis(-headAngle, side);

            Vector3 headDir1 = (rot1 * -dir).normalized;
            Vector3 headDir2 = (rot2 * -dir).normalized;

            Vector3 arrowPoint1 = end + headDir1 * headLength;
            Vector3 arrowPoint2 = end + headDir2 * headLength;

            // Two small lines for the arrow head
            CreateLine(new[] { end, arrowPoint1 }, color, duration, width, "DebugArrowHead");
            CreateLine(new[] { end, arrowPoint2 }, color, duration, width, "DebugArrowHead");
        }

        /// <summary>
        /// Draw a wireframe cube using center and size (like Gizmos.DrawWireCube).
        /// </summary>
        public static void DrawWireCube(
            Vector3 center,
            Vector3 size,
            Color color,
            float duration = 0f,
            float width = 0.02f)
        {
            Vector3 extents = size * 0.5f;

            // 8 corners
            Vector3 p0 = center + new Vector3(-extents.x, -extents.y, -extents.z);
            Vector3 p1 = center + new Vector3(extents.x, -extents.y, -extents.z);
            Vector3 p2 = center + new Vector3(extents.x, -extents.y, extents.z);
            Vector3 p3 = center + new Vector3(-extents.x, -extents.y, extents.z);

            Vector3 p4 = center + new Vector3(-extents.x, extents.y, -extents.z);
            Vector3 p5 = center + new Vector3(extents.x, extents.y, -extents.z);
            Vector3 p6 = center + new Vector3(extents.x, extents.y, extents.z);
            Vector3 p7 = center + new Vector3(-extents.x, extents.y, extents.z);

            // Bottom square
            DrawLine(p0, p1, color, duration, width);
            DrawLine(p1, p2, color, duration, width);
            DrawLine(p2, p3, color, duration, width);
            DrawLine(p3, p0, color, duration, width);

            // Top square
            DrawLine(p4, p5, color, duration, width);
            DrawLine(p5, p6, color, duration, width);
            DrawLine(p6, p7, color, duration, width);
            DrawLine(p7, p4, color, duration, width);

            // Vertical edges
            DrawLine(p0, p4, color, duration, width);
            DrawLine(p1, p5, color, duration, width);
            DrawLine(p2, p6, color, duration, width);
            DrawLine(p3, p7, color, duration, width);
        }

        // Convenience overload using Bounds
        public static void DrawWireCube(
            Bounds bounds,
            Color color,
            float duration = 0f,
            float width = 0.02f)
        {
            DrawWireCube(bounds.center, bounds.size, color, duration, width);
        }

        // -------------------------
        // SCREEN SPACE DRAWING
        // -------------------------

        public static void DrawScreenLine(
            Vector2 start,
            Vector2 end,
            Color color,
            float duration = 0.05f,
            float width = 2f)
        {
            start.y = Screen.height - start.y;
            end.y = Screen.height - end.y;
            ScreenDrawManager.Instance.AddLine(start, end, color, width, duration);
        }

        public static void DrawScreenArrow(
            Vector2 start,
            Vector2 end,
            Color color,
            float duration = 0.05f,
            float width = 2f,
            float headLength = 12f,
            float headAngle = 25f)
        {
            // Shaft
            DrawScreenLine(start, end, color, duration, width);

            // Arrow head
            Vector2 dir = (end - start).normalized;
            float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;

            float angle1 = angle + headAngle;
            float angle2 = angle - headAngle;

            Vector2 h1 = end + new Vector2(
                Mathf.Cos((angle1) * Mathf.Deg2Rad),
                Mathf.Sin((angle1) * Mathf.Deg2Rad)
            ) * headLength;

            Vector2 h2 = end + new Vector2(
                Mathf.Cos((angle2) * Mathf.Deg2Rad),
                Mathf.Sin((angle2) * Mathf.Deg2Rad)
            ) * headLength;

            DrawScreenLine(end, h1, color, duration, width);
            DrawScreenLine(end, h2, color, duration, width);
        }

        public static void DrawScreenRect(
            Rect rect,
            Color color,
            float duration = 0.05f,
            float width = 2f)
        {
            Vector2 p0 = new(rect.xMin, rect.yMin);
            Vector2 p1 = new(rect.xMax, rect.yMin);
            Vector2 p2 = new(rect.xMax, rect.yMax);
            Vector2 p3 = new(rect.xMin, rect.yMax);

            DrawScreenLine(p0, p1, color, duration, width);
            DrawScreenLine(p1, p2, color, duration, width);
            DrawScreenLine(p2, p3, color, duration, width);
            DrawScreenLine(p3, p0, color, duration, width);
        }
    }

}
