using UnityEngine;

namespace Snm.Runtime.WaterSystem
{
    [ExecuteInEditMode]
    public class WaterSurfaceMB : MonoBehaviour
    {
        private WaterSurface _waterSurface;

        public void SetWaterSurface(WaterSurface waterSurface)
        {
            _waterSurface = waterSurface;
        }

        private void Update()
        {
            AssignWaterSurface(_waterSurface);
        }

        public void AssignWaterSurface(WaterSurface waterSurface)
        {
            waterSurface.position = transform.position;
            waterSurface.normal = transform.up;
        }
    }
}