using UnityEngine;

namespace Snm.Visual.Layout3D
{
    public interface IWorldBounds
    {
        Bounds Bounds { get; }
        Transform Transform { get; }
    }

    public class WorldBounds : MonoBehaviour, IWorldBounds
    {
        [SerializeField] private Bounds bounds;

        Bounds IWorldBounds.Bounds => bounds;
        Transform IWorldBounds.Transform => transform;

        public Vector3 GetRandomPosInside()
        {
            var x = Random.Range(bounds.min.x, bounds.max.x);
            var y = Random.Range(bounds.min.y, bounds.max.y);
            var z = Random.Range(bounds.min.z, bounds.max.z);
            return transform.TransformPoint(new Vector3(x, y, z));
        }

        private void OnDrawGizmos()
        {
            Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.DrawWireCube(bounds.center, bounds.size);
        }
    }
}
