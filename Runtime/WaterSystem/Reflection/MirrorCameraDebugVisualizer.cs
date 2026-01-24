using System.Collections.Generic;
using UnityEngine;

namespace Snm.Runtime.WaterSystem
{
    public class MirrorCameraDebugVisualizer : MonoBehaviour
    {
        private WaterSurface _waterSurface;
        private Camera _reflectionCamera;

        public Color frustumColor = Color.cyan;
        public Color waterColor = Color.blue;
        public Color hitColor = Color.yellow;
        public Color volumeColor = new(1f, 0.8f, 0f, 0.7f);

        public void SetWaterSurface(WaterSurface waterSurface)
        {
            _waterSurface = waterSurface;
        }

        public void SetCamera(Camera reflectionCamera)
        {
            _reflectionCamera = reflectionCamera;
        }

        private void OnDrawGizmos()
        {
            if (_reflectionCamera == null || _waterSurface == null) return;

            var camTransform = _reflectionCamera.transform;

            if (RayPlaneUtil.RayPlaneIntersection(
                camTransform.position,
                camTransform.forward,
                _waterSurface.position,
                _waterSurface.normal,
                out var hitPoint))
            {
                var oldMatrix = Gizmos.matrix;
                var oldColor = Gizmos.color;

                Gizmos.color = Color.red;

                Gizmos.DrawLine(camTransform.position, hitPoint);

                Gizmos.matrix = oldMatrix;
                Gizmos.color = oldColor;
            }

            DrawCameraFrustum();
            DrawWaterSurface();
            DrawExtrudedVolume();
            DrawWaterFrustumIntersection();
        }

        private void DrawExtrudedVolume()
        {

            Vector3[] water = GetWaterCorners(_waterSurface);
            Vector3[] far = ExtrudeToReflectionDepth(_reflectionCamera, _waterSurface, water, _waterSurface.reflectionDepth);

            DrawQuad(water, waterColor);
            DrawExtrudedVolume(water, far, volumeColor);
        }
        void DrawQuad(Vector3[] q, Color c)
        {
            Gizmos.color = c;

            for (int i = 0; i < 4; i++)
                Gizmos.DrawLine(q[i], q[(i + 1) % 4]);
        }
        void DrawExtrudedVolume(
            Vector3[] near,
            Vector3[] far,
            Color c)
        {
            Gizmos.color = c;

            // Near face
            DrawQuad(near, c);

            // Far face
            DrawQuad(far, c);

            // Sides
            for (int i = 0; i < 4; i++)
                Gizmos.DrawLine(near[i], far[i]);
        }

        static Plane BuildDepthLimitPlane(WaterSurface water, float reflectionDepth)
        {
            // Plane parallel to water surface, offset along normal
            Vector3 point = water.position + water.normal * reflectionDepth;
            return new Plane(water.normal, point);
        }

        static Vector3[] ExtrudeToReflectionDepth(
            Camera cam,
            WaterSurface water,
            Vector3[] waterCorners,
            float reflectionDepth)
        {
            Vector3[] farCorners = new Vector3[4];

            Plane depthPlane = new Plane(
                water.normal,
                water.position + water.normal * reflectionDepth
            );

            Vector3 camPos = cam.transform.position;

            for (int i = 0; i < 4; i++)
            {
                Vector3 dir = (waterCorners[i] - camPos).normalized;
                Ray ray = new Ray(camPos, dir);

                if (depthPlane.Raycast(ray, out float t))
                {
                    farCorners[i] = ray.origin + ray.direction * t;
                }
                else
                {
                    // Fallback: clamp distance
                    farCorners[i] = waterCorners[i] + dir * reflectionDepth;
                }
            }

            return farCorners;
        }

        static Vector3[] ExtrudeToFarPlane(
            Camera cam,
            Vector3[] waterCorners)
        {
            Vector3[] farCorners = new Vector3[4];

            Vector3 camPos = cam.transform.position;
            Vector3 camForward = cam.transform.forward;
            float far = cam.farClipPlane;

            // Far plane in world space
            Plane farPlane = new Plane(
                -camForward,
                camPos + camForward * far
            );

            for (int i = 0; i < 4; i++)
            {
                Vector3 dir = (waterCorners[i] - camPos).normalized;
                Ray ray = new Ray(camPos, dir);

                if (farPlane.Raycast(ray, out float t))
                {
                    farCorners[i] = ray.origin + ray.direction * t;
                }
                else
                {
                    // Fallback (should rarely happen)
                    farCorners[i] = camPos + dir * far;
                }
            }

            return farCorners;
        }

        static Vector3[] GetWaterCorners(WaterSurface s)
        {
            Vector3 right = Vector3.right * (s.size.x * 0.5f);
            Vector3 forward = Vector3.forward * (s.size.y * 0.5f);

            return new Vector3[]
            {
        s.position - right - forward, // BL
        s.position - right + forward, // TL
        s.position + right + forward, // TR
        s.position + right - forward, // BR
            };
        }

        private static void DrawFrustum(Camera cam)
        {
            var t = cam.transform;

            float near = cam.nearClipPlane;
            float far = cam.farClipPlane;
            float fov = cam.fieldOfView;
            float aspect = cam.aspect;

            float halfFovRad = Mathf.Deg2Rad * fov * 0.5f;

            float nearHeight = Mathf.Tan(halfFovRad) * near;
            float nearWidth = nearHeight * aspect;

            float farHeight = Mathf.Tan(halfFovRad) * far;
            float farWidth = farHeight * aspect;

            Vector3 nc = t.position + t.forward * near;
            Vector3 fc = t.position + t.forward * far;

            // Near plane
            Vector3 ntl = nc + t.up * nearHeight - t.right * nearWidth;
            Vector3 ntr = nc + t.up * nearHeight + t.right * nearWidth;
            Vector3 nbl = nc - t.up * nearHeight - t.right * nearWidth;
            Vector3 nbr = nc - t.up * nearHeight + t.right * nearWidth;

            // Far plane
            Vector3 ftl = fc + t.up * farHeight - t.right * farWidth;
            Vector3 ftr = fc + t.up * farHeight + t.right * farWidth;
            Vector3 fbl = fc - t.up * farHeight - t.right * farWidth;
            Vector3 fbr = fc - t.up * farHeight + t.right * farWidth;

            // Draw lines
            Gizmos.DrawLine(ntl, ntr);
            Gizmos.DrawLine(ntr, nbr);
            Gizmos.DrawLine(nbr, nbl);
            Gizmos.DrawLine(nbl, ntl);

            Gizmos.DrawLine(ftl, ftr);
            Gizmos.DrawLine(ftr, fbr);
            Gizmos.DrawLine(fbr, fbl);
            Gizmos.DrawLine(fbl, ftl);

            Gizmos.DrawLine(ntl, ftl);
            Gizmos.DrawLine(ntr, ftr);
            Gizmos.DrawLine(nbl, fbl);
            Gizmos.DrawLine(nbr, fbr);
        }

        void DrawWaterSurface()
        {
            Bounds b = BuildWaterBounds(_waterSurface);

            Gizmos.color = waterColor;
            Gizmos.DrawWireCube(b.center, b.size);
        }

        void DrawCameraFrustum()
        {
            GetCameraFrustumCorners(_reflectionCamera, out var nearC, out var farC);

            Gizmos.color = frustumColor;

            for (int i = 0; i < 4; i++)
            {
                Gizmos.DrawLine(nearC[i], nearC[(i + 1) % 4]);
                Gizmos.DrawLine(farC[i], farC[(i + 1) % 4]);
                Gizmos.DrawLine(nearC[i], farC[i]);
            }
        }

