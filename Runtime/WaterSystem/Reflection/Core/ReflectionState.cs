using Snm.Reactivity;
using Snm.WaterSystem.Surface;
using UnityEngine;

namespace Snm.WaterSystem.Reflection
{
    public class ReflectionState
    {
        public readonly Signal<Matrix4x4> Projection;
        public readonly Signal<bool> RenderRequested;

        private readonly Vector3[] _waterCorners = new Vector3[4];

        public ReflectionState()
        {
            Projection = new Signal<Matrix4x4>(Matrix4x4.identity);
            RenderRequested = new Signal<bool>(false);
        }

        public Vector3[] ComputeWaterCorners(SurfaceData surface)
        {
            var right = surface.rotation * Vector3.right * surface.size.x * 0.5f;
            var forward = surface.rotation * Vector3.forward * surface.size.y * 0.5f;
            var center = surface.position;

            _waterCorners[0] = center - right - forward;
            _waterCorners[1] = center - right + forward;
            _waterCorners[2] = center + right + forward;
            _waterCorners[3] = center + right - forward;

            return _waterCorners;
        }
    }
}
