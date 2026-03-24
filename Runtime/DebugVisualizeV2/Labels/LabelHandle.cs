using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Snm.Runtime.DebugDraw
{
    /// <summary>
    /// A live handle to a pooled debug label.
    /// Update offset or text each frame, or dispose when done.
    /// </summary>
    public sealed class LabelHandle : IDisposable
    {
        // Unity objects owned by LabelDrawer, reused via pool
        internal readonly Canvas        Canvas;
        internal readonly RectTransform Rect;
        private  readonly TextMeshPro   _tmp;
        private  readonly RectTransform _barBg;
        private  readonly Image         _barFill;

        // Per-activation state
        private Transform    _target;     // null → use fixed world position
        private Vector3      _fixedPos;
        private Vector3      _offset;
        private Func<string> _textGetter;
        private Func<float>  _barCurrent;
        private Func<float>  _barMax;
        private bool         _autoUpdate;
        private bool         _autoHide;
        private Camera       _cam;
        private Action       _onReturn;

        internal bool IsActive;

        internal LabelHandle(Canvas canvas, RectTransform rect, TextMeshPro tmp, RectTransform barBg, Image barFill)
        {
            Canvas = canvas; Rect = rect; _tmp = tmp; _barBg = barBg; _barFill = barFill;
        }

        // ── Internal setup (called by LabelDrawer) ────────────────────────────

        internal void Activate(
            Func<string> textGetter,
            Transform    target,
            Vector3      fixedPos,
            Vector3      offset,
            Color        color,
            float        fontSize,
            bool         autoUpdate,
            bool         showBar,
            Func<float>  barCurrent,
            Func<float>  barMax,
            bool         autoHide,
            Action       onReturn)
        {
            _textGetter = textGetter;
            _target     = target;
            _fixedPos   = fixedPos;
            _offset     = offset;
            _autoUpdate = autoUpdate;
            _autoHide   = autoHide;
            _barCurrent = barCurrent;
            _barMax     = barMax;
            _cam        = Camera.main;
            _onReturn   = onReturn;
            IsActive    = true;

            _tmp.color    = color;
            _tmp.fontSize = fontSize;
            _barBg.gameObject.SetActive(showBar);
            Canvas.enabled = true;

            Refresh();
        }

        // ── Frame update (called by LabelDrawer.Tick) ─────────────────────────

        internal void Tick()
        {
            var worldPos = _target != null
                ? _target.position + _offset
                : _fixedPos + _offset;

            Rect.position = worldPos;

            if (_cam != null)
                Rect.rotation = Quaternion.LookRotation(_cam.transform.forward);

            if (_autoHide && _cam != null)
            {
                var  sp  = _cam.WorldToScreenPoint(worldPos);
                bool vis = sp.z > 0 && sp.x >= 0 && sp.x <= Screen.width && sp.y >= 0 && sp.y <= Screen.height;
                if (Canvas.enabled != vis) Canvas.enabled = vis;
            }

            if (_autoUpdate) Refresh();
        }

        // ── Public API ────────────────────────────────────────────────────────

        /// <summary>Changes the world-space offset applied on top of the target's position.</summary>
        public void SetOffset(Vector3 offset) => _offset = offset;

        /// <summary>Forces an immediate text and bar refresh.</summary>
        public void Refresh()
        {
            _tmp.text = _textGetter?.Invoke() ?? string.Empty;

            if (_barBg.gameObject.activeSelf && _barCurrent != null && _barMax != null)
            {
                float max = _barMax();
                _barFill.fillAmount = max > 0 ? Mathf.Clamp01(_barCurrent() / max) : 0f;
            }
        }

        /// <summary>Replaces the text getter and immediately updates.</summary>
        public void SetText(string text) { _textGetter = () => text; _tmp.text = text; }

        /// <summary>Returns the label to the pool. Equivalent to Dispose().</summary>
        public void Release() => Dispose();

        public void Dispose()
        {
            if (!IsActive) return;
            IsActive       = false;
            Canvas.enabled = false;
            _tmp.text      = string.Empty;
            _target        = null;
            var ret = _onReturn;
            _onReturn = null;
            ret?.Invoke();
        }
    }
}
