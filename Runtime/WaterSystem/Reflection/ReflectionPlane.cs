// ─────────────────────────────────────────────
// WaterPlane.cs
// Represents the infinite reflection plane derived from a WaterSurface.
// ─────────────────────────────────────────────
using Snm.WaterSystem.Surface;
using UnityEngine;

namespace Snm.WaterSystem.Reflection
{
    public readonly struct ReflectionPlane
    {
        public readonly Vector3 Normal;   // world-space upward normal
        public readonly Vector3 Point;    // any point on the plane

        public ReflectionPlane(SurfaceData surface)
        {
            Normal = surface.rotation * Vector3.up;
            Point = surface.position;
        }

        /// Signed distance from a world-space point to the plane.
        public float SignedDistance(Vector3 worldPoint)
            => Vector3.Dot(Normal, worldPoint - Point);

        /// Reflect a world-space point across the plane.
        public Vector3 ReflectPoint(Vector3 worldPoint)
            => worldPoint - 2f * SignedDistance(worldPoint) * Normal;

        /// Reflect a world-space direction across the plane.
        public Vector3 ReflectDirection(Vector3 direction)
            => direction - 2f * Vector3.Dot(direction, Normal) * Normal;

        /// Camera-space plane equation (ax + by + cz + d = 0) with an inward side sign.
        public Vector4 ToCameraSpace(Matrix4x4 worldToCamera, float sideSign = 1f)
        {
            // Offset slightly above plane to avoid self-intersection artifacts.
            var offsetPoint_ws = Point + Normal * 0.01f;
            var point_cs = worldToCamera.MultiplyPoint(offsetPoint_ws);
            var normal_cs = worldToCamera.MultiplyVector(Normal).normalized * sideSign;
            return new Vector4(normal_cs.x, normal_cs.y, normal_cs.z,
                               -Vector3.Dot(point_cs, normal_cs));
        }
    }
}
