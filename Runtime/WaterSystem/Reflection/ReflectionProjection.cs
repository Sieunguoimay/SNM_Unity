// ─────────────────────────────────────────────
// ReflectionFrustum.cs
// Pure math: computes the tightest valid projection matrix for
// rendering a reflection that only covers the visible water quad.
// All intermediate work is explicit about coordinate space.
// ─────────────────────────────────────────────
using UnityEngine;

namespace Snm.WaterSystem.Reflection
{
    public static class ReflectionProjection
    {
        private const float FarPadding = 5f;
        private const float EdgePadding = 0.01f;

        /// Build a full oblique projection for the reflection camera.
        public static Matrix4x4 Compute(
            Camera reflectionCamera,
            Vector3[] waterCorners_ws,
            in ReflectionPlane plane)
        {
            var clamped = ClampFrustumToWaterQuad(reflectionCamera, waterCorners_ws);
            var clipPlane_cs = plane.ToCameraSpace(reflectionCamera.worldToCameraMatrix, sideSign: 1f);
            return ApplyObliqueClip(clamped, clipPlane_cs);
        }

        /// Tight frustum that contains the water corners but stays inside the camera frustum.
        private static Matrix4x4 ClampFrustumToWaterQuad(Camera camera, Vector3[] corners_ws)
        {
            var w2c = camera.worldToCameraMatrix;
            float near = camera.nearClipPlane;
            float far = camera.farClipPlane;

            // ── extend far plane to include all water corners ────────────────
            float maxDepth = float.MinValue;
            foreach (var c_ws in corners_ws)
                maxDepth = Mathf.Max(maxDepth, -w2c.MultiplyPoint(c_ws).z);

            far = Mathf.Clamp(maxDepth + FarPadding, near + 0.01f, far);

            // ── project corners to near-plane bounds ─────────────────────────
            float left = float.PositiveInfinity;
            float right = float.NegativeInfinity;
            float bottom = float.PositiveInfinity;
            float top = float.NegativeInfinity;

            foreach (var c_ws in corners_ws)
            {
                var c_cs = w2c.MultiplyPoint(c_ws);
                float scale = near / Mathf.Max(-c_cs.z, near);   // project onto near plane

                left = Mathf.Min(left, c_cs.x * scale);
                right = Mathf.Max(right, c_cs.x * scale);
                bottom = Mathf.Min(bottom, c_cs.y * scale);
                top = Mathf.Max(top, c_cs.y * scale);
            }

            // ── clamp to original camera frustum ────────────────────────────
            GetNearPlaneBounds(camera, out float cL, out float cR, out float cB, out float cT);
            left = Mathf.Max(left, cL);
            right = Mathf.Min(right, cR);
            bottom = Mathf.Max(bottom, cB);
            top = Mathf.Min(top, cT);

            if (left >= right || bottom >= top)
                return camera.projectionMatrix;   // degenerate — fall back to default

            return Matrix4x4.Frustum(
                left - EdgePadding,
                right + EdgePadding,
                bottom - EdgePadding,
                top + EdgePadding,
                near, far);
        }

        /// Tilt the near clip plane so it coincides with the water surface,
        /// eliminating any geometry rendered below the water.
        private static Matrix4x4 ApplyObliqueClip(Matrix4x4 projection, Vector4 clipPlane_cs)
        {
            // Find the camera-space point that maps to the far corner of the clip plane's quadrant.
            var q = new Vector4(
                (Mathf.Sign(clipPlane_cs.x) + projection[0, 2]) / projection[0, 0],
                (Mathf.Sign(clipPlane_cs.y) + projection[1, 2]) / projection[1, 1],
                -1f,
                (1f + projection[2, 2]) / projection[2, 3]);

            // Scale the clip plane so it passes through q, then replace row 2.
            var c = clipPlane_cs * (2f / Vector4.Dot(clipPlane_cs, q));

            projection[2, 0] = c.x;
            projection[2, 1] = c.y;
            projection[2, 2] = c.z + 1f;
            projection[2, 3] = c.w;

            return projection;
        }

        private static void GetNearPlaneBounds(Camera camera,
            out float left, out float right, out float bottom, out float top)
        {
            var p = camera.projectionMatrix;
            float near = camera.nearClipPlane;

            left = near * (p[0, 2] - 1f) / p[0, 0];
            right = near * (p[0, 2] + 1f) / p[0, 0];
            bottom = near * (p[1, 2] - 1f) / p[1, 1];
            top = near * (p[1, 2] + 1f) / p[1, 1];
        }
    }
}