using UnityEngine;

namespace Snm.GrassSystemV2
{
    /// <summary>
    /// Pure math for the chunk grid: world &lt;-&gt; chunk coordinate mapping,
    /// bounds construction, deterministic hashing. No Unity object state,
    /// fully testable.
    /// </summary>
    public static class GrassGridMath
    {
        /// <summary>Chunk coordinate containing the given world position (XZ plane).</summary>
        public static Vector2Int WorldToChunk(Vector3 worldPosition, float chunkSize)
        {
            return new Vector2Int(
                Mathf.FloorToInt(worldPosition.x / chunkSize),
                Mathf.FloorToInt(worldPosition.z / chunkSize));
        }

        /// <summary>World-space origin (min corner on XZ) of a chunk.</summary>
        public static Vector3 ChunkOrigin(Vector2Int coord, float chunkSize)
        {
            return new Vector3(coord.x * chunkSize, 0f, coord.y * chunkSize);
        }

        /// <summary>World-space center of a chunk on the XZ plane (y = 0).</summary>
        public static Vector3 ChunkCenter(Vector2Int coord, float chunkSize)
        {
            return new Vector3((coord.x + 0.5f) * chunkSize, 0f, (coord.y + 0.5f) * chunkSize);
        }

        /// <summary>
        /// World-space AABB of a chunk. minY/maxY come from the painted instances;
        /// <paramref name="padding"/> accounts for blade height and wind sway so
        /// frustum tests stay conservative.
        /// </summary>
        public static Bounds ChunkBounds(Vector2Int coord, float chunkSize, float minY, float maxY, float padding)
        {
            float y0 = minY - padding;
            float y1 = maxY + padding;
            var center = new Vector3(
                (coord.x + 0.5f) * chunkSize,
                (y0 + y1) * 0.5f,
                (coord.y + 0.5f) * chunkSize);
            var size = new Vector3(chunkSize + padding * 2f, y1 - y0, chunkSize + padding * 2f);
            return new Bounds(center, size);
        }

        /// <summary>Squared XZ distance from a point to a chunk's rectangle (0 when inside).</summary>
        public static float SqrDistanceToChunkXZ(Vector3 point, Vector2Int coord, float chunkSize)
        {
            float minX = coord.x * chunkSize;
            float minZ = coord.y * chunkSize;
            float dx = Mathf.Max(minX - point.x, 0f, point.x - (minX + chunkSize));
            float dz = Mathf.Max(minZ - point.z, 0f, point.z - (minZ + chunkSize));
            return dx * dx + dz * dz;
        }

        /// <summary>Knuth multiplicative hash mapped to [0..1).</summary>
        public static float Hash01(uint value)
        {
            unchecked
            {
                return (value * 2654435761u) / 4294967296f;
            }
        }

        /// <summary>Deterministic seed for a chunk, stable across sessions and machines.</summary>
        public static int ChunkSeed(Vector2Int coord)
        {
            unchecked
            {
                return coord.x * 73856093 ^ coord.y * 19349663;
            }
        }

        /// <summary>
        /// Density factor in [0..1] for distance-based thinning: 1 inside
        /// <paramref name="falloffStart"/>, fading to 0 at <paramref name="maxDistance"/>.
        /// </summary>
        public static float DensityFactor(float distance, float falloffStart, float maxDistance)
        {
            if (maxDistance <= falloffStart) return distance <= maxDistance ? 1f : 0f;
            return 1f - Mathf.Clamp01((distance - falloffStart) / (maxDistance - falloffStart));
        }
    }
}
