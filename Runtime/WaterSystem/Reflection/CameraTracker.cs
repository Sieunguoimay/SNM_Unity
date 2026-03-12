using Snm.Reactivity;
using UnityEngine;

namespace Snm.WaterSystem.Reflection
{
    public class CameraTracker
    {
        private const float MinMoveSqr = 0.0001f;
        private const float MinAngleDelta = 0.1f;

        private readonly Camera _camera;

        private Vector3 _lastPosition;
        private Quaternion _lastRotation;

        public readonly Signal<Vector3> Position;
        public readonly Signal<Quaternion> Rotation;

        public CameraTracker(Camera camera)
        {
            _camera = camera;

            camera.transform.GetPositionAndRotation(out _lastPosition, out _lastRotation);
            Position = new Signal<Vector3>(_lastPosition);
            Rotation = new Signal<Quaternion>(_lastRotation);
        }

        public void Poll()
        {
            _camera.transform.GetPositionAndRotation(out var pos, out var rot);

            bool moved = (pos - _lastPosition).sqrMagnitude > MinMoveSqr;
            bool rotated = Quaternion.Angle(_lastRotation, rot) > MinAngleDelta;

            if (!moved && !rotated) return;

            _lastPosition = pos;
            _lastRotation = rot;

            Position.Value = pos;
            Rotation.Value = rot;
        }
    }
}
