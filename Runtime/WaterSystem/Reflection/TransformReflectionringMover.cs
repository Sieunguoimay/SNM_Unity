using UnityEngine;

namespace Snm.Runtime.WaterSystem
{
    public class TransformReflectionMover
    {
        private readonly WaterSurface waterSurface;
        private readonly Transform target;
        private readonly Transform reflection;

        public TransformReflectionMover(
            WaterSurface waterSurface,
            Transform target,
            Transform reflection)
        {
            this.waterSurface = waterSurface;
            this.target = target;
            this.reflection = reflection;
        }

        public void Move()
        {
            Vector3 planeNormal = waterSurface.rotation * Vector3.up;
            Vector3 planePoint = waterSurface.position;

            TransformReflectionUtil.Reflection(target, reflection, planePoint, planeNormal);
        }
    }
}