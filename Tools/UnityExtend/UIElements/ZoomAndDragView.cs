using Common.UnityExtend.UIElements.Utilities;
using System;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Common.UnityExtend.UIElements
{

    public class ZoomAndDragView : VisualElement
    {
        private readonly ZoomManipulator _zoomManipulator;
        private readonly VisualElement _contentContainer = new() { name = "contents" };
        public VisualElement ContentContainer => _contentContainer;
        protected Dragger _dragger;
        public ZoomAndDragView(float zoomMin = .25f, float zoomMax = 4f)
        {
            _zoomManipulator = new ZoomManipulator(_contentContainer, zoomMin, zoomMax);
            _dragger = new Dragger(_contentContainer, MarkDirtyRepaint);

            this.AddManipulator(_zoomManipulator);

            _contentContainer.style.position = Position.Absolute;
            Add(_contentContainer);

            RegisterCallback<MouseUpEvent>(OnMouseUp);
            RegisterCallback<MouseMoveEvent>(OnMouseMove);
        }

        protected virtual void OnMouseMove(MouseMoveEvent evt)
        {
            _dragger.ProcessDrag(evt.pressedButtons == 4, evt.mousePosition);
        }

        private void OnMouseUp(MouseUpEvent evt)
        {
            if (evt.button == 1)
            {
                ShowContextMenu();
            }
        }
        protected virtual Rect CalculateFocusBound()
        {
            return this.WorldToLocal(VisualElementTransformUtility.CalculateWorldBoundOfChildren(_contentContainer.Children()));
        }
        private void ShowContextMenu()
        {
#if UNITY_EDITOR
            var menu = new GenericMenu();
            menu.AddItem(new GUIContent("Focus"), false, () => FocusContent());
            menu.AddItem(new GUIContent("Refresh"), false, () => Refresh());
            menu.ShowAsContext();
#endif
        }

        protected virtual void Refresh()
        {
        }

        public void FocusContent()
        {
            var virtualContentRect = CalculateFocusBound();
            var focusContentRect = CalculateTargetFocusBound(virtualContentRect);
            var scale = focusContentRect.width / virtualContentRect.width;
            var contentCenter = new Vector2(virtualContentRect.x + virtualContentRect.width / 2, virtualContentRect.y + virtualContentRect.height / 2);
            _zoomManipulator.ForceZoom(contentCenter, scale);
        }

        private Rect CalculateTargetFocusBound(Rect currentContentRect)
        {
            var parentRect = new Rect(0, 0, contentRect.width, contentRect.height);

            var contentRectRatio = currentContentRect.width / currentContentRect.height;
            var targetRectRatio = parentRect.width / parentRect.height;
            var desireContentRect = new Rect();
            if (contentRectRatio > targetRectRatio)
            {
                desireContentRect.x = parentRect.x;
                desireContentRect.width = parentRect.width;
                desireContentRect.height = parentRect.width * (1f / contentRectRatio);
                desireContentRect.y = parentRect.y + parentRect.height / 2 - desireContentRect.height / 2;
            }
            else
            {
                desireContentRect.y = parentRect.y;
                desireContentRect.width = parentRect.height * contentRectRatio;
                desireContentRect.height = parentRect.height;
                desireContentRect.x = parentRect.x + parentRect.width / 2 - desireContentRect.width / 2;
            }
            return desireContentRect;
        }
    }
}