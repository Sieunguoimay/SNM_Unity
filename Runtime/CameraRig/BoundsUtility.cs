using System.Collections.Generic;
using UnityEngine;

namespace Snm.CameraRig
{
    public static class BoundsUtility
    {
        public static Bounds Combine(IEnumerable<Bounds> boundList)
        {
            var min = new Vector3(float.MaxValue, float.MaxValue, float.MaxValue);
            var max = new Vector3(float.MinValue, float.MinValue, float.MinValue);
            var any = false;
            foreach (var b in boundList)
            {
                any = true;
                if (b.min.x < min.x) min.x = b.min.x;
                if (b.min.y < min.y) min.y = b.min.y;
                if (b.min.z < min.z) min.z = b.min.z;
                if (b.max.x > max.x) max.x = b.max.x;
                if (b.max.y > max.y) max.y = b.max.y;
                if (b.max.z > max.z) max.z = b.max.z;
            }
            if (!any) return new Bounds(Vector3.zero, Vector3.zero);
            var center = (max + min) / 2f;
            var size = max - min;
            return new Bounds(center, size);
        }

        /// <summary>
        /// Project a world AABB into NDC space, computing the NDC bounds of its 8 corners.
        /// Returns false when every corner is behind the camera (w &lt;= 0) — callers must skip
        /// the result rather than treating an empty Bounds as origin-centered.
        /// </summary>
        public static bool TryBoundsWorldToNDC(Bounds worldBounds, Matrix4x4 camMatrix_VP, out Bounds ndcBounds)
        {
            // View-projection matrix: world -> clip
            var vp = camMatrix_VP;

            // 8 corners of the world-space AABB
            var min = worldBounds.min;
            var max = worldBounds.max;

            Vector3[] corners = {
                new(min.x, min.y, min.z),
                new(max.x, min.y, min.z),
                new(min.x, max.y, min.z),
                new(max.x, max.y, min.z),
                new(min.x, min.y, max.z),
                new(max.x, min.y, max.z),
                new(min.x, max.y, max.z),
                new(max.x, max.y, max.z),
            };
            var first = true;
            ndcBounds = new Bounds();

            for (int i = 0; i < 8; i++)
            {
                var worldPos = corners[i];
                var clip = vp * new Vector4(worldPos.x, worldPos.y, worldPos.z, 1f);

                // Skip corners behind the camera (w <= 0) to avoid sign-flipped NDC
                if (clip.w <= 0f)
                    continue;

                // Homogeneous divide -> NDC
                var ndc = new Vector3(clip.x / clip.w, clip.y / clip.w, clip.z / clip.w);

                if (first)
                {
                    ndcBounds = new Bounds(ndc, Vector3.zero);
                    first = false;
                }
                else
                {
                    ndcBounds.Encapsulate(ndc);
                }
            }

            return !first;
        }

        /// <summary>
        /// Legacy overload kept for back-compat; returns an empty Bounds when every corner is
        /// behind the camera, which callers will accidentally treat as origin-centered.
        /// Prefer <see cref="TryBoundsWorldToNDC"/>.
        /// </summary>
        public static Bounds BoundsWorldToNDC(Bounds worldBounds, Matrix4x4 camMatrix_VP)
        {
            TryBoundsWorldToNDC(worldBounds, camMatrix_VP, out var ndc);
            return ndc;
        }

        public static Rect BoundsNDCToScreenRect(Bounds ndcBounds, Vector2Int screenPixelSize)
        {
            var min = ndcBounds.min;
            var max = ndcBounds.max;

            // Convert NDC [-1,1] to viewport [0,1]
            var vpMin = new Vector2((min.x + 1f) * 0.5f, (min.y + 1f) * 0.5f);
            var vpMax = new Vector2((max.x + 1f) * 0.5f, (max.y + 1f) * 0.5f);

            // Clamp to viewport
            vpMin = Vector2.Max(Vector2.zero, Vector2.Min(Vector2.one, vpMin));
            vpMax = Vector2.Max(Vector2.zero, Vector2.Min(Vector2.one, vpMax));

            // Viewport -> screen
            var xMin = vpMin.x * screenPixelSize.x;
            var yMin = vpMin.y * screenPixelSize.y;
            var xMax = vpMax.x * screenPixelSize.x;
            var yMax = vpMax.y * screenPixelSize.y;

            return Rect.MinMaxRect(xMin, yMin, xMax, yMax);
        }

        public static Bounds ScreenRectToNdcBounds(
            Vector2Int screenPixelSize,
            Rect screenRect,
            float minNdcZ = -1f,
            float maxNdcZ = 1f)
        {
            var w = (float)screenPixelSize.x;
            var h = (float)screenPixelSize.y;

            // 0..1 normalized
            var x0 = screenRect.xMin / w;
            var x1 = screenRect.xMax / w;
            var y0 = screenRect.yMin / h;
            var y1 = screenRect.yMax / h;

            // 0..1 → -1..1 (NDC)
            var ndcX0 = x0 * 2f - 1f;
            var ndcX1 = x1 * 2f - 1f;
            var ndcY0 = y0 * 2f - 1f;
            var ndcY1 = y1 * 2f - 1f;

            var min = new Vector3(
                Mathf.Min(ndcX0, ndcX1),
                Mathf.Min(ndcY0, ndcY1),
                Mathf.Min(minNdcZ, maxNdcZ)
            );

            var max = new Vector3(
                Mathf.Max(ndcX0, ndcX1),
                Mathf.Max(ndcY0, ndcY1),
                Mathf.Max(minNdcZ, maxNdcZ)
            );

            var b = new Bounds();
            b.SetMinMax(min, max);
            return b;
        }

        public static Vector3 CalculateCamOffset_ToCenterAndFitNdcBounds_Perspective(
            Matrix4x4 camMatrix_P_Inverse,
            float fov, float aspect,
            Bounds ndcBounds,
            float convergenceRateXY = 0.5f,
            float convergenceRateZ = 0.5f,
            float minDistance = 0f,
            float maxDistance = float.MaxValue)
        {
            // ============================================================
            // Step 1 — NDC → View-space center
            // ============================================================
            var ndcCenter = ndcBounds.center;
            var clipCenter = new Vector4(ndcCenter.x, ndcCenter.y, ndcCenter.z, 1f);
            var viewCenter = camMatrix_P_Inverse * clipCenter;
            viewCenter /= viewCenter.w;

            var halfHeight = Mathf.Tan(0.5f * fov * Mathf.Deg2Rad);
            var halfWidth = halfHeight * aspect;
            var viewCenterZ = viewCenter.z;        // NEGATIVE
            var viewCenterZ_Minus = -viewCenterZ;        // POSITIVE

            // ============================================================
            // Step 2 — Compute centering offset in view space
            // ============================================================
            var viewCenterOffsetX = ndcCenter.x * viewCenterZ_Minus * halfWidth;
            var viewCenterOffsetY = ndcCenter.y * viewCenterZ_Minus * halfHeight;

            // ============================================================
            // Step 3 — Compute target depth so bounds fit the screen
            // ============================================================
            var ndcMinX = ndcBounds.min.x;
            var ndcMaxX = ndcBounds.max.x;
            var ndcMinY = ndcBounds.min.y;
            var ndcMaxY = ndcBounds.max.y;

            // Required depth scalings (in positive-depth space)
            var d1 = viewCenterZ_Minus * Mathf.Abs(ndcMinX / -1f);
            var d2 = viewCenterZ_Minus * Mathf.Abs(ndcMaxX / +1f);
            var d3 = viewCenterZ_Minus * Mathf.Abs(ndcMinY / -1f);
            var d4 = viewCenterZ_Minus * Mathf.Abs(ndcMaxY / +1f);

            var viewCenterZ_Minus_New = Mathf.Max(d1, Mathf.Max(d2, Mathf.Max(d3, d4))); // POSITIVE
            viewCenterZ_Minus_New = Mathf.Clamp(viewCenterZ_Minus_New, minDistance, maxDistance);

            // Convert back to view-space Z (negative)
            var viewCenterZ_New = -viewCenterZ_Minus_New;

            // ============================================================
            // Step 4 — Convert Δz to world movement, apply per-axis convergence
            // ============================================================
            var viewCenterOffsetZ = viewCenterZ_New - viewCenterZ;

            return new Vector3(
                viewCenterOffsetX * convergenceRateXY,
                viewCenterOffsetY * convergenceRateXY,
                viewCenterOffsetZ * convergenceRateZ);
        }
    }
}