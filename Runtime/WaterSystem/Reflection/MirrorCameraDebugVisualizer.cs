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
                _waterSurface.rotation * Vector3.up,
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
            // DrawExtrudedVolume();
            // DrawWaterFrustumIntersection();
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

            DrawQuad(near, c);
            DrawQuad(far, c);

            for (int i = 0; i < 4; i++) Gizmos.DrawLine(near[i], far[i]);
        }

        static Vector3[] ExtrudeToReflectionDepth(
            Camera cam,
            WaterSurface water,
            Vector3[] waterCorners,
            float reflectionDepth)
        {
            Vector3[] farCorners = new Vector3[4];
            Vector3 camPos = cam.transform.position;
            var planeNormal = (water.position - camPos).normalized;

            Plane depthPlane = new Plane(
                planeNormal,
                water.position + planeNormal * reflectionDepth
            );


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

        void DrawWaterSurface()
        {
            // Bounds b = BuildWaterBounds(_waterSurface);

            Gizmos.color = waterColor;
            // Gizmos.DrawWireCube(b.center, b.size);
            var c = WaterReflectionFrustumCalculator.GetWaterCorners(_waterSurface);
            DrawQuad(c[0], c[1], c[2], c[3]);
        }

        void DrawCameraFrustum()
        {
            var waterCorners = WaterReflectionFrustumCalculator.GetWaterCorners(_waterSurface);
            var waterPlaneCS = WaterReflectionFrustumCalculator.CameraSpacePlane(_reflectionCamera.worldToCameraMatrix, _waterSurface.position, _waterSurface.rotation * Vector3.up, 1);
            var projection = WaterReflectionFrustumCalculator.CalculateClampedWaterReflectionFrustum(_reflectionCamera, waterCorners);
            projection = WaterReflectionFrustumCalculator.CalculateObliqueMatrix(projection, waterPlaneCS);
            // var projection = _reflectionCamera.projectionMatrix;
            DrawFrustum(projection, _reflectionCamera.worldToCameraMatrix, frustumColor);
        }

        // void DrawWaterFrustumIntersection()
        // {
        //     List<Vector3> hits =
        //         ComputeFrustumWaterIntersections(_reflectionCamera, _waterSurface);

        //     Gizmos.color = hitColor;

        //     for (int i = 0; i < hits.Count; i++)
        //     {
        //         Gizmos.DrawSphere(hits[i], 0.2f);
        //         Gizmos.DrawLine(hits[i], hits[(i + 1) % hits.Count]);
        //     }
        // }
        static void DrawFrustum(
            Matrix4x4 proj,
            Matrix4x4 view,
            Color color)
        {
            Gizmos.color = color;

            Matrix4x4 invVP = (proj * view).inverse;

            Vector3[] near = new Vector3[4];
            Vector3[] far = new Vector3[4];

            int i = 0;

            for (int y = 0; y <= 1; y++)
                for (int x = 0; x <= 1; x++)
                {
                    float nx = x == 0 ? -1 : 1;
                    float ny = y == 0 ? -1 : 1;

                    Vector4 n = invVP * new Vector4(nx, ny, -1, 1);
                    Vector4 f = invVP * new Vector4(nx, ny, 1, 1);

                    near[i] = n / n.w;
                    far[i] = f / f.w;

                    i++;
                }

            DrawQuad(near);
            DrawQuad(far);

            for (int k = 0; k < 4; k++)
                Gizmos.DrawLine(near[k], far[k]);
        }

        static void DrawQuad(Vector3[] v)
        {
            Gizmos.DrawLine(v[0], v[1]);
            Gizmos.DrawLine(v[1], v[3]);
            Gizmos.DrawLine(v[3], v[2]);
            Gizmos.DrawLine(v[2], v[0]);
        }

        private void DrawPoints(Vector3[] c)
        {

            DrawQuad(c[0], c[1], c[2], c[3]);

            // far
            DrawQuad(c[4], c[5], c[6], c[7]);

            // sides
            for (int i = 0; i < 4; i++)
                Gizmos.DrawLine(c[i], c[i + 4]);
        }

        void DrawQuad(Vector3 a, Vector3 b, Vector3 c, Vector3 d)
        {
            Gizmos.DrawLine(a, b);
            Gizmos.DrawLine(b, c);
            Gizmos.DrawLine(c, d);
            Gizmos.DrawLine(d, a);
        }
        static Bounds BuildWaterBounds(WaterSurface surface)
        {
            Vector3 size = new Vector3(surface.size.x, 0.05f, surface.size.y);
            return new Bounds(surface.position, size);
        }

        // static List<Vector3> ComputeFrustumWaterIntersections(
        //     Camera cam,
        //     WaterSurface surface)
        // {
        //     GetFrustumCornersWorld(cam);

        //     Plane waterPlane = BuildWaterPlane(surface);
        //     List<Vector3> hits = new();

        //     Vector3 camPos = cam.transform.position;

        //     // Cast rays through FAR plane corners
        //     for (int i = 0; i < 4; i++)
        //     {
        //         Vector3 dir = (farC[i] - camPos).normalized;

        //         if (IntersectPlane(waterPlane, camPos, dir, out Vector3 hit))
        //         {
        //             hits.Add(hit);
        //         }
        //     }

        //     return hits;
        // }

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

        static Vector3[] GetFrustumCornersWorld(Camera cam)
        {
            Matrix4x4 vp = cam.projectionMatrix * cam.worldToCameraMatrix;
            return GetFrustumCornersWorld(vp);
        }

        static Vector3[] GetFrustumCornersWorld(Matrix4x4 vp)
        {
            Matrix4x4 invVP = vp.inverse;

            Vector3[] corners = new Vector3[8];

            int i = 0;
            for (int z = 0; z <= 1; z++)
            {
                float ndcZ = (z == 0) ? -1f : 1f;

                for (int y = -1; y <= 1; y += 2)
                    for (int x = -1; x <= 1; x += 2)
                    {
                        Vector4 ndc = new Vector4(x, y, ndcZ, 1);
                        Vector4 world = invVP * ndc;
                        world /= world.w;

                        corners[i++] = world;
                    }
            }

            return corners;
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