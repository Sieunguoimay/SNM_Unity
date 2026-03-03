using UnityEngine;

namespace Snm.Runtime.WaterSystem
{
    public static class TransformReflectionUtil
    {
        public static void Reflection(
            Transform sourceCamera,
            Transform reflectionCamera,
            Vector3 planePoint, Vector3 planeNormal)
        {
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