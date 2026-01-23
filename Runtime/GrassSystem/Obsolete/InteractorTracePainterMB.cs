using System;
using UnityEngine;

namespace Snm.Runtime.GrassSystem.Obsolete
{
    [ExecuteInEditMode]
    public class InteractorTracePainterMB : MonoBehaviour
    {
        public float minDistance = 0.01f; // world units

        private Vector3 _lastPaintPos;

        private InteractorTracePainter _painter;
        private Action _paintCallback;
        private WorldCanvasChecker _worldCanvas;
        private Vector3 _dir;
        private bool _paintRequired;
        private float _delay = 1f;

        public void SetPainter(InteractorTracePainter painter, Action paintCallback)
        {
            _painter = painter;
            _paintCallback = paintCallback;
        }

        public void SetWorldCanvas(WorldCanvasChecker worldCanvas) { _worldCanvas = worldCanvas; }

        [ContextMenu("Paint")]
        private void PaintHere()
        {
            SetPaintPos(transform.position);
            _paintCallback?.Invoke();
        }

        private void Update()
        {
            if (_painter == null) return;

            var pos = transform.position;

            if (_worldCanvas.IsInWorldCanvas(pos))
            {
                if ((pos - _lastPaintPos).sqrMagnitude >= minDistance * minDistance)
                {
                    SetPaintPos(pos);

                    _paintRequired = true;
                    _delay = 1f;
                }
            }

            if (_paintRequired)
            {
                _painter.Paint(_lastPaintPos, _dir, Time.deltaTime);
                _paintCallback?.Invoke();

                if (_delay > 0)
                {
                    _delay -= Time.deltaTime * .1f;
                }
                else
                {
                    _delay = 0f;
                    _paintRequired = false;
                }
            }
        }

        void SetPaintPos(Vector3 pos)
        {
            _dir = pos - _lastPaintPos;
            _lastPaintPos = pos;
        }
    }
}