#if UNITY_EDITOR
using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace Snm.Tools.GraphVisualizer
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
            var isDragging = false;
            var capturedPointerId = -1;
            var pointerStartLocal = Vector2.zero;
            var worldLeftStart = 0f;
            var worldTopStart = 0f;

            var parent = ve.parent;

            parent.RegisterCallback<PointerDownEvent>(OnPointerDown);
            parent.RegisterCallback<PointerMoveEvent>(OnPointerMove);
            parent.RegisterCallback<PointerUpEvent>(OnPointerUp);

            parent.RegisterCallback<PointerCancelEvent>(OnPointerUp);
            parent.RegisterCallback<PointerLeaveEvent>(OnPointerUp);

            void OnPointerDown(PointerDownEvent evt)
            {
                if (evt.button != 0)
                    return;

                capturedPointerId = evt.pointerId;
                pointerStartLocal = evt.localPosition;

                worldLeftStart = ve.style.left.value.value;
                worldTopStart = ve.style.top.value.value;

                var worldWidth = ve.contentRect.width;
                var worldHeight = ve.contentRect.height;

                isDragging = !checkInside || CheckInside(
                    pointerStartLocal,
                    new Vector2(worldLeftStart, worldTopStart),
                    new Vector2(worldLeftStart + worldWidth, worldTopStart + worldHeight));

                if (isDragging)
                {
                    parent.CapturePointer(capturedPointerId);
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
                if (!isDragging || evt.pointerId != capturedPointerId || ve == null)
                    return;

                Vector2 curLocal = evt.localPosition;
                var delta = curLocal - pointerStartLocal;
                var newLeft = worldLeftStart + delta.x;
                var newTop = worldTopStart + delta.y;

                SetWorldPosition(newLeft, newTop);

                evt.StopPropagation();
            }

            void OnPointerUp(IPointerEvent evt)
            {
                if (!isDragging)
                    return;

                if (capturedPointerId != -1)
                {
                    try { parent.ReleasePointer(capturedPointerId); } catch { }
                }

                isDragging = false;
                capturedPointerId = -1;

                if (evt is EventBase eb) eb.StopPropagation();
            }

            void SetWorldPosition(float left, float top)
            {
                if (ve == null) return;
                ve.style.left = left;
                ve.style.top = top;
                dragCallback?.Invoke();
            }
        }

        public static VisualElement CreateWorld()
        {
            var world = new VisualElement
            {
                name = "world",
                style ={
                    position = Position.Absolute,
                    width = 1400,
                    height = 900,
                    // backgroundColor = Color.aliceBlue,
                    paddingLeft = 0,
                    paddingTop = 0,
                }
            };
            for (int i = 0; i < 6; i++)
            {
                for (int j = 0; j < 4; j++)
                {
                    var dot = new VisualElement()
                    {
                        style ={
                            position = Position.Absolute,
                            left = 200 + i * 160,
                            top = 150 + j * 150,
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