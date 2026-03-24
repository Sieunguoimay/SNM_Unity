using System;
using System.Collections.Generic;
using UnityEngine;

namespace Snm.Runtime.DebugDraw
{
    /// <summary>
    /// Stacks multiple debug labels vertically above a Transform.
    ///
    ///   var panel = DebugDraw.Panel(transform);
    ///   panel.Add("mass",  () => rb.mass);
    ///   panel.Add("speed", () => rb.velocity.magnitude);
    ///   panel.Add("hp",    () => hp, maxGetter: () => maxHp, showBar: true);
    ///
    /// Dispose the panel to release all labels at once.
    /// </summary>
    public sealed class StatPanel : IDisposable
    {
        private readonly LabelDrawer       _drawer;
        private readonly Transform         _target;
        private readonly Vector3           _baseOffset;
        private readonly float             _spacing;
        private readonly List<LabelHandle> _labels = new();
        private bool _disposed;

        internal StatPanel(LabelDrawer drawer, Transform target, Vector3 baseOffset, float spacing)
        {
            _drawer     = drawer;
            _target     = target;
            _baseOffset = baseOffset;
            _spacing    = spacing;
        }

        // ── Add ───────────────────────────────────────────────────────────────

        /// <summary>Adds a label with a fully custom live text getter.</summary>
        public LabelHandle Add(Func<string> textGetter, bool autoUpdate = true, Color? color = null)
        {
            if (_disposed) return null;
            LabelHandle h = null;
            h = _drawer.Show(textGetter, _target, SlotOffset(_labels.Count), color,
                autoUpdate: autoUpdate,
                onDispose: () => OnLabelDisposed(h));
            _labels.Add(h);
            return h;
        }

        /// <summary>Adds a "name: value" stat label, with optional progress bar.</summary>
        public LabelHandle Add(string name, Func<float> valueGetter,
            Func<float> maxGetter = null, bool showBar = false, bool autoUpdate = true, Color? color = null)
        {
            if (_disposed) return null;
            LabelHandle h = null;
            h = _drawer.Show(
                () => $"{name}: {valueGetter():F1}",
                _target, SlotOffset(_labels.Count), color,
                autoUpdate: autoUpdate,
                showBar: showBar, barCurrent: valueGetter, barMax: maxGetter,
                onDispose: () => OnLabelDisposed(h));
            _labels.Add(h);
            return h;
        }

        // ── Remove ────────────────────────────────────────────────────────────

        /// <summary>Removes and disposes a specific label, shifting the rest down.</summary>
        public void Remove(LabelHandle handle)
        {
            if (handle == null || !_labels.Remove(handle)) return;
            handle.Dispose();
            Relayout();
        }

        // ── Internal ──────────────────────────────────────────────────────────

        private void OnLabelDisposed(LabelHandle handle)
        {
            if (_labels.Remove(handle)) Relayout();
        }

        private Vector3 SlotOffset(int index) => _baseOffset + Vector3.up * (index * _spacing);

        private void Relayout()
        {
            for (int i = 0; i < _labels.Count; i++)
                _labels[i].SetOffset(SlotOffset(i));
        }

        // ── Lifetime ─────────────────────────────────────────────────────────

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            foreach (var h in new List<LabelHandle>(_labels)) h.Dispose();
            _labels.Clear();
        }
    }
}
