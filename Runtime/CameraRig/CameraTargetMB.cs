using UnityEngine;

namespace Snm.CameraRig
{
    public class CameraTargetMB : MonoBehaviour
    {
        private CameraTarget _target;
        private Vector3 _prevPosition;

        public void SetTarget(CameraTarget target)
        {
            _target = target;
            _prevPosition = transform.position;
        }

        private void FixedUpdate()
        {
            if (_target == null) return;

            var pos = transform.position;

            var b = _target.VisibleBounds;
            b.center = pos;
            _target.VisibleBounds = b;

            _target.Velocity = (pos - _prevPosition) / Time.fixedDeltaTime;
            _prevPosition = pos;
        }
    }
}
