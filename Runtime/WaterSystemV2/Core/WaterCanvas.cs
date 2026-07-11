using UnityEngine;

namespace Snm.WaterSystemV2
{
    /// <summary>
    /// World ↔ UV mapping for a rectangular water surface lying in its local
    /// XZ plane. Pure math over three fields — no Unity object state, fully
    /// testable. One instance is owned by <see cref="WaterBody"/> and shared
    /// by reference with the dynamic systems (waves, buoyancy, reflection).
    /// </summary>
    public class WaterCanvas
    {
        public Vector3 Position;
        public Quaternion Rotation = Quaternion.identity;
        public Vector2 Size = new(30f, 30f);

        /// <summary>World-space height of the water plane at its origin.</summary>
        public float SurfaceY => Position.y;

        public Vector2 WorldToUV(Vector3 worldPos)
        {
            Vector3 local = Quaternion.Inverse(Rotation) * (worldPos - Position);
            return new Vector2(
                local.x / Size.x + 0.5f,
                local.z / Size.y + 0.5f);
        }

        public Vector3 UVToWorld(Vector2 uv)
        {
            float x = (uv.x - 0.5f) * Size.x;
            float z = (uv.y - 0.5f) * Size.y;
            return Position + Rotation * new Vector3(x, 0f, z);
        }

        public bool Contains(Vector3 worldPos)
        {
            Vector3 local = Quaternion.Inverse(Rotation) * (worldPos - Position);
            return Mathf.Abs(local.x) <= Size.x * 0.5f
                && Mathf.Abs(local.z) <= Size.y * 0.5f;
        }

        public bool Overlaps(Vector3 worldPos, float radius)
        {
            Vector3 local = Quaternion.Inverse(Rotation) * (worldPos - Position);
            return Mathf.Abs(local.x) <= Size.x * 0.5f + radius
                && Mathf.Abs(local.z) <= Size.y * 0.5f + radius;
        }

        /// <summary>Converts a world-space radius to UV space (relative to the longer side).</summary>
        public float WorldRadiusToUV(float worldRadius)
            => worldRadius / Mathf.Max(Size.x, Size.y);

        public void ComputeCorners(Vector3[] corners)
        {
            var right = Rotation * Vector3.right * (Size.x * 0.5f);
            var forward = Rotation * Vector3.forward * (Size.y * 0.5f);

            corners[0] = Position - right - forward;
            corners[1] = Position - right + forward;
            corners[2] = Position + right + forward;
            corners[3] = Position + right - forward;
        }
    }
}
