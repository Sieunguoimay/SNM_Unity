using Snm.Reactivity;
using Snm.SurfaceInteraction;
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

        public Vector3[] ComputeWaterCorners(SurfaceCanvas surface)
        {
            var right = surface.Rotation * Vector3.right * surface.Size.x * 0.5f;
            var forward = surface.Rotation * Vector3.forward * surface.Size.y * 0.5f;
            var center = surface.Position;

            _waterCorners[0] = center - right - forward;
            _waterCorners[1] = center - right + forward;
            _waterCorners[2] = center + right + forward;
            _waterCorners[3] = center + right - forward;

            return _waterCorners;
        }
    }
}
