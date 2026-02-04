using UnityEngine;

namespace Snm.Runtime.WaterSystem
{
    public class WaterReflectionFrustumCalculator
    {
        public static Matrix4x4 Calculate(WaterSurface waterSurface, Camera mirrorCamera)
        {
            var waterCorners = GetWaterCorners(waterSurface);
            var clipPlaneCS = CameraSpacePlane(
                mirrorCamera.worldToCameraMatrix,
                waterSurface.position,
                waterSurface.rotation * Vector3.up, 1);

            var frustum = CalculateClampedWaterReflectionFrustum(mirrorCamera, waterCorners);
            var oblique = CalculateObliqueMatrix(frustum, clipPlaneCS);
            return oblique;
        }

        public static Vector3[] GetWaterCorners(WaterSurface water)
        {
            Vector3 right = water.rotation * Vector3.right * water.size.x * 0.5f;
            Vector3 forward = water.rotation * Vector3.forward * water.size.y * 0.5f;

            Vector3 center = water.position;

            return new Vector3[]
            {
                center - right - forward,
                center - right + forward,
                center + right + forward,
                center + right - forward,
            };
        }

        // public static void CalculateObliqueMatrix(ref Matrix4x4 projection, Vector4 clipPlaneCS)
        // {
        //     Vector4 q = projection.inverse
        //         * new Vector4(Mathf.Sign(clipPlaneCS.x), Mathf.Sign(clipPlaneCS.y), 1.0f, 1.0f);
        //     Vector4 c = clipPlaneCS * (2.0f / Vector4.Dot(clipPlaneCS, q));

        //     projection[2] = c.x - projection[3];
        //     projection[6] = c.y - projection[7];
        //     projection[10] = c.z - projection[11];
        //     projection[14] = c.w - projection[15];
        // }

        public static Matrix4x4 CalculateObliqueMatrix(
            Matrix4x4 projection,
            Vector4 clipPlaneCS)
        {
            Vector4 q = new Vector4(
                (Mathf.Sign(clipPlaneCS.x) + projection[0, 2]) / projection[0, 0],
                (Mathf.Sign(clipPlaneCS.y) + projection[1, 2]) / projection[1, 1],
                -1.0f,
                (1.0f + projection[2, 2]) / projection[2, 3]
            );

            float scale = 2.0f / Vector4.Dot(clipPlaneCS, q);
            Vector4 c = clipPlaneCS * scale;

            // ONLY replace row 3 correctly
            projection[2, 0] = c.x;
            projection[2, 1] = c.y;
            projection[2, 2] = c.z + 1.0f;
            projection[2, 3] = c.w;

            return projection;
        }

        public static Vector4 CameraSpacePlane(
            Matrix4x4 w2c,
            Vector3 pos,
            Vector3 normal,
            float sideSign)
        {
            Vector3 offsetPos = pos + normal * 0.01f;
            Vector3 cPos = w2c.MultiplyPoint(offsetPos);
            Vector3 cNormal = w2c.MultiplyVector(normal).normalized * sideSign;

            return new Vector4(
                cNormal.x,
                cNormal.y,
                cNormal.z,
                -Vector3.Dot(cPos, cNormal)
            );
        }

        public static Matrix4x4 CalculateClampedWaterReflectionFrustum(
            Camera reflectionCamera,
            Vector3[] waterWorldCorners
        )
        {
            Matrix4x4 w2c = reflectionCamera.worldToCameraMatrix;

            float near = reflectionCamera.nearClipPlane;
            float far = reflectionCamera.farClipPlane;

            // float newNear = float.MaxValue;
            float newFar = float.MinValue;

            foreach (var corner in waterWorldCorners)
            {
                Vector3 cam = w2c.MultiplyPoint(corner);
                newFar = Mathf.Max(newFar, -cam.z);
            }
            far = Mathf.Max(Mathf.Min(newFar + 5f, far), near + 0.01f);

            float left = float.PositiveInfinity;
            float right = float.NegativeInfinity;
            float bottom = float.PositiveInfinity;
            float top = float.NegativeInfinity;

            foreach (var corner in waterWorldCorners)
            {
                Vector3 cam = w2c.MultiplyPoint(corner);

                float z = Mathf.Max(-cam.z, near);
                float scale = near / z;

                left = Mathf.Min(left, cam.x * scale);
                right = Mathf.Max(right, cam.x * scale);
                bottom = Mathf.Min(bottom, cam.y * scale);
                top = Mathf.Max(top, cam.y * scale);
            }

            // Extract original camera frustum
            ExtractCameraFrustumAtNear(
                reflectionCamera,
                out float cLeft,
                out float cRight,
                out float cBottom,
                out float cTop
            );

            // Clamp to original frustum
            left = Mathf.Max(left, cLeft);
            right = Mathf.Min(right, cRight);
            bottom = Mathf.Max(bottom, cBottom);
            top = Mathf.Min(top, cTop);

            if (left >= right || bottom >= top)
                return reflectionCamera.projectionMatrix;

            const float padding = 0.01f;
            left -= padding;
            right += padding;
            bottom -= padding;
            top += padding;

            return Matrix4x4.Frustum(left, right, bottom, top, near, far);
        }

        static void ExtractCameraFrustumAtNear(
            Camera cam,
            out float left,
            out float right,
            out float bottom,
            out float top
        )
        {
            Matrix4x4 p = cam.projectionMatrix;
            float near = cam.nearClipPlane;

            left = near * (p[0, 2] - 1f) / p[0, 0];
            right = near * (p[0, 2] + 1f) / p[0, 0];
            bottom = near * (p[1, 2] - 1f) / p[1, 1];
            top = near * (p[1, 2] + 1f) / p[1, 1];
        }
    }
}