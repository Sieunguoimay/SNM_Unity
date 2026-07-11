using UnityEngine;

namespace Snm.GrassSystemV2
{
    /// <summary>
    /// Authoring helper for covering large areas fast: drop a box, set rules,
    /// press "Fill Volume" in the inspector. Filling BAKES instances into the
    /// world's GrassWorldData — the volume does nothing at runtime and can be
    /// deleted afterwards (kept around it makes re-filling easy after level
    /// changes). Painted results remain editable with the brush.
    /// </summary>
    [AddComponentMenu("Snm/Grass System V2/Grass Volume")]
    public class GrassVolume : MonoBehaviour
    {
        [Header("Area")]
        [Tooltip("Local XZ size of the box footprint to fill.")]
        public Vector2 areaSize = new(20f, 20f);

        [Header("Rules")]
        [Tooltip("Blades per square meter.")]
        public float density = 4f;

        [Tooltip("Indices into the world data's types to scatter, picked randomly per blade. Empty = type 0.")]
        public int[] typeIndices = { 0 };

        [Tooltip("Only surfaces on these layers receive grass.")]
        public LayerMask groundMask = ~0;

        [Range(0f, 90f)]
        [Tooltip("Skip ground steeper than this (degrees from horizontal).")]
        public float maxSlopeAngle = 40f;

        [Tooltip("Seed for reproducible scattering. Same seed = same result.")]
        public int randomSeed = 12345;

        [Header("Raycast")]
        [Tooltip("Height above the volume to start the downward ground raycast.")]
        public float raycastOriginHeight = 50f;

        [Tooltip("Maximum raycast distance downward.")]
        public float raycastMaxDistance = 200f;

        void OnDrawGizmosSelected()
        {
            var size = new Vector3(areaSize.x, 0.05f, areaSize.y);
            Gizmos.matrix = Matrix4x4.TRS(transform.position, Quaternion.Euler(0f, transform.eulerAngles.y, 0f), Vector3.one);
            Gizmos.color = new Color(0.2f, 0.8f, 0.3f, 0.12f);
            Gizmos.DrawCube(Vector3.zero, size);
            Gizmos.color = new Color(0.2f, 0.8f, 0.3f, 0.8f);
            Gizmos.DrawWireCube(Vector3.zero, size);
            Gizmos.matrix = Matrix4x4.identity;
        }
    }
}
