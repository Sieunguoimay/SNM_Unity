#if UNITY_EDITOR
using Common.UnityExtend.UIElements.GraphView;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

public partial class AssetDependencyGraph : VisualElement
{
    private readonly GraphView _graphView;
    private readonly HeaderBar _headerBar;
    private FoldableDependencyNode _rootNode;

    public bool LoadScriptDependency { get; private set; }
    public bool FocusOnSelection { get; private set; }
    public bool ShowBuiltInAssets { get; private set; }
    private IReadOnlyList<NodeArrangementItem> _arrangementItems;

    public AssetDependencyGraph()
    {
        _graphView = new();
        _headerBar = new(this);
        _graphView.StretchToParentSize();
        Add(_graphView);
        Add(_headerBar);

        Selection.selectionChanged -= OnSelectionChanged;
        Selection.selectionChanged += OnSelectionChanged;
    }

    private void OnSelectionChanged()
    {
        if (!FocusOnSelection) return;
        var found = _graphView.Nodes.FirstOrDefault(n => (n as DependencyNode)?.Target == Selection.activeObject);
        if (found != null)
        {
            FocusSingleNode(found);
        }
    }

    private void Import(Object target)
    {
        var rootNode = DependencyGraphCreator.CreateGraph(target, !LoadScriptDependency, !ShowBuiltInAssets, out var nodes, out var edges, out var arrangementItems);

        _arrangementItems = arrangementItems;

        _rootNode = new FoldableDependencyNode(_graphView, rootNode.Path);

        RedirectConnections(rootNode, _rootNode);

        _rootNode.Init();

        nodes[nodes.IndexOf(rootNode)] = _rootNode;

        _graphView.ClearAll();

        AddNodesAndEdgesToGraphView(nodes, edges);
        RegisterMouseUpOnNodes(nodes);
        RegisterMouseUpOnNodes(_rootNode.SubNodes);
    }

    private void RearrangeSelectedNodes(NodeView node)
    {
        var hashSet = new HashSet<NodeView>();
        var arrangementItems = TraverseToGetArrangementItems(node, null, 0, hashSet).ToArray();
        DependencyGraphCreator.ArrangeNodes(arrangementItems, node.style.left.value.value, node.style.top.value.value);
        foreach (var n in hashSet)
        {
            foreach (var e in n.ConnectedEdges)
            {
                e.MarkDirtyRepaint();
            }
        }
    }

    private IEnumerable<NodeArrangementItem> TraverseToGetArrangementItems(NodeView node, NodeArrangementItem parent, int depth, HashSet<NodeView> hashSet)
    {
        if (!hashSet.Contains(node))
        {
            hashSet.Add(node);

            var current = new NodeArrangementItem { node = node as DependencyNode, depth = depth, parent = parent };
            yield return current;

            var children = node.QueryConnectedNodes(true);

            foreach (var c in children)
            {
                foreach (var i in TraverseToGetArrangementItems(c, current, depth + 1, hashSet))
                {
                    yield return i;
                }

            }
        }
    }

    private static void RedirectConnections(DependencyNode node, FoldableDependencyNode newNode)
    {
        var arr = node.ConnectedEdges.ToArray();
        foreach (var edge in arr)
        {
            if (edge.From == node)
            {
                edge.Connect(newNode, edge.To);
            }
            else
            {
                edge.Connect(edge.From, newNode);
            }
        }
    }
    private void RegisterMouseUpOnNodes(IEnumerable<DependencyNode> nodes)
    {
        foreach (var node in nodes)
        {
            node.RegisterCallback<MouseUpEvent>(OnMouseUpInsideNode);
        }
    }

    private void AddNodesAndEdgesToGraphView(List<DependencyNode> nodes, List<DependencyEdge> edges)
    {
        foreach (var node in nodes)
        {
            _graphView.AddNode(node);
            node.RegisterCallback<GeometryChangedEvent>(OnGeometryChanged);
        }
        foreach (var edge in edges)
        {
            _graphView.AddEdge(edge);
            edge.RegisterCallback<GeometryChangedEvent>(OnGeometryChanged);
        }

        void OnGeometryChanged(GeometryChangedEvent evt)
        {
            if (_graphView.Nodes.All(n => n.GeometryReady) && _graphView.Edges.All(n => n.GeometryReady))
            {
                _graphView.ContentContainer.MarkDirtyRepaint();
                _graphView.FocusContent();
            }

            if (evt.target is VisualElement ve)
            {
                ve.UnregisterCallback<GeometryChangedEvent>(OnGeometryChanged);
            }
        }
    }

    private void OnMouseUpInsideNode(MouseUpEvent evt)
    {
        if (evt.button == 1 && evt.currentTarget is NodeView nv)
        {
            evt.StopPropagation();
            CreateMenuForNode(nv);
        }
    }

    private void CreateMenuForNode(NodeView nv)
    {
        var menu = new GenericMenu();

        int v = _graphView.Nodes.Count(n => n.IsSelected);
        var selectedMany = v > 1;
        var hasDependencies = nv.QueryConnectedNodes(true).Any();
        var hasDependent = nv.QueryConnectedNodes(false).Any();

        menu.AddItem(new GUIContent("Arrange Children"), false, () => RearrangeSelectedNodes(nv));

        if (selectedMany)
        {
            menu.AddItem(new GUIContent("Focus"), false, () => _graphView.FocusContent());
        }
        else
        {
            menu.AddItem(new GUIContent("Focus Node"), false, () => FocusSingleNode(nv));
        }

        if (nv != _rootNode && hasDependencies)
        {
            menu.AddItem(new GUIContent("Go into"), false, () => GoInto(nv));
        }

        if (hasDependencies)
        {
            menu.AddItem(new GUIContent("Select Dependencies"), false, () => SelectDirectConnections(nv, true, false));
            menu.AddItem(new GUIContent("Select All Dependencies"), false, () => SelectConnectionsRecurr(nv, true));
        }
        if (hasDependent)
        {
            menu.AddItem(new GUIContent("Select Dependents"), false, () => SelectDirectConnections(nv, false, true));
        }
        menu.ShowAsContext();
    }

    private void FocusSingleNode(NodeView nv)
    {
        _graphView.UnselectAllNodes();
        nv.Select(_graphView);
        _graphView.FocusContent();
    }

    private void SelectDirectConnections(NodeView nv, bool fromThis, bool visibleOnly)
    {
        var edges = nv.QueryConnectedEdges(fromThis);
        foreach (var e in edges)
        {
            if (visibleOnly && !_graphView.Edges.Contains(e)) continue;
            var n = fromThis ? e.To : e.From;
            n.Select(_graphView);
        }
    }
    
    private void SelectConnectionsRecurr(NodeView nv, bool fromThis)
    {
        var dependentNodes = TraverseConnections(nv, fromThis, new HashSet<NodeView>());
        foreach (var n in dependentNodes)
        {
            n.Select(_graphView);
        }
    }

    private IEnumerable<NodeView> TraverseConnections(NodeView node, bool fromThis, HashSet<NodeView> hashSet)
    {
        if (!hashSet.Contains(node))
        {
            hashSet.Add(node);
            var dependentNodes = node.QueryConnectedNodes(fromThis);
            foreach (var n in dependentNodes)
            {
                yield return n;
                foreach (var d in TraverseConnections(n, fromThis, hashSet))
                {
                    yield return d;
                }
            }
        }
    }

    private void GoInto(NodeView nv)
    {
        var dnv = nv.Q<DependencyNode>();
        if (dnv?.Target != null)
        {
            _headerBar.ObjectTrain.Push(dnv.Target);
        }
    }
}
#endif