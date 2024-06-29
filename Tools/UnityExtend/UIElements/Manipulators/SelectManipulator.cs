using Common.UnityExtend.UIElements.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;

namespace Common.UnityExtend.UIElements
{
    public class SelectManipulator : IManipulator
    {
        private VisualElement _target;
        public VisualElement target
        {
            get => _target; set
            {
                _target = value;
                InitEvents();
            }
        }

        private bool _mouseDown;
        private bool _dragging;
        private bool _firstFrameDragging;
        private Vector2 _firstMousePosition;
        private VisualElement[] _selectedElements;
        private readonly SelectionBoxDrawer _boxDrawer;
        public IReadOnlyList<VisualElement> SelectedElements => _selectedElements;

        public Rect SelectionBox { get; private set; } = new Rect();
        public bool Dragging => _dragging;

        public event Action<SelectManipulator> OnSelectionBoxChanged;
        public event Action<SelectManipulator> OnSelectionResult;

        public SelectManipulator()
        {
            _boxDrawer = new SelectionBoxDrawer(this);
        }

        private void InitEvents()
        {
            target.RegisterCallback<MouseDownEvent>(OnMouseDown);
            target.RegisterCallback<MouseMoveEvent>(OnMouseMove);
        }

        private void OnMouseDown(MouseDownEvent evt)
        {
            _mouseDown = evt.pressedButtons == 1;
        }
        private void OnMouseMove(MouseMoveEvent evt)
        {
            if (!_mouseDown) return;
            if (evt.pressedButtons == 1)
            {
                if (!_firstFrameDragging)
                {
                    _firstFrameDragging = true;
                    BeginDragging();
                    _firstMousePosition = target.WorldToLocal(evt.mousePosition);
                }
                if (_dragging)
                {
                    var currentMousePos = target.WorldToLocal(evt.mousePosition);
                    UpdateSelectionBox(currentMousePos);
                }
            }
            else
            {
                _mouseDown = false;
                _firstFrameDragging = false;
                EndDragging();
            }
        }

        private void BeginDragging()
        {
            _dragging = true;
            target.Add(_boxDrawer);
        }

        private void EndDragging()
        {
            if (!_dragging) return;
            _selectedElements = FindChildElementInBox().ToArray();
            OnSelectionResult?.Invoke(this);

            _dragging = false;
            target.Remove(_boxDrawer);
            SelectionBox = new Rect();
        }

        private void UpdateSelectionBox(Vector2 currentMousePos)
        {
            var xMin = Mathf.Min(currentMousePos.x, _firstMousePosition.x);
            var yMin = Mathf.Min(currentMousePos.y, _firstMousePosition.y);
            var xMax = Mathf.Max(currentMousePos.x, _firstMousePosition.x);
            var yMax = Mathf.Max(currentMousePos.y, _firstMousePosition.y);

            SelectionBox = new Rect(xMin, yMin, Mathf.Max(0, xMax - xMin), Mathf.Max(0, yMax - yMin));
            OnSelectionBoxChanged?.Invoke(this);
        }
        private IEnumerable<VisualElement> FindChildElementInBox()
        {
            foreach (var e in VisualElementTransformUtility.TraverseTree(target))
            {
                if (DoesIntersect(target, SelectionBox, e, e.contentRect))
                {
                    yield return e;
                }
            }
            static bool DoesIntersect(VisualElement v1, Rect r1, VisualElement v2, Rect r2)
            {

                var points1 = GetRectCornerPoints(r1).Select(v1.LocalToWorld).ToArray();
                var points2 = GetRectCornerPoints(r2).Select(v2.LocalToWorld).ToArray();

                foreach (var p in points1)
                {
                    if (r2.Contains(v2.WorldToLocal(p)))
                    {
                        return true;
                    }
                }

                foreach (var p in points2)
                {
                    if (r1.Contains(v1.WorldToLocal(p)))
                    {
                        return true;
                    }
                }

                for (var i = 0; i < points1.Length; i++)
                {
                    var lp1 = points1[i];
                    var lp2 = points1[(i + 1) % points1.Length];
                    for (var j = 0; j < points2.Length; j++)
                    {
                        var lp3 = points2[j];
                        var lp4 = points2[(j + 1) % points2.Length];

                        if (VisualElementTransformUtility.IntersectLineSegments2D(lp1, lp2, lp3, lp4, out var _))
                        {
                            return true;
                        }
                    }
                }
                return false;
            }

            static IEnumerable<Vector2> GetRectCornerPoints(Rect r)
            {
                yield return new Vector2(r.xMin, r.yMin);
                yield return new Vector2(r.xMax, r.yMin);
                yield return new Vector2(r.xMax, r.yMax);
                yield return new Vector2(r.xMin, r.yMax);
            }
        }

        public class SelectionBoxDrawer : VisualElement
        {
            private readonly SelectManipulator _selectManipulator;
            public SelectionBoxDrawer(SelectManipulator selectManipulator)
            {
                generateVisualContent += OnRepaint;
                _selectManipulator = selectManipulator;
                _selectManipulator.OnSelectionBoxChanged += OnBoxChanged;
                style.position = Position.Absolute;
            }

            private void OnBoxChanged(SelectManipulator obj)
            {
                this.MarkDirtyRepaint();
            }

            private void OnRepaint(MeshGenerationContext obj)
            {
                Painter2DUtility.DrawRect(obj.painter2D, _selectManipulator.SelectionBox, Color.red, 1);
            }
        }
        public interface ISelectElement
        {
            void Select(VisualElement selector);
            void Unselect(VisualElement selector);
        }
    }
}