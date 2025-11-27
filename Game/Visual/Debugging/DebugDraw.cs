using UnityEngine;

namespace Snm.Debugging
{
    public static class DebugDraw
    {
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
            float width = 0.02f,
            float duration = 0f)
        {
            WorldDrawManager.CreateLine(new[] { start, end }, color, width, duration);
        }

        /// <summary>
        /// Draw a line with an arrow head at the end.
        /// </summary>
        public static void DrawArrow(
            Vector3 start,
            Vector3 end,
            Color color,
            float width = 0.02f,
            float headLength = 0.25f,
            float headAngle = 30f,
            float duration = 0f)
        {
            Vector3 dir = end - start;
            if (dir.sqrMagnitude < 0.0001f)
                return;

            dir.Normalize();

            // Shaft
            DrawLine(start, end - width / 2f * dir, color, width, duration);

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
            WorldDrawManager.CreateLine(new[] { arrowPoint1, end, arrowPoint2 }, color, width / 2f, duration);
        }

        /// <summary>
        /// Draw a wireframe cube using center and size (like Gizmos.DrawWireCube).
        /// </summary>
        public static void DrawWireCube(
            Vector3 center,
            Vector3 size,
            Color color,
            float width = 0.02f,
            float duration = 0f)
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
            DrawLine(p0, p1, color, width, duration);
            DrawLine(p1, p2, color, width, duration);
            DrawLine(p2, p3, color, width, duration);
            DrawLine(p3, p0, color, width, duration);

            // Top square
            DrawLine(p4, p5, color, width, duration);
            DrawLine(p5, p6, color, width, duration);
            DrawLine(p6, p7, color, width, duration);
            DrawLine(p7, p4, color, width, duration);

            // Vertical edges
            DrawLine(p0, p4, color, width, duration);
            DrawLine(p1, p5, color, width, duration);
            DrawLine(p2, p6, color, width, duration);
            DrawLine(p3, p7, color, width, duration);
        }

        // Convenience overload using Bounds
        public static void DrawWireCube(
            Bounds bounds,
            Color color,
            float width = 0.02f,
            float duration = 0f)
        {
            DrawWireCube(bounds.center, bounds.size, color, width, duration);
        }

        // -------------------------
        // SCREEN SPACE DRAWING
        // -------------------------

        public static void DrawScreenLine(
            Vector2 start,
            Vector2 end,
            Color color,
            float width = 2f,
            float duration = 0.05f)
        {
            start.y = Screen.height - start.y;
            end.y = Screen.height - end.y;
            ScreenDrawManager.Instance.AddLine(start, end, color, width, duration);
        }

        public static void DrawScreenArrow(
            Vector2 start,
            Vector2 end,
            Color color,
            float width = 2f,
            float headLength = 12f,
            float headAngle = 25f,
            float duration = 0.05f)
        {
            // Shaft
            DrawScreenLine(start, end, color, width, duration);

            // Arrow head
            Vector2 dir = (start - end).normalized;
            float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;

            float angle1 = angle + headAngle;
            float angle2 = angle - headAngle;

            Vector2 h1 = end + new Vector2(
                Mathf.Cos(angle1 * Mathf.Deg2Rad),
                Mathf.Sin(angle1 * Mathf.Deg2Rad)
            ) * headLength;

            Vector2 h2 = end + new Vector2(
                Mathf.Cos(angle2 * Mathf.Deg2Rad),
                Mathf.Sin(angle2 * Mathf.Deg2Rad)
            ) * headLength;

            DrawScreenLine(end, h1, color, width, duration);
            DrawScreenLine(end, h2, color, width, duration);
        }

        public static void DrawScreenRect(
            Rect rect,
            Color color,
            float width = 2f,
            float duration = 0.05f)
        {
            Vector2 p0 = new(rect.xMin, rect.yMin);
            Vector2 p1 = new(rect.xMax, rect.yMin);
            Vector2 p2 = new(rect.xMax, rect.yMax);
            Vector2 p3 = new(rect.xMin, rect.yMax);

            DrawScreenLine(p0, p1, color, width, duration);
            DrawScreenLine(p1, p2, color, width, duration);
            DrawScreenLine(p2, p3, color, width, duration);
            DrawScreenLine(p3, p0, color, width, duration);
        }
    }

}