        void DrawWaterFrustumIntersection()
        {
            List<Vector3> hits =
                ComputeFrustumWaterIntersections(_reflectionCamera, _waterSurface);

            Gizmos.color = hitColor;

            for (int i = 0; i < hits.Count; i++)
            {
                Gizmos.DrawSphere(hits[i], 0.2f);
                Gizmos.DrawLine(hits[i], hits[(i + 1) % hits.Count]);
            }
        }

        static Bounds BuildWaterBounds(WaterSurface surface)
        {
            Vector3 size = new Vector3(surface.size.x, 0.05f, surface.size.y);
            return new Bounds(surface.position, size);
        }

        static List<Vector3> ComputeFrustumWaterIntersections(
            Camera cam,
            WaterSurface surface)
        {
            GetCameraFrustumCorners(cam, out var nearC, out var farC);

            Plane waterPlane = BuildWaterPlane(surface);
            List<Vector3> hits = new();

            Vector3 camPos = cam.transform.position;

            // Cast rays through FAR plane corners
            for (int i = 0; i < 4; i++)
            {
                Vector3 dir = (farC[i] - camPos).normalized;

                if (IntersectPlane(waterPlane, camPos, dir, out Vector3 hit))
                {
                    hits.Add(hit);
                }
            }

            return hits;
        }

        static bool IntersectPlane(
            Plane plane,
            Vector3 rayOrigin,
            Vector3 rayDir,
            out Vector3 hit)
        {
            if (plane.Raycast(new Ray(rayOrigin, rayDir), out float enter))
            {
                hit = rayOrigin + rayDir * enter;
                return true;
            }

            hit = default;
            return false;
        }

        static Plane BuildWaterPlane(WaterSurface surface)
        {
            return new Plane(surface.normal, surface.position);
        }
        static void GetCameraFrustumCorners(
            Camera cam,
            out Vector3[] nearCorners,
            out Vector3[] farCorners)
        {
            nearCorners = new Vector3[4];
            farCorners = new Vector3[4];

            Transform t = cam.transform;

            float near = cam.nearClipPlane;
            float far = cam.farClipPlane;
            float fov = cam.fieldOfView;
            float aspect = cam.aspect;

            float halfFovRad = Mathf.Deg2Rad * fov * 0.5f;

            float nearHeight = Mathf.Tan(halfFovRad) * near;
            float nearWidth = nearHeight * aspect;

            float farHeight = Mathf.Tan(halfFovRad) * far;
            float farWidth = farHeight * aspect;

            Vector3 nc = t.position + t.forward * near;
            Vector3 fc = t.position + t.forward * far;

            // Near
            nearCorners[0] = nc + t.up * nearHeight - t.right * nearWidth; // TL
            nearCorners[1] = nc + t.up * nearHeight + t.right * nearWidth; // TR
            nearCorners[2] = nc - t.up * nearHeight + t.right * nearWidth; // BR
            nearCorners[3] = nc - t.up * nearHeight - t.right * nearWidth; // BL

            // Far
            farCorners[0] = fc + t.up * farHeight - t.right * farWidth;
            farCorners[1] = fc + t.up * farHeight + t.right * farWidth;
            farCorners[2] = fc - t.up * farHeight + t.right * farWidth;
            farCorners[3] = fc - t.up * farHeight - t.right * farWidth;
        }

    }

    public static class RayPlaneUtil
    {
        public static bool RayPlaneIntersection(
            Vector3 position,
            Vector3 direction,
            Vector3 planePoint,
            Vector3 planeNormal,
            out Vector3 hitPoint)
        {
            hitPoint = default;

            float denom = Vector3.Dot(planeNormal, direction);

            // Ray is parallel to the plane
            if (Mathf.Abs(denom) < 1e-6f)
                return false;

            float t = Vector3.Dot(planeNormal, planePoint - position) / denom;

            // Intersection is behind the ray origin
            if (t < 0f)
                return false;

            hitPoint = position + direction * t;
            return true;
        }
    }

}