using System;
using UnityEngine;

namespace Snm.Runtime.GrassSystem
{
    [ExecuteInEditMode]
    public class InteractorTracePainterMB : MonoBehaviour
    {
        public float minDistance = 0.01f; // world units

        private Vector3 _lastPaintPos;
        private bool _hasPainted;

        private InteractorTracePainter _painter;
        private Action _paintCallback;
        private WorldCanvasChecker _worldCanvas;

        public void SetPainter(InteractorTracePainter painter, Action paintCallback)
        {
            _painter = painter;
            _paintCallback = paintCallback;
        }

        public void SetWorldCanvas(WorldCanvasChecker worldCanvas) { _worldCanvas = worldCanvas; }

        [ContextMenu("Paint")]
        private void PaintHere()
        {
            // _painter?.Paint(transform.position);
            // _paintCallback?.Invoke();
        }

        private void Update()
        {
            if (_painter == null) return;

            var pos = transform.position;

            if (!_worldCanvas.IsInWorldCanvas(pos)) return;

            if (!_hasPainted)
            {
                Paint(pos);
                return;
            }

            if ((pos - _lastPaintPos).sqrMagnitude >= minDistance * minDistance)
            {
                Paint(pos);
            }
        }

        void Paint(Vector3 pos)
        {
            _painter.Paint(pos, Vector3.Normalize(pos - _lastPaintPos));
            _lastPaintPos = pos;
            _hasPainted = true;
            _paintCallback?.Invoke();
        }
    }
}