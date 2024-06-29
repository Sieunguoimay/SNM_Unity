using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace Common.UnityExtend.UIElements
{

    public class Dragger
    {
        private readonly VisualElement _dragContent;
        private readonly Action _dragAction;

        private bool _firstDraggingFrame;
        private bool _isDragging;
        private Vector2 _dragBeginContainerPos;
        private Vector2 _dragBeginMousePos;
        public Dragger(VisualElement dragContent, Action dragAction = null)
        {
            _dragContent = dragContent;
            _dragAction = dragAction;
        }

        public void ProcessDrag(bool pressed, Vector2 mousePosition)
        {
            if (pressed)
            {
                if (_firstDraggingFrame == false)
                {
                    _firstDraggingFrame = true;
                    BeginDrag(mousePosition);
                }
                Drag(mousePosition);
            }
            else
            {
                _firstDraggingFrame = false;
                EndDrag();
            }
        }
        private void BeginDrag(Vector2 mousePosition)
        {
            _isDragging = true;
            _dragBeginContainerPos = new Vector2(_dragContent.style.left.value.value, _dragContent.style.top.value.value);
            _dragBeginMousePos = _dragContent.parent.WorldToLocal(mousePosition);
        }
        private void Drag(Vector2 mousePosition)
        {
            if (_isDragging)
            {
                UpdateDrag(_dragContent.parent.WorldToLocal(mousePosition));
            }
        }
        private void EndDrag()
        {
            _isDragging = false;
        }

        private void UpdateDrag(Vector2 targetPosition)
        {
            var delta = targetPosition - _dragBeginMousePos;
            var newContainerPos = _dragBeginContainerPos + delta;

            _dragContent.style.left = newContainerPos.x;
            _dragContent.style.top = newContainerPos.y;

            _dragAction?.Invoke();
        }
    }
}