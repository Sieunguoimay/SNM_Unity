using UnityEngine;

namespace Snm.Runtime.GrassSystem
{
    [ExecuteInEditMode]
    public class GrassTrampleBrushMB : MonoBehaviour, IGrassDisturber
    {
        [SerializeField] private float radius = 0.5f;

        public Vector3 WorldPosition => transform.position;
        public float GrassContactRadius => radius;

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, radius);
        }
    }
}
