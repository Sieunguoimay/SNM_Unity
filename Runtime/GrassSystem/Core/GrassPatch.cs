using System.Collections.Generic;
using UnityEngine;

namespace Snm.GrassSystem
{
    /// <summary>
    /// Authoring component for placing grass within a rectangular area.
    /// Place as a child of a <see cref="GrassSystem"/> GameObject.
    /// The Transform defines the center and orientation of the patch.
    /// </summary>
    public class GrassPatch : MonoBehaviour
    {
        [Header("Rendering")]
        public Mesh mesh;
        public Material material;

        [Header("Area")]
        [Tooltip("Local XZ extents of the rectangular patch.")]
        public Vector2 areaSize = new(5f, 5f);

        [Header("Distribution")]
        [Tooltip("Distance between blade grid cells in world units.")]
        public float cellSpacing = 0.5f;

        [Range(0f, 1f)]
        [Tooltip("Random XZ offset per blade as a fraction of cellSpacing. 0 = perfect grid, 1 = max scatter.")]
        public float jitter = 0.4f;

        [Tooltip("Seed for reproducible randomization. Same seed = same blade positions.")]
        public int randomSeed;

        [Header("Scale")]
        public float minScale = 0.8f;
        public float maxScale = 1.2f;

        [Header("Rotation")]
        [Range(0f, 360f)]
        public float minYaw;
        [Range(0f, 360f)]
        public float maxYaw = 360f;

        [Header("Placement Map (Optional)")]
        [Tooltip("Optional texture controlling density (R), rotation (G), scale (B) per cell. " +
                 "When assigned, grid size is derived from texture dimensions within the area.")]
        public Texture2D placementMap;
        [Range(0f, 1f)]
        public float densityThreshold = 0.1f;

        [Header("Terrain Height Snapping")]
        [Tooltip("Layer mask for terrain raycast. Only surfaces on these layers will receive grass.")]
        public LayerMask raycastMask = ~0;

        [Tooltip("How far above the patch's Y position to start the downward raycast.")]
        public float raycastOriginHeight = 50f;

        [Tooltip("Maximum raycast distance downward.")]
        public float raycastMaxDistance = 100f;

        /// <summary>
        /// Builds the instance matrices for all blades in this patch.
        /// Uses grid + jitter distribution with terrain height snapping.
        /// </summary>
        public Matrix4x4[] BuildMatrices()
        {
            if (mesh == null || material == null) return System.Array.Empty<Matrix4x4>();

            float halfX = areaSize.x * 0.5f;
            float halfZ = areaSize.y * 0.5f;

            int countX = Mathf.Max(1, Mathf.FloorToInt(areaSize.x / cellSpacing));
            int countZ = Mathf.Max(1, Mathf.FloorToInt(areaSize.y / cellSpacing));

            Color32[] pixels = null;
            int texW = 0, texH = 0;
            if (placementMap != null)
            {
                pixels = placementMap.GetPixels32();
                texW = placementMap.width;
                texH = placementMap.height;
            }

            var rng = new System.Random(randomSeed);
            var list = new List<Matrix4x4>(countX * countZ);
            var patchTransform = transform;

            for (int z = 0; z < countZ; z++)
            {
                for (int x = 0; x < countX; x++)
                {
                    // Grid position in local space (centered on patch)
                    float localX = -halfX + (x + 0.5f) * cellSpacing;
                    float localZ = -halfZ + (z + 0.5f) * cellSpacing;

                    // Jitter
                    float jitterRange = cellSpacing * jitter * 0.5f;
                    localX += (float)(rng.NextDouble() * 2.0 - 1.0) * jitterRange;
                    localZ += (float)(rng.NextDouble() * 2.0 - 1.0) * jitterRange;

                    // Placement map sampling
                    float density = 1f;
                    float yawNorm = -1f;   // -1 = use random
                    float scaleNorm = -1f; // -1 = use random

                    if (pixels != null)
                    {
                        float u = Mathf.InverseLerp(-halfX, halfX, localX);
                        float v = Mathf.InverseLerp(-halfZ, halfZ, localZ);
                        int px = Mathf.Clamp(Mathf.FloorToInt(u * texW), 0, texW - 1);
                        int pz = Mathf.Clamp(Mathf.FloorToInt(v * texH), 0, texH - 1);
                        var c = pixels[pz * texW + px];

                        density = c.r / 255f;
                        if (density < densityThreshold) continue;

                        yawNorm = c.g / 255f;
                        scaleNorm = c.b / 255f;
                    }

                    // Transform to world space (XZ only, Y comes from raycast)
                    var localPos = new Vector3(localX, 0f, localZ);
                    var worldPos = patchTransform.TransformPoint(localPos);

                    // Raycast down to snap to terrain
                    var rayOrigin = worldPos + Vector3.up * raycastOriginHeight;
                    if (!Physics.Raycast(rayOrigin, Vector3.down, out var hit, raycastMaxDistance, raycastMask))
                        continue; // No terrain below — skip this blade

                    worldPos.y = hit.point.y;

                    // Yaw rotation
                    float yaw;
                    if (yawNorm >= 0f)
                        yaw = Mathf.Lerp(minYaw, maxYaw, yawNorm);
                    else
                        yaw = Mathf.Lerp(minYaw, maxYaw, (float)rng.NextDouble());

                    var rot = patchTransform.rotation * Quaternion.Euler(0f, yaw, 0f);

                    // Scale
                    float scale;
                    if (scaleNorm >= 0f)
                        scale = Mathf.Lerp(minScale, maxScale, scaleNorm);
                    else
                        scale = Mathf.Lerp(minScale, maxScale, (float)rng.NextDouble());

                    list.Add(Matrix4x4.TRS(worldPos, rot, Vector3.one * scale));
                }
            }

            return list.ToArray();
        }

#if UNITY_EDITOR
        void OnDrawGizmosSelected()
        {
            var t = transform;
            var center = t.position;
            var size = new Vector3(areaSize.x, 0.02f, areaSize.y);

            Gizmos.matrix = Matrix4x4.TRS(center, t.rotation, Vector3.one);

            // Patch bounds
            Gizmos.color = new Color(0.2f, 0.8f, 0.2f, 0.15f);
            Gizmos.DrawCube(Vector3.zero, size);
            Gizmos.color = new Color(0.2f, 0.8f, 0.2f, 0.7f);
            Gizmos.DrawWireCube(Vector3.zero, size);

            Gizmos.matrix = Matrix4x4.identity;

            // Mesh preview at center
            if (mesh != null)
            {
                Gizmos.color = new Color(0.3f, 0.9f, 0.3f, 0.5f);
                Gizmos.DrawMesh(mesh, center, t.rotation);
            }

            // Estimated blade count label
            if (cellSpacing > 0f)
            {
                int countX = Mathf.Max(1, Mathf.FloorToInt(areaSize.x / cellSpacing));
                int countZ = Mathf.Max(1, Mathf.FloorToInt(areaSize.y / cellSpacing));
#if UNITY_EDITOR
                UnityEditor.Handles.color = Color.white;
                UnityEditor.Handles.Label(center + Vector3.up * 0.5f,
                    $"{gameObject.name}: ~{countX * countZ} blades");
#endif
            }
        }
#endif
    }
}
