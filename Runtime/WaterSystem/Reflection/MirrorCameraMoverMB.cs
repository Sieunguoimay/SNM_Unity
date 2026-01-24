using UnityEngine;

namespace Snm.Runtime.WaterSystem
{
    [ExecuteInEditMode]
    public class MirrorCameraMoverMB : MonoBehaviour
    {
        private MirrorCameraMover _mover;

        public void SetMover(MirrorCameraMover mover) { _mover = mover; }

        private void Update()
        {
            _mover?.Move();
        }
    }
}