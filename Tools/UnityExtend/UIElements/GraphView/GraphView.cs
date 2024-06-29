using Common.UnityExtend.UIElements.Utilities;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;

namespace Common.UnityExtend.UIElements.GraphView
{
    public class GraphView : ZoomAndDragView
    {
        private readonly VisualElement _nodeLayer = new() { name = "node-layer" };
        private readonly VisualElement _edgeLayer = new() { name = "edge-layer" };
        private readonly List<EdgeView> _edges = new();
        private readonly List<NodeView> _nodes = new();
        private readonly SelectManipulator _selectManipulator;
        public IReadOnlyList<NodeView> Nodes => _nodes;
        public IReadOnlyList<EdgeView> Edges => _edges;

        private bool _isHoldingCtrKey;
        public GraphView()
        {
            _selectManipulator = new SelectManipulator();

            _nodeLayer.style.position = Position.Absolute;
            _edgeLayer.style.position = Position.Absolute;

            ContentContainer.Add(_edgeLayer);
            ContentContainer.Add(_nodeLayer);
            this.AddManipulator(_selectManipulator);

            RegisterCallback<MouseMoveEvent>(OnMouseMove);
            RegisterCallback<MouseDownEvent>(OnMouseDown);

            _selectManipulator.OnSelectionResult += OnSelectionResult;
            ContentContainer.generateVisualContent += OnBackgroundRepaint;
        }

        private void OnBackgroundRepaint(MeshGenerationContext obj)
        {
            var children = TraverseChildrenRecurr(_nodeLayer).Concat(TraverseChildrenRecurr(_edgeLayer)).ToArray();
            var rect = ContentContainer.WorldToLocal(VisualElementTransformUtility.CalculateWorldBoundOfChildren(children));
            rect.x -= 20;
            rect.y -= 20;
            rect.width += 40;
            rect.height += 40;
            Painter2DUtility.FillRect(obj.painter2D, rect, new Color(0.1568628f, 0.1568628f, 0.1568628f, 1f));
        }

        protected override void Refresh()
        {
            base.Refresh();
            foreach (var node in Nodes)
            {
                node.style.left = node.DefaultPosition.x;
                node.style.top = node.DefaultPosition.y;
                node.InvokeMoveEvent();
            }
            ContentContainer.MarkDirtyRepaint();
        }
        private void OnSelectionResult(SelectManipulator obj)
        {
            if (!_isHoldingCtrKey)
            {
                UnselectAllNodes();
            }

            var selectedNodes = _selectManipulator.SelectedElements.OfType<NodeView>().ToArray();

            foreach (var node in selectedNodes)
            {
                node.Select(this);
            }
        }

        private void OnMouseDown(MouseDownEvent evt)
        {
            _isHoldingCtrKey = evt.ctrlKey;
            if (evt.pressedButtons == 1 && !evt.ctrlKey)
            {
                UnselectAllNodes();
            }
        }

        protected override void OnMouseMove(MouseMoveEvent evt)
        {
            base.OnMouseMove(evt);
            if (evt.ctrlKey) return;

            foreach (var n in _nodes)
            {
                if (n.IsSelected)
                {
                    n.ProcessMouseMove(evt);
                }
            }
        }

        protected override Rect CalculateFocusBound()
        {
            var selected = _nodes.Where(n => n.IsSelected).ToArray();
            if (selected.Length > 0)
            {
                var worldBound = selected[0].worldBound;
                if (selected.Length > 1)
                {
                    for (int i = 1; i < selected.Length; i++)
                    {
                        worldBound = VisualElementTransformUtility.CombineBound(worldBound, selected[i].worldBound);
                    }
                }
                return this.WorldToLocal(worldBound);
            }

            return CalculateContentBound();
        }
        private Rect CalculateContentBound()
        {
            var children = TraverseChildrenRecurr(_nodeLayer).Concat(TraverseChildrenRecurr(_edgeLayer)).ToArray();
            //Debug.Log($"CalculateContentBound {children.Length}");
            var rect=  this.WorldToLocal(VisualElementTransformUtility.CalculateWorldBoundOfChildren(children));
            rect.x -= 10;
            rect.y -= 10;
            rect.width += 20;
            rect.height += 20;
            return rect;
        }
        private IEnumerable<VisualElement> TraverseChildrenRecurr(VisualElement root)
        {
            foreach (var c in root.Children())
            {
                yield return c;
                foreach (var cc in TraverseChildrenRecurr(c))
                {
                    yield return cc;
                }
            }
        }

        public void AddNode(NodeView node)
        {
            node.OnMove += OnNodeMove;
            node.OnClick += OnNodeClick;
            _nodeLayer.Add(node);
            _nodes.Add(node);
            _nodeLayer.MarkDirtyRepaint();
        }

        public void RemoveNode(NodeView node)
        {
            node.OnMove -= OnNodeMove;
            node.OnClick -= OnNodeClick;
            _nodeLayer.Remove(node);
            _nodes.Remove(node);
            _nodeLayer.MarkDirtyRepaint();
        }

        public void AddEdge(EdgeView edge)
        {
            _edges.Add(edge);
            _edgeLayer.Add(edge);
            _edgeLayer.MarkDirtyRepaint();
        }
        public void RemoveEdge(EdgeView edge)
        {
            _edges.Remove(edge);
            _edgeLayer.Remove(edge);
            _edgeLayer.MarkDirtyRepaint();
        }
        public void ClearAll()
        {
            foreach (var node in _nodes)
            {
                node.OnMove -= OnNodeMove;
                node.OnClick -= OnNodeClick;
            }
            _nodes.Clear();
            _edges.Clear();
            _nodeLayer.Clear();
            _edgeLayer.Clear();
            _nodeLayer.MarkDirtyRepaint();
            _edgeLayer.MarkDirtyRepaint();
        }

        private void OnNodeMove(NodeView view)
        {
            ContentContainer.MarkDirtyRepaint();
        }


        private void OnNodeClick(NodeView obj, MouseDownEvent evt)
        {
            if (!obj.IsSelected)
            {
                if (!evt.ctrlKey)
                {
                    UnselectAllNodes();
                }
                obj.Select(this);
            }
        }
        public void UnselectAllNodes()
        {
            foreach (var n in _nodes)
            {
                n.Unselect(this);
            }
        }
    }
}