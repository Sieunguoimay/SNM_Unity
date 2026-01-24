using UnityEngine;

namespace Snm.Runtime.WaterSystem
{
    public class MirrorCameraMover
    {
        private readonly WaterSurface waterSurface;
        private readonly MirrorCameraMoveData data;

        public MirrorCameraMover(
            WaterSurface waterSurface,
            MirrorCameraMoveData data)
        {
            this.waterSurface = waterSurface;
            this.data = data;
        }

        public void Move()
        {
            WaterReflectionUtil.MirrorCameraTransform(data.target, data.mirror, waterSurface);
        }
    }
    public class MirrorCameraMoveData
    {
        public Transform target;
        public Transform mirror;
    }

    public static class WaterReflectionUtil
    {
        public static void MirrorCameraTransform(
            Transform sourceCamera,
            Transform reflectionCamera,
            WaterSurface water)
        {
            Vector3 planeNormal = water.normal.normalized;
            Vector3 planePoint = water.position;

            // --- Position ---
            reflectionCamera.position = ReflectPoint(
                sourceCamera.position,
                planePoint,
                planeNormal
            );

            // --- Rotation ---
            Vector3 reflectedForward = ReflectDirection(
                sourceCamera.forward,
                planeNormal
            );

            Vector3 reflectedUp = ReflectDirection(
                sourceCamera.up,
                planeNormal
            );

            reflectionCamera.rotation = Quaternion.LookRotation(
                reflectedForward,
                reflectedUp
            );
        }

        private static Vector3 ReflectPoint(
            Vector3 point,
            Vector3 planePoint,
            Vector3 planeNormal)
        {
            float distance = Vector3.Dot(planeNormal, point - planePoint);
            return point - 2f * distance * planeNormal;
        }

        private static Vector3 ReflectDirection(
            Vector3 direction,
            Vector3 planeNormal)
        {
            return direction - 2f * Vector3.Dot(direction, planeNormal) * planeNormal;
        }
    }
}