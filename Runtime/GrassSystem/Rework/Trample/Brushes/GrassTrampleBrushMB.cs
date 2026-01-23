using UnityEngine;

namespace Snm.Runtime.GrassSystem
{
    [ExecuteInEditMode]
    public class GrassTrampleBrushMB : MonoBehaviour
    {
        [SerializeField] private float radius = 0.5f;
        [SerializeField] private float strength = 1f;

        private GrassTrampleBrush _brush;

        public GrassTrampleBrush Brush => _brush ??= CreateBrush();

        private void OnEnable()
        {
            _brush ??= CreateBrush();
        }

        private void OnDisable()
        {
            _brush = null;
        }

        private void Update()
        {
            if (_brush != null)
            {
                _brush.position = transform.position;
            }
        }

        private GrassTrampleBrush CreateBrush()
        {
            return new GrassTrampleBrush
            {
                radius = radius,
                strength = strength,
                position = transform.position
            };
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, radius);
        }
    }
}