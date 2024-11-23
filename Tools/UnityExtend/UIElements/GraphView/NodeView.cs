using Common.UnityExtend.UIElements.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;

namespace Common.UnityExtend.UIElements.GraphView
{
    public class NodeView : VisualElement, SelectManipulator.ISelectElement
    {
        private readonly List<EdgeView> _connectedEdges = new();
        private readonly Dragger _dragger;
        public event Action<NodeView> OnMove;
        public event Action<NodeView, MouseDownEvent> OnClick;

        private bool _hover;
        public bool IsSelected { get; private set; }
        public bool GeometryReady { get; set; }
        public IReadOnlyList<EdgeView> ConnectedEdges => _connectedEdges;

        public Vector2 DefaultPosition;

        public NodeView()
        {
            generateVisualContent += OnRepaint;
            style.minWidth = 25;
            style.minHeight = 25;
            style.position = Position.Absolute;
            _dragger = new Dragger(this, InvokeMoveEvent);
            GeometryReady = false;
            RegisterCallback<MouseDownEvent>(OnMouseDown);
            RegisterCallback<MouseEnterEvent>(OnMouseEnter);
            RegisterCallback<MouseLeaveEvent>(OnMouseLeave);
            RegisterCallback<GeometryChangedEvent>(OnGeometryChanged);
        }

        public IEnumerable<NodeView> QueryConnectedNodes(bool fromOrToThis)
        {
            var fromThis = fromOrToThis;
            if (fromThis)
            {
                return _connectedEdges.Where(e => e.From == this).Select(e => e.To);
            }
            else
            {
                return _connectedEdges.Where(e => e.To == this).Select(e => e.From);
            }
        }
        public IEnumerable<EdgeView> QueryConnectedEdges(bool fromOrToThis)
        {
            var fromThis = fromOrToThis;
            if (fromThis)
            {
                return _connectedEdges.Where(e => e.From == this);
            }
            else
            {
                return _connectedEdges.Where(e => e.To == this);
            }
        }

        private void OnGeometryChanged(GeometryChangedEvent evt)
        {
            GeometryReady = true;
        }

        public void ProcessMouseMove(MouseMoveEvent evt)
        {
            _dragger.ProcessDrag(evt.pressedButtons == 1, evt.mousePosition);
        }

        private void OnMouseDown(MouseDownEvent evt)
        {
            if (evt.button == 0)
            {
                InvokeClickEvent(evt);
            }

            evt.StopPropagation();
        }
        protected void InvokeClickEvent(MouseDownEvent evt)
        {
            OnClick?.Invoke(this, evt);
        }

        private void OnMouseLeave(MouseLeaveEvent evt)
        {
            _hover = false;
            this.MarkDirtyRepaint();
        }

        private void OnMouseEnter(MouseEnterEvent evt)
        {
            _hover = true;
            this.MarkDirtyRepaint();
        }

        public void InvokeMoveEvent()
        {
            OnMove?.Invoke(this);
        }
        private void OnRepaint(MeshGenerationContext context)
        {
            var painter = context.painter2D;

            var outlineColor = new Color(0.09803922f, 0.09803922f, 0.09803922f);
            var strokeWidth = .5f;

            if (IsSelected)
            {
                outlineColor = new Color(0.2666667f, 0.7529f, 1f, 1f);
                strokeWidth = 1f;
            }
            else if (_hover)
            {
                outlineColor = Color.gray;
            }

            Painter2DUtility.FillAndStrokeRoundedCornerRect(painter, contentRect, new Color(0.2313726f, 0.2313726f, 0.2313726f, 1f), outlineColor, strokeWidth); ;
        }

        public void AddEdge(EdgeView edge)
        {
            _connectedEdges.Add(edge);
        }
        public void RemoveEdge(EdgeView edge)
        {
            _connectedEdges.Remove(edge);
        }

        public EdgeConnector GetEdgeConnector(EdgeView edge)
        {
            var other = edge.From == this ? edge.To : edge.From;
            var cp = new Vector2(other.contentRect.x + other.contentRect.width / 2, other.contentRect.y + other.contentRect.height / 2);
            var otherCenterPoint = this.WorldToLocal(other.LocalToWorld(cp));
            var connectPoint = this.LocalToWorld(GetIntersectPointFromInsideRect(otherCenterPoint, contentRect, out var normal));
            var worldNormal = this.worldTransform.rotation * normal;
            return new EdgeConnector { normal = worldNormal, position = connectPoint };
        }
        private Vector2 GetIntersectPointFromInsideRect(Vector2 outsidePoint, Rect rect, out Vector2 normal)
        {
            var p1 = new Vector2(rect.x, rect.y);
            var p2 = new Vector2(rect.x + rect.width, rect.y);
            var p3 = new Vector2(rect.x + rect.width, rect.y + rect.height);
            var p4 = new Vector2(rect.x, rect.y + rect.height);
            var centerPoint = new Vector2(rect.x + rect.width / 2, rect.y + rect.height / 2);
            var ps = new[] { p1, p2, p3, p4 };
            for (var i = 0; i < 4; i++)
            {
                var lp1 = ps[i];
                var lp2 = ps[(i + 1) % 4];
                if (VisualElementTransformUtility.IntersectLineSegments2D(lp1, lp2, centerPoint, outsidePoint, out var intersect))
                {
                    normal = Vector2.Perpendicular(lp1 - lp2);
                    return intersect;
                }
            }
            normal = Vector2.up;
            return centerPoint;
        }


        public void Select(VisualElement selector)
        {
            IsSelected = true;
            this.MarkDirtyRepaint();
            foreach (var e in _connectedEdges)
            {
                e.RefreshSelection();
            }
        }

        public void Unselect(VisualElement selector)
        {
            IsSelected = false;
            this.MarkDirtyRepaint();
            foreach (var e in _connectedEdges)
            {
                e.RefreshSelection();
            }
        }

        public class EdgeConnector
        {
            public Vector2 position;
            public Vector2 normal;
        }
    }
}