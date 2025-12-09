#if UNITY_EDITOR
using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace Snm.Tools.GraphPresentation
{
    public class GraphVESupport
    {
        public static VisualElement CreateViewport()
        {

            var viewport = new VisualElement
            {
                name = "viewport",
                style ={
                    flexGrow = 1,
                    position = Position.Relative,
                    overflow = Overflow.Hidden,
                    borderTopWidth = 1,
                    borderRightWidth = 1,
                    borderBottomWidth = 1,
                    borderLeftWidth = 1,
                    borderTopColor = new StyleColor(Color.black * 0.15f),
                    borderRightColor = new StyleColor(Color.black * 0.15f),
                    borderBottomColor = new StyleColor(Color.black * 0.15f),
                    borderLeftColor = new StyleColor(Color.black * 0.15f),
                    unityBackgroundImageTintColor = new StyleColor(Color.white),
                }
            };
            return viewport;
        }

        public static void SetupDraggable(VisualElement ve, Action dragCallback, bool checkInside)
        {
            new Dragging(ve, dragCallback, checkInside);
        }

        private class Dragging
        {
            private readonly VisualElement parent;
            private readonly VisualElement ve;
            private readonly Action dragCallback;
            private readonly bool checkInside;

            private bool _isDragging;
            private int _capturedPointerId;
            private Vector2 _pointerStartLocal;
            private float _worldLeftStart;
            private float _worldTopStart;

            public Dragging(VisualElement ve, Action dragCallback, bool checkInside)
            {
                this.ve = ve;
                this.dragCallback = dragCallback;
                this.checkInside = checkInside;

                _isDragging = false;
                _capturedPointerId = -1;
                _pointerStartLocal = Vector2.zero;
                _worldLeftStart = 0f;
                _worldTopStart = 0f;

                parent = ve.parent;

                parent.RegisterCallback<PointerDownEvent>(OnPointerDown);
                parent.RegisterCallback<PointerMoveEvent>(OnPointerMove);
                parent.RegisterCallback<PointerUpEvent>(OnPointerUp);

                parent.RegisterCallback<PointerCancelEvent>(OnPointerUp);
                parent.RegisterCallback<PointerLeaveEvent>(OnPointerUp);
            }

            void OnPointerDown(PointerDownEvent evt)
            {
                if (evt.button != 0)
                    return;

                _capturedPointerId = evt.pointerId;
                _pointerStartLocal = evt.localPosition;

                _worldLeftStart = ve.resolvedStyle.left;
                _worldTopStart = ve.resolvedStyle.top;

                var worldWidth = ve.contentRect.width;
                var worldHeight = ve.contentRect.height;

                var isValidDrag = !checkInside || CheckInside(
                    _pointerStartLocal,
                    new Vector2(_worldLeftStart, _worldTopStart),
                    new Vector2(_worldLeftStart + worldWidth, _worldTopStart + worldHeight));

                if (isValidDrag)
                {
                    _isDragging = true;
                    parent.CapturePointer(_capturedPointerId);
                    evt.StopPropagation();
                }
            }

            bool CheckInside(Vector2 point, Vector2 topLeft, Vector2 bottomRight)
            {
                return point.x > topLeft.x
                    && point.y > topLeft.y
                    && point.x < bottomRight.x
                    && point.y < bottomRight.y;
            }

            void OnPointerMove(PointerMoveEvent evt)
            {
                if (!_isDragging || evt.pointerId != _capturedPointerId || ve == null)
                    return;

                Vector2 curLocal = evt.localPosition;
                var delta = curLocal - _pointerStartLocal;
                var newLeft = _worldLeftStart + delta.x;
                var newTop = _worldTopStart + delta.y;

                SetLocalPosition(newLeft, newTop);

                evt.StopPropagation();
            }

            void OnPointerUp(IPointerEvent evt)
            {
                if (!_isDragging)
                    return;

                if (_capturedPointerId != -1)
                {
                    try { parent.ReleasePointer(_capturedPointerId); } catch { }
                }

                _isDragging = false;
                _capturedPointerId = -1;

                if (evt is EventBase eb) eb.StopPropagation();
            }

            void SetLocalPosition(float left, float top)
            {
                if (ve == null) return;
                ve.style.left = left;
                ve.style.top = top;
                dragCallback?.Invoke();
            }
        }

        public static void SetupZoomable(
            VisualElement ve,
            float minScale = 0.5f,
            float maxScale = 2.0f,
            float zoomSpeed = 0.01f)
        {
            float currentScale = 1f;

            // We listen on the VE's parent so zooming works anywhere inside it
            var parent = ve.parent;
            parent.RegisterCallback<WheelEvent>(OnWheel);

            void OnWheel(WheelEvent evt)
            {
                // delta.y > 0 scrolls down → usually zoom OUT
                float scroll = evt.delta.y;

                if (Mathf.Abs(scroll) < 0.01f)
                    return;

                // Calculate new scale
                float oldScale = currentScale;
                float newScale = Mathf.Clamp(currentScale - scroll * zoomSpeed, minScale, maxScale);
                if (Mathf.Approximately(newScale, oldScale))
                    return;

                currentScale = newScale;

                // Mouse position relative to ve
                Vector2 mouseLocal = ve.WorldToLocal(evt.mousePosition);

                // Apply scale
                ve.style.scale = new Scale(new Vector2(newScale, newScale));

                // --- Keep zoom centered on cursor ---
                // Get new mouse-local after scaling
                Vector2 mouseLocalNew = ve.WorldToLocal(evt.mousePosition);

                // Move VE so the point under cursor stays still
                Vector2 delta = mouseLocalNew - mouseLocal;

                float left = ve.style.left.value.value + delta.x;
                float top = ve.style.top.value.value + delta.y;

                ve.style.left = left;
                ve.style.top = top;

                evt.StopPropagation();
            }
        }

        public static VisualElement CreateWorld()
        {
            var width = 1400;
            var height = 900;

            var world = new VisualElement
            {
                name = "world",
                style ={
                    position = Position.Absolute,
                    width = width,
                    height = height,
                    paddingLeft = 0,
                    paddingTop = 0,
                }
            };

            var xCount = width / 100 + 1;
            var yCount = height / 100 + 1;

            for (int i = 0; i < xCount; i++)
            {
                for (int j = 0; j < yCount; j++)
                {
                    var dot = new VisualElement()
                    {
                        style ={
                            position = Position.Absolute,
                            left = i * 100,
                            top = j * 100,
                            width = 6,
                            height = 6,
                            borderTopLeftRadius = 3,
                            borderTopRightRadius = 3,
                            borderBottomLeftRadius = 3,
                            borderBottomRightRadius = 3,
                            backgroundColor = new StyleColor(Color.black * 0.25f),
                        }
                    };
                    world.Add(dot);
                }
            }
            return world;
        }
    }
}
#endif