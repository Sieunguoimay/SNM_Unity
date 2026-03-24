using System;
using UnityEngine;

namespace Snm.Runtime.DebugDraw
{
    /// <summary>
    /// A live handle to a pooled debug shape.
    /// Provides universal operations that work on any shape type.
    /// Dispose (or call Release) to return the shape to the pool.
    /// </summary>
    public sealed class DrawHandle : IDisposable
    {
        private Action  _onReturn;
        private bool    _disposed;

        // Internal so ShapeDrawer can access the underlying renderers.
        internal LineRenderer LineRenderer { get; private set; }
        internal MeshRenderer MeshRenderer { get; private set; }

        public Transform Transform  { get; private set; }
        public bool      IsDisposed => _disposed;

        // ── Factory ───────────────────────────────────────────────────────────

        internal static DrawHandle ForLine(LineRenderer lr, Action onReturn)
            => new() { LineRenderer = lr, Transform = lr.transform, _onReturn = onReturn };

        internal static DrawHandle ForMesh(MeshRenderer mr, Action onReturn)
            => new() { MeshRenderer = mr, Transform = mr.transform, _onReturn = onReturn };

        // ── Universal operations — valid on any shape ─────────────────────────

        public void SetColor(Color color)
        {
            if (_disposed) return;
            if (LineRenderer != null) { LineRenderer.startColor = LineRenderer.endColor = color; }
            else if (MeshRenderer != null) MeshRenderer.material.color = color;
        }

        public void MoveTo(Vector3 position)
        {
            if (!_disposed && Transform != null) Transform.position = position;
        }

        public void SetPositionAndRotation(Vector3 position, Quaternion rotation)
        {
            if (!_disposed && Transform != null) Transform.SetPositionAndRotation(position, rotation);
        }

        public void SetScale(Vector3 scale)
        {
            if (!_disposed && Transform != null) Transform.localScale = scale;
        }

        // ── Lifetime ─────────────────────────────────────────────────────────

        /// <summary>Returns the shape to the pool. Equivalent to Dispose().</summary>
        public void Release() => Dispose();

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            _onReturn?.Invoke();
            LineRenderer = null; MeshRenderer = null; Transform = null; _onReturn = null;
        }
    }
}
