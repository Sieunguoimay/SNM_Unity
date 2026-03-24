using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Snm.Runtime.DebugDraw
{
    // Internal — all public API goes through DebugDraw.cs
    internal sealed class LabelDrawer : IDisposable
    {
        private readonly DebugDrawConfig    _cfg;
        private readonly GameObject         _root;
        private readonly Queue<LabelHandle> _pool   = new();
        private readonly List<LabelHandle>  _active = new();

        // ── Init ─────────────────────────────────────────────────────────────

        internal LabelDrawer(DebugDrawConfig cfg)
        {
            _cfg  = cfg;
            _root = new GameObject("[DebugDraw] Labels") { hideFlags = HideFlags.DontSave };
            for (int i = 0; i < cfg.labelPoolSize; i++) _pool.Enqueue(MakeHandle());
        }

        // ── Show (tracking Transform) ────────────────────────────────────────

        internal LabelHandle Show(
            Func<string> textGetter,
            Transform    target,
            Vector3?     offset     = null,
            Color?       color      = null,
            float        fontSize   = 0,
            bool         autoUpdate = false,
            bool         showBar    = false,
            Func<float>  barCurrent = null,
            Func<float>  barMax     = null,
            Action       onDispose  = null)
        {
            return Activate(textGetter, target, Vector3.zero, offset, color, fontSize, autoUpdate, showBar, barCurrent, barMax, onDispose);
        }

        // ── Show (fixed world position) ──────────────────────────────────────

        internal LabelHandle Show(
            Func<string> textGetter,
            Vector3      worldPos,
            Vector3?     offset     = null,
            Color?       color      = null,
            float        fontSize   = 0,
            bool         autoUpdate = false,
            bool         showBar    = false,
            Func<float>  barCurrent = null,
            Func<float>  barMax     = null,
            Action       onDispose  = null)
        {
            return Activate(textGetter, null, worldPos, offset, color, fontSize, autoUpdate, showBar, barCurrent, barMax, onDispose);
        }

        // ── Panel ────────────────────────────────────────────────────────────

        internal StatPanel CreatePanel(Transform target, Vector3 baseOffset, float spacing)
            => new(this, target, baseOffset, spacing);

        // ── Frame update ──────────────────────────────────────────────────────

        internal void Tick()
        {
            for (int i = _active.Count - 1; i >= 0; i--)
                _active[i].Tick();
        }

        // ── Pool ──────────────────────────────────────────────────────────────

        private LabelHandle Activate(
            Func<string> textGetter,
            Transform    target,
            Vector3      worldPos,
            Vector3?     offset,
            Color?       color,
            float        fontSize,
            bool         autoUpdate,
            bool         showBar,
            Func<float>  barCurrent,
            Func<float>  barMax,
            Action       onDispose)
        {
            var handle = _pool.Count > 0 ? _pool.Dequeue() : MakeHandle();
            _active.Add(handle);

            handle.Activate(
                textGetter,
                target, worldPos,
                offset   ?? _cfg.labelOffset,
                color    ?? _cfg.labelColor,
                fontSize > 0 ? fontSize : _cfg.fontSize,
                autoUpdate,
                showBar, barCurrent, barMax,
                _cfg.autoHideOffScreen,
                onReturn: () =>
                {
                    onDispose?.Invoke();
                    Return(handle);
                });

            return handle;
        }

        internal void Return(LabelHandle handle)
        {
            _active.Remove(handle);
            _pool.Enqueue(handle);
        }

        // ── Factory ───────────────────────────────────────────────────────────

        private LabelHandle MakeHandle()
        {
            var root = new GameObject("[DebugDraw] Label") { hideFlags = HideFlags.DontSave };
            root.transform.SetParent(_root.transform, false);

            // Canvas
            var canvas = root.AddComponent<Canvas>();
            canvas.renderMode      = RenderMode.WorldSpace;
            canvas.overrideSorting = true;
            canvas.sortingOrder    = 32767;
            canvas.enabled         = false;

            var rect = root.GetComponent<RectTransform>();
            rect.sizeDelta          = new Vector2(2, 1);
            rect.anchoredPosition3D = Vector3.zero;

            // Text
            var textGo   = new GameObject("Text") { hideFlags = HideFlags.DontSave };
            textGo.transform.SetParent(root.transform, false);
            var textRect = textGo.AddComponent<RectTransform>();
            textRect.anchorMin        = new Vector2(0.5f, 0.5f);
            textRect.anchorMax        = new Vector2(0.5f, 0.5f);
            textRect.pivot            = new Vector2(0.5f, 0.5f);
            textRect.anchoredPosition = Vector2.zero;
            var tmp = textGo.AddComponent<TextMeshPro>();
            tmp.font             = ResolveFont();
            tmp.fontSize         = _cfg.fontSize;
            tmp.alignment        = TextAlignmentOptions.Center;
            tmp.enableAutoSizing = false;
            textGo.AddComponent<ContentSizeFitter>().horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            if (tmp.fontMaterial != null)
            {
                tmp.fontMaterial.SetFloat(ShaderUtilities.ID_OutlineWidth, 0.25f);
                tmp.fontMaterial.SetColor(ShaderUtilities.ID_OutlineColor, Color.black);
            }

            // Progress bar background
            var barBgGo   = new GameObject("BarBg") { hideFlags = HideFlags.DontSave };
            barBgGo.transform.SetParent(root.transform, false);
            var barBgRect = barBgGo.AddComponent<RectTransform>();
            barBgRect.anchorMin        = new Vector2(0, 0);
            barBgRect.anchorMax        = new Vector2(1, 0);
            barBgRect.pivot            = new Vector2(0.5f, 0);
            barBgRect.anchoredPosition = new Vector2(0, -0.45f);
            barBgRect.sizeDelta        = new Vector2(1.5f, 0.15f);
            barBgGo.AddComponent<Image>().color = _cfg.barBgColor;

            // Progress bar fill
            var barFillGo   = new GameObject("BarFill") { hideFlags = HideFlags.DontSave };
            barFillGo.transform.SetParent(barBgGo.transform, false);
            var barFillRect = barFillGo.AddComponent<RectTransform>();
            barFillRect.anchorMin        = Vector2.zero;
            barFillRect.anchorMax        = Vector2.one;
            barFillRect.pivot            = new Vector2(0, 0.5f);
            barFillRect.anchoredPosition = Vector2.zero;
            barFillRect.sizeDelta        = Vector2.zero;
            var fill = barFillGo.AddComponent<Image>();
            fill.color      = _cfg.barFillColor;
            fill.type       = Image.Type.Filled;
            fill.fillMethod = Image.FillMethod.Horizontal;
            fill.fillAmount = 1f;

            barBgGo.SetActive(false);

            return new LabelHandle(canvas, rect, tmp, barBgRect, fill);
        }

        private TMP_FontAsset ResolveFont()
        {
            if (_cfg.font != null) return _cfg.font;
            if (TMP_Settings.defaultFontAsset != null) return TMP_Settings.defaultFontAsset;
            var found = Resources.FindObjectsOfTypeAll<TMP_FontAsset>();
            return found.Length > 0 ? found[0] : null;
        }

        // ── Cleanup ───────────────────────────────────────────────────────────

        public void Dispose()
        {
            foreach (var h in _active) h.Canvas.enabled = false;
            _active.Clear();
            if (_root) UnityEngine.Object.Destroy(_root);
        }
    }
}
