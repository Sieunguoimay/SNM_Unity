using UnityEngine;

namespace Snm.SurfaceInteraction
{
    public class SurfaceCanvas
    {
        public Vector3 Position;
        public Quaternion Rotation;
        public Vector2 Size;

        public Vector2 WorldToUV(Vector3 worldPos)
        {
            Vector3 local = Quaternion.Inverse(Rotation) * (worldPos - Position);
            float u = local.x / Size.x + 0.5f;
            float v = local.z / Size.y + 0.5f;
            return new Vector2(u, v);
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

        public Vector2 WorldMin => new(Position.x - Size.x * 0.5f, Position.z - Size.y * 0.5f);
        public Vector2 WorldMax => new(Position.x + Size.x * 0.5f, Position.z + Size.y * 0.5f);
    }
}
