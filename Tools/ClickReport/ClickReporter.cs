#if UNITY_EDITOR
using UnityEngine;
using UnityEngine.EventSystems;
using System.Collections.Generic;
using System;

namespace Snm.Tools
{

    public class ClickReporter : MonoBehaviour
    {
        public static GameObject LastClickedObject { get; private set; }
        public static bool IsUIObject { get; private set; }
        public static string DetectionMethod { get; private set; }

        [Header("3D Raycast Settings")]
        [SerializeField] private LayerMask raycastLayers = -1;
        [SerializeField] private float maxRaycastDistance = 1000f;

        [Header("Mesh Raycast Settings")]
        [SerializeField] private bool enableMeshRaycast = true;
        [SerializeField] private float meshRaycastMaxDistance = 100f;

        [Header("2D Raycast Settings")]
        [SerializeField] private bool enable2DRaycast = true;
        [SerializeField] private LayerMask raycast2DLayers = -1;

        [Header("Debug")]
        [SerializeField] private bool showDebugLogs = true;
        [SerializeField] private bool showDebugRays = true;

        public event Action<ClickReporter, GameObject> OnClickDetected;

        void Update()
        {
            if (Input.GetMouseButtonDown(0))
            {
                DetectClick();
            }
        }

        void DetectClick()
        {
            // Priority 1: Check for UI clicks
            if (DetectUIClick())
            {
                IsUIObject = true;
                DetectionMethod = "UI EventSystem";
                if (showDebugLogs)
                    Debug.Log($"[UI] Clicked: {LastClickedObject.name}", LastClickedObject);
                return;
            }

            foreach (var cam in FindObjectsOfType<Camera>())
            {


                // Priority 2: Check for 3D objects with colliders
                if (Detect3DColliderClick(cam))
                {
                    IsUIObject = false;
                    DetectionMethod = "Physics Raycast";
                    if (showDebugLogs)
                        Debug.Log($"[3D Collider] Clicked: {LastClickedObject.name}", LastClickedObject);
                    return;
                }

                // Priority 3: Check for 2D sprites/colliders
                if (enable2DRaycast && Detect2DClick(cam))
                {
                    IsUIObject = false;
                    DetectionMethod = "Physics2D Raycast";
                    if (showDebugLogs)
                        Debug.Log($"[2D] Clicked: {LastClickedObject.name}", LastClickedObject);
                    return;
                }

                // Priority 4: Check for 3D meshes without colliders
                if (enableMeshRaycast && DetectMeshClick(cam))
                {
                    IsUIObject = false;
                    DetectionMethod = "Mesh Raycast";
                    if (showDebugLogs)
                        Debug.Log($"[Mesh] Clicked: {LastClickedObject.name}", LastClickedObject);
                    return;
                }
            }

            // Nothing was clicked
            if (showDebugLogs)
                Debug.Log("Clicked on nothing");
        }

        bool DetectUIClick()
        {
            if (EventSystem.current == null)
                return false;

            PointerEventData pointerData = new PointerEventData(EventSystem.current)
            {
                position = Input.mousePosition
            };

            List<RaycastResult> results = new List<RaycastResult>();
            EventSystem.current.RaycastAll(pointerData, results);

            if (results.Count > 0)
            {
                LastClickedObject = results[0].gameObject;
                OnClickDetected?.Invoke(this, LastClickedObject);
                return true;
            }

            return false;
        }

        bool Detect3DColliderClick(Camera cam)
        {
            Ray ray = cam.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;

            if (showDebugRays)
                Debug.DrawRay(ray.origin, ray.direction * maxRaycastDistance, Color.red, 1f);

            if (Physics.Raycast(ray, out hit, maxRaycastDistance, raycastLayers))
            {
                LastClickedObject = hit.collider.gameObject;
                OnClickDetected?.Invoke(this, LastClickedObject);
                return true;
            }

            return false;
        }

        bool Detect2DClick(Camera cam)
        {
            Vector2 rayOrigin = cam.ScreenToWorldPoint(Input.mousePosition);
            RaycastHit2D hit = Physics2D.Raycast(rayOrigin, Vector2.zero, 0f, raycast2DLayers);

            if (hit.collider != null)
            {
                LastClickedObject = hit.collider.gameObject;
                OnClickDetected?.Invoke(this, LastClickedObject);
                return true;
            }

            return false;
        }

        bool DetectMeshClick(Camera cam)
        {
            Ray ray = cam.ScreenPointToRay(Input.mousePosition);
            ray.direction = ray.direction.normalized;

            if (showDebugRays)
                Debug.DrawRay(ray.origin, ray.direction * meshRaycastMaxDistance, Color.yellow, 1f);

            float closestDistance = float.MaxValue;
            GameObject closestObject = null;
            Vector3 closestPoint = Vector3.zero;

            // -------- Static Meshes --------
            foreach (var renderer in FindObjectsOfType<MeshRenderer>())
            {
                if (!renderer.enabled || !renderer.gameObject.activeInHierarchy)
                    continue;

                // 🔑 CRITICAL: ray must intersect bounds first
                if (!renderer.bounds.IntersectRay(ray, out float distance))
                    continue;


                if (distance < closestDistance && distance <= meshRaycastMaxDistance)
                {
                    closestDistance = distance;
                    closestObject = renderer.gameObject;
                }
            }

            // -------- Skinned Meshes --------
            foreach (var renderer in FindObjectsOfType<SkinnedMeshRenderer>())
            {
                if (!renderer.enabled || !renderer.gameObject.activeInHierarchy)
                    continue;

                if (!renderer.bounds.IntersectRay(ray, out float distance))
                    continue;

                if (distance < closestDistance && distance <= meshRaycastMaxDistance)
                {
                    closestDistance = distance;
                    closestObject = renderer.gameObject;
                }
            }

            if (closestObject != null)
            {
                LastClickedObject = closestObject;
                OnClickDetected?.Invoke(this, LastClickedObject);
                return true;
            }

            return false;
        }

        bool RaycastMesh(
            Ray ray,
            Mesh mesh,
            Transform transform,
            out Vector3 hitPoint,
            out float distance)
        {
            hitPoint = Vector3.zero;
            distance = float.MaxValue;

            var vertices = mesh.vertices;
            var triangles = mesh.triangles;

            bool hit = false;

            for (int i = 0; i < triangles.Length; i += 3)
            {
                Vector3 v0 = transform.TransformPoint(vertices[triangles[i]]);
                Vector3 v1 = transform.TransformPoint(vertices[triangles[i + 1]]);
                Vector3 v2 = transform.TransformPoint(vertices[triangles[i + 2]]);

                // 🔑 Backface culling
                Vector3 normal = Vector3.Cross(v1 - v0, v2 - v0);
                if (Vector3.Dot(normal, ray.direction) >= 0f)
                    continue;

                if (RayIntersectsTriangle(ray, v0, v1, v2, out Vector3 intersection, out float t))
                {
                    if (t < distance)
                    {
                        distance = t;
                        hitPoint = intersection;
                        hit = true;
                    }
                }
            }

            return hit;
        }

        bool RayIntersectsTriangle(Ray ray, Vector3 v0, Vector3 v1, Vector3 v2, out Vector3 hitPoint, out float t)
        {
            hitPoint = Vector3.zero;
            t = 0;

            // Möller–Trumbore intersection algorithm
            Vector3 edge1 = v1 - v0;
            Vector3 edge2 = v2 - v0;
            Vector3 h = Vector3.Cross(ray.direction, edge2);
            float a = Vector3.Dot(edge1, h);

            if (a > -0.00001f && a < 0.00001f)
                return false; // Ray is parallel to triangle

            float f = 1.0f / a;
            Vector3 s = ray.origin - v0;
            float u = f * Vector3.Dot(s, h);

            if (u < 0.0f || u > 1.0f)
                return false;

            Vector3 q = Vector3.Cross(s, edge1);
            float v = f * Vector3.Dot(ray.direction, q);

            if (v < 0.0f || u + v > 1.0f)
                return false;

            t = f * Vector3.Dot(edge2, q);

            if (t > 0.00001f)
            {
                hitPoint = ray.origin + ray.direction * t;
                return true;
            }

            return false;
        }

        bool IsVisibleFromCamera(Bounds bounds, Camera camera)
        {
            Plane[] planes = GeometryUtility.CalculateFrustumPlanes(camera);
            return GeometryUtility.TestPlanesAABB(planes, bounds);
        }
    }
}
#endif