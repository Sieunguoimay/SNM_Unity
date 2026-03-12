using UnityEngine;

namespace Snm.WaterSystem.Surface
{
    [ExecuteInEditMode]
    public class SurfaceMB : MonoBehaviour
    {
        private SurfaceData _waterSurface;

        public void Bind(SurfaceData waterSurface)
        {
            _waterSurface = waterSurface;
        }

        private void Update()
        {
            if (_waterSurface == null) return;
            _waterSurface.position = transform.position;
            _waterSurface.rotation = transform.rotation;
        }
    }
}