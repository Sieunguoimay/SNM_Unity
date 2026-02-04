using UnityEngine;

namespace Snm.Runtime.WaterSystem
{
    public class TransformMirroringMover
    {
        private readonly WaterSurface waterSurface;
        private readonly Transform target;
        private readonly Transform mirror;

        public TransformMirroringMover(
            WaterSurface waterSurface,
            Transform target,
            Transform mirror)
        {
            this.waterSurface = waterSurface;
            this.target = target;
            this.mirror = mirror;
        }

        public void Move()
        {
            Vector3 planeNormal = waterSurface.rotation * Vector3.up;
            Vector3 planePoint = waterSurface.position;

            TransformMirroringUtil.Mirror(target, mirror, planePoint, planeNormal);
        }
    }
}