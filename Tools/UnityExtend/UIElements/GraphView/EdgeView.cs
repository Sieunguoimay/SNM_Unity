using Common.UnityExtend.UIElements.Utilities;
using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace Common.UnityExtend.UIElements.GraphView
{
    public class EdgeView : VisualElement
    {
        private NodeView _from;
        private NodeView _to;

        private Vector2 _p1;
        private Vector2 _p2;
        private Vector2 _p3;
        private Vector2 _p4;

        public NodeView From => _from;
        public NodeView To => _to;
        protected virtual Color Color => IsSelected ? new Color(0.2666667f, 0.7529f, 1f, 1f) : Color.gray;
        public bool GeometryReady { get; set; }
        public bool IsSelected { get; private set; }

        public EdgeView()
        {
            GeometryReady = false;
            style.position = Position.Absolute;
            RegisterCallback<GeometryChangedEvent>(OnGeometryChanged);
            generateVisualContent += OnRepaint;
        }
        public void RefreshSelection()
        {
            IsSelected = _from.IsSelected && _to.IsSelected;
            MarkDirtyRepaint();
        }
        public void Select(VisualElement selector)
        {
            IsSelected = true;
            MarkDirtyRepaint();
        }

        public void Unselect(VisualElement selector)
        {
            IsSelected = false;
            this.MarkDirtyRepaint();
        }

        private void OnRepaint(MeshGenerationContext context)
        {
            if (_from == null || _to == null) return;

            UpdateDrawPoints();
            Painter2DUtility.DrawPath(context.painter2D, new[] { _p1, _p2, _p3, _p4 }, Color, 10f, 1f);
            DrawCap(context);

        }
        private void DrawCap(MeshGenerationContext context)
        {
            //var capMesh = context.Allocate(3, 3);
            var capDir = (_p4 - _p3).normalized;
            var capNor = Vector2.Perpendicular(capDir);
            //capMesh.SetAllVertices(new[] {
            //    new Vertex() { position = _p4 ,tint = Color },
            //    new Vertex() { position = _p4 - capDir * 3f + capNor * 2f ,tint = Color },
            //    new Vertex() { position = _p4 - capDir * 3f - capNor * 2f ,tint = Color },
            //});
            //capMesh.SetAllIndices(new ushort[] { 0, 1, 2 });
            Painter2DUtility.FillTriangle(context, _p4, _p4 - capDir * 3f + capNor * 2f, _p4 - capDir * 3f - capNor * 2f, Color);
        }
        private void OnGeometryChanged(GeometryChangedEvent evt)
        {
            GeometryReady = true;
            this.MarkDirtyRepaint();
        }

        private void OnNodeMove(NodeView view)
        {
            this.MarkDirtyRepaint();
        }

        public void Connect(NodeView from, NodeView to)
        {
            Disconnect();

            _from = from;
            _to = to;
            _from.AddEdge(this);
            _to.AddEdge(this);
            _from.OnMove += OnNodeMove;
            _to.OnMove += OnNodeMove;
        }
        public void Disconnect()
        {
            if (_from != null)
            {
                _from.OnMove -= OnNodeMove;
                _from.RemoveEdge(this);
                _from = null;
            }
            if (_to != null)
            {
                _to.OnMove -= OnNodeMove;
                _to.RemoveEdge(this);
                _to = null;
            }
        }
        private void UpdateDrawPoints()
        {
            var connector1 = _from.GetEdgeConnector(this);
            var connector2 = _to.GetEdgeConnector(this);

            var pos1 = parent.WorldToLocal(connector1.position);
            var pos2 = parent.WorldToLocal(connector2.position);

            var xMin = Mathf.Min(pos1.x, pos2.x);
            var yMin = Mathf.Min(pos1.y, pos2.y);
            var xMax = Mathf.Max(pos1.x, pos2.x);
            var yMax = Mathf.Max(pos1.y, pos2.y);

            style.left = xMin;
            style.top = yMin;
            style.width = xMax - xMin;
            style.height = yMax - yMin;

            var localNormal1 = worldTransform.inverse.rotation * connector1.normal;
            var localNormal2 = worldTransform.inverse.rotation * connector2.normal;


            _p1 = this.WorldToLocal(connector1.position);
            _p4 = this.WorldToLocal(connector2.position);

            var distance = (_p1 - _p4).magnitude;
            if (distance > 30f)
            {
                var d = 10f;
                _p2 = _p1 + new Vector2(localNormal1.x, localNormal1.y).normalized * d;
                _p3 = _p4 + new Vector2(localNormal2.x, localNormal2.y).normalized * d;
            }
            else
            {
                _p2 = _p3 = (_p1 + _p4) / 2f;
            }
        }

    }
}