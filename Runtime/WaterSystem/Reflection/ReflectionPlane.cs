using Snm.SurfaceInteraction;
using UnityEngine;

namespace Snm.WaterSystem.Reflection
{
    public readonly struct ReflectionPlane
    {
        public readonly Vector3 Normal;
        public readonly Vector3 Point;

        public ReflectionPlane(SurfaceCanvas surface)
        {
            Normal = surface.Rotation * Vector3.up;
            Point = surface.Position;
        }

        public float SignedDistance(Vector3 worldPoint)
            => Vector3.Dot(Normal, worldPoint - Point);

        public Vector3 ReflectPoint(Vector3 worldPoint)
            => worldPoint - 2f * SignedDistance(worldPoint) * Normal;

        public Vector3 ReflectDirection(Vector3 direction)
            => direction - 2f * Vector3.Dot(direction, Normal) * Normal;

        public Vector4 ToCameraSpace(Matrix4x4 worldToCamera, float sideSign = 1f)
        {
            var offsetPoint_ws = Point + Normal * 0.01f;
            var point_cs = worldToCamera.MultiplyPoint(offsetPoint_ws);
            var normal_cs = worldToCamera.MultiplyVector(Normal).normalized * sideSign;
            return new Vector4(normal_cs.x, normal_cs.y, normal_cs.z,
                               -Vector3.Dot(point_cs, normal_cs));
        }
    }
}
