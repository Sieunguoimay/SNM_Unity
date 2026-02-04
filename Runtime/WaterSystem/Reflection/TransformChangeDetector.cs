using UnityEngine;

namespace Snm.Runtime.WaterSystem
{
    public class TransformChangeDetector
    {
        private readonly Transform source;
        private readonly float minDistance;
        private readonly float angleThresholdDeg;

        private Vector3 _oldPos;
        private Quaternion _oldRot;

        public TransformChangeDetector(
            Transform source,
            float minDistance,
            float angleThresholdDeg)
        {
            this.source = source;
            this.minDistance = minDistance;
            this.angleThresholdDeg = angleThresholdDeg;

            _oldPos = source.position;
            _oldRot = source.rotation;
        }

        public bool HasChanged()
        {
            source.GetPositionAndRotation(out var pos, out var rot);
            
            var changed =
                (pos - _oldPos).sqrMagnitude > minDistance * minDistance ||
                Quaternion.Angle(_oldRot, rot) > angleThresholdDeg;

            if (changed)
            {
                _oldPos = pos;
                _oldRot = rot;
            }

            return changed;
        }
    }
}