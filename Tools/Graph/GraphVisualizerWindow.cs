#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace Snm.Tools.GraphPresentation
{
    public class GraphVisualizerWindow : EditorWindow
    {
        [SerializeField] private SerializeData data = null;

        private VisualElement _viewport;
        private VisualElement _world;
        private VisualElement _graphVE;
        private TextField _searchField;
        private Label _searchResultLabel;
        private List<VisualElement> _highlightedNodes = new();
        private int _searchResultIndex;

        public void CreateGUI()
        {
            var toolbar = new VisualElement { style = { flexDirection = FlexDirection.Row, paddingLeft = 4f, paddingRight = 4f, paddingTop = 2f, paddingBottom = 2f } };

            var button_LoadTestGraph = new Button { text = "Test Graph", clickable = new(() => LoadGraph(GraphTestData.CreateComplexGraph())) };
            var enumField_Algorithm = new EnumField { value = LayoutAlgorithm.LayoutAlgorithm_DAG };
            var button_Refresh = new Button { text = "Refresh", clickable = new(() => Refresh()) };
            var button_FitAll = new Button { text = "Fit All", clickable = new(() => FitAll()) };

            enumField_Algorithm.BindProperty(new SerializedObject(this).FindProperty("data.layoutAlgorithm"));

            toolbar.Add(button_LoadTestGraph);
            toolbar.Add(enumField_Algorithm);
            toolbar.Add(button_Refresh);
            toolbar.Add(button_FitAll);

            // Search bar
            toolbar.Add(new VisualElement { style = { width = 10f } });
            _searchField = new TextField { style = { flexGrow = 1f, minWidth = 100f } };
            _searchField.RegisterValueChangedCallback(_ => OnSearchChanged());
            toolbar.Add(_searchField);

            _searchResultLabel = new Label { style = { unityTextAlign = TextAnchor.MiddleLeft, minWidth = 50f, color = new Color(0.6f, 0.6f, 0.6f) } };
            toolbar.Add(_searchResultLabel);

            var button_Prev = new Button { text = "\u25C0", clickable = new(() => NavigateSearch(-1)), style = { width = 24f } };
            var button_Next = new Button { text = "\u25B6", clickable = new(() => NavigateSearch(1)), style = { width = 24f } };
            toolbar.Add(button_Prev);
            toolbar.Add(button_Next);

            rootVisualElement.Add(toolbar);

            SetupGraphPanel();
            Refresh();
        }

        public void LoadGraph(
            Graph graph,
            GraphVisualizerComponent<IGraphVEBuilder> graphVEBuilderComponent = null,
            GraphVisualizerComponent<IGraphLayout> graphLayoutComponent = null)
        {
            graphVEBuilderComponent ??= GraphVisualizerComponent<IGraphVEBuilder>.Create<Component_DefaultGraphVEBuilder>(null);
            graphLayoutComponent ??= GraphVisualizerComponent<IGraphLayout>.Create<Component_SimpleGraphLayout>(Component_SimpleGraphLayout.SerializeConfig(GraphLayout.DefaultSpacing));

            data = new()
            {
                components = new GraphVisualizerComponent[] { graphVEBuilderComponent, graphLayoutComponent },
                graph = graph,
                isValid = true
            };

            Refresh();
        }

        private void Refresh()
        {
            if (!data.isValid) return;

            LayoutGraph();
            RebuildWorld();
            Visualize(data.graph);
        }

        private void RebuildWorld()
        {
            if (_world != null && _viewport != null && _viewport.Contains(_world))
                _viewport.Remove(_world);

            _world = GraphVESupport.CreateWorld(data.graph);
            _viewport.Add(_world);

            GraphVESupport.SetupDraggable(_world, null, false);
            GraphVESupport.SetupZoomable(_world);
        }

        private void LayoutGraph()
        {
            var layouter = GetComponentOfType<IGraphLayout>();
            if (layouter == null)
            {
                Debug.LogWarning("[GraphVisualizer] No IGraphLayout component found. Using default.");
                layouter = new Component_SimpleGraphLayout(Component_SimpleGraphLayout.SerializeConfig(GraphLayout.DefaultSpacing));
            }
            layouter.LayoutGraph(data.graph, data.layoutAlgorithm == LayoutAlgorithm.LayoutAlgorithm_Cyclic);
        }

        public void Visualize(Graph graph)
        {
            if (_graphVE != null && _world.Contains(_graphVE))
                _world.Remove(_graphVE);

            var builder = GetComponentOfType<IGraphVEBuilder>();
            if (builder == null)
            {
                Debug.LogWarning("[GraphVisualizer] No IGraphVEBuilder component found. Using default.");
                builder = new Component_DefaultGraphVEBuilder();
            }

            _graphVE = builder.CreateGraphVE(graph);
            _world.Add(_graphVE);
            _graphVE.schedule
                .Execute(() => LayoutCenter(_graphVE))
                .StartingIn(100);
        }

        public TComp GetComponentOfType<TComp>()
        {
            if (data.components == null) return default;

            var comp = data.components
                .FirstOrDefault(c => c != null && !string.IsNullOrEmpty(c.type) && typeof(TComp).IsAssignableFrom(Type.GetType(c.type)));

            if (comp == null || string.IsNullOrEmpty(comp.type))
            {
                Debug.LogWarning($"[GraphVisualizer] Component of type {typeof(TComp).Name} not found in data.components.");
                return default;
            }

            var compType = Type.GetType(comp.type);
            if (compType == null)
            {
                Debug.LogError($"[GraphVisualizer] Failed to resolve type: {comp.type}");
                return default;
            }

            try
            {
                if (compType.GetConstructors().Any(c => c.GetParameters().Length == 1))
                    return (TComp)Activator.CreateInstance(compType, comp.data);
                return (TComp)Activator.CreateInstance(compType);
            }
            catch (Exception e)
            {
                Debug.LogError($"[GraphVisualizer] Failed to instantiate {compType.Name}: {e.Message}");
                return default;
            }
        }

        // ── Search ───────────────────────────────────────────────

        private void OnSearchChanged()
        {
            ClearHighlights();

            var query = _searchField.value;
            if (string.IsNullOrWhiteSpace(query) || _graphVE == null || data.graph?.nodes == null)
            {
                _searchResultLabel.text = "";
                return;
            }

            var queryLower = query.ToLowerInvariant();

            foreach (var node in data.graph.nodes)
            {
                if (node.name == null || !node.name.ToLowerInvariant().Contains(queryLower))
                    continue;

                var nodeVE = _graphVE.Q($"node-{node.id}");
                if (nodeVE == null) continue;

                HighlightNode(nodeVE);
            }

            _searchResultIndex = 0;
            UpdateSearchLabel();

            if (_highlightedNodes.Count > 0)
                PanToNode(_highlightedNodes[0]);
        }

        private void NavigateSearch(int direction)
        {
            if (_highlightedNodes.Count == 0) return;

            _searchResultIndex = (_searchResultIndex + direction + _highlightedNodes.Count) % _highlightedNodes.Count;
            UpdateSearchLabel();
            PanToNode(_highlightedNodes[_searchResultIndex]);
        }

        private void UpdateSearchLabel()
        {
            if (_highlightedNodes.Count == 0)
                _searchResultLabel.text = "0 found";
            else
                _searchResultLabel.text = $"{_searchResultIndex + 1}/{_highlightedNodes.Count}";
        }

        private void HighlightNode(VisualElement nodeVE)
        {
            nodeVE.style.borderTopWidth = 2;
            nodeVE.style.borderBottomWidth = 2;
            nodeVE.style.borderLeftWidth = 2;
            nodeVE.style.borderRightWidth = 2;
            nodeVE.style.borderTopColor = new Color(1f, 0.8f, 0.2f);
            nodeVE.style.borderBottomColor = new Color(1f, 0.8f, 0.2f);
            nodeVE.style.borderLeftColor = new Color(1f, 0.8f, 0.2f);
            nodeVE.style.borderRightColor = new Color(1f, 0.8f, 0.2f);
            _highlightedNodes.Add(nodeVE);
        }

        private void ClearHighlights()
        {
            foreach (var ve in _highlightedNodes)
            {
                ve.style.borderTopWidth = 0;
                ve.style.borderBottomWidth = 0;
                ve.style.borderLeftWidth = 0;
                ve.style.borderRightWidth = 0;
            }
            _highlightedNodes.Clear();
        }

        private void PanToNode(VisualElement nodeVE)
        {
            if (_world == null || _viewport == null) return;

            var nodeWorld = nodeVE.worldBound;
            var viewportRect = _viewport.worldBound;

            var nodeCenter = nodeWorld.center;
            var viewportCenter = viewportRect.center;

            var delta = viewportCenter - nodeCenter;

            _world.style.left = _world.resolvedStyle.left + delta.x;
            _world.style.top = _world.resolvedStyle.top + delta.y;
        }

        // ── Fit All ──────────────────────────────────────────────

        private void FitAll()
        {
            if (_graphVE == null || _world == null || _viewport == null) return;

            _graphVE.schedule.Execute(() =>
            {
                // Reset scale first
                _world.style.scale = new Scale(Vector2.one);

                LayoutCenter(_graphVE);

                // Calculate scale to fit
                var bounds = GetBounds(_graphVE.Children());
                if (bounds.width < 1 || bounds.height < 1) return;

                var viewportW = _viewport.resolvedStyle.width;
                var viewportH = _viewport.resolvedStyle.height;
                var padding = 60f;

                var scaleX = (viewportW - padding) / bounds.width;
                var scaleY = (viewportH - padding) / bounds.height;
                var scale = Mathf.Clamp(Mathf.Min(scaleX, scaleY), 0.1f, 1.5f);

                _world.style.scale = new Scale(new Vector2(scale, scale));

                // Re-center after scaling
                _world.schedule.Execute(() => LayoutCenter(_world)).StartingIn(10);
            }).StartingIn(10);
        }

        // ── Layout Helpers ───────────────────────────────────────

        private static void LayoutCenter(VisualElement ve)
        {
            var parent = ve.parent;
            var veBoundRect = GetBounds(ve.Children());
            var parentSize = new Vector2(parent.resolvedStyle.width, parent.resolvedStyle.height);
            var veCenter = new Vector2(veBoundRect.x + veBoundRect.width * 0.5f, veBoundRect.y + veBoundRect.height * 0.5f);
            var parentCenter = new Vector2(parentSize.x / 2f, parentSize.y / 2f);
            var veOffset = parentCenter - veCenter;
            ve.style.left = ve.resolvedStyle.left + veOffset.x;
            ve.style.top = ve.resolvedStyle.top + veOffset.y;
        }

        public static Rect GetBounds(IEnumerable<VisualElement> elements)
        {
            if (elements == null || !elements.Any())
                return Rect.zero;

            var minX = float.MaxValue;
            var minY = float.MaxValue;
            var maxX = float.MinValue;
            var maxY = float.MinValue;

            foreach (var e in elements)
            {
                var x = e.resolvedStyle.left;
                var y = e.resolvedStyle.top;
                var w = e.resolvedStyle.width;
                var h = e.resolvedStyle.height;

                minX = Mathf.Min(minX, x);
                minY = Mathf.Min(minY, y);
                maxX = Mathf.Max(maxX, x + w);
                maxY = Mathf.Max(maxY, y + h);
            }

            return new Rect(minX, minY, maxX - minX, maxY - minY);
        }

        private void SetupGraphPanel()
        {
            _viewport = GraphVESupport.CreateViewport();
            _world = GraphVESupport.CreateWorld();

            rootVisualElement.Add(_viewport);
            _viewport.Add(_world);

            GraphVESupport.SetupDraggable(_world, null, false);
            GraphVESupport.SetupZoomable(_world);

            _world.schedule
                .Execute(() => LayoutCenter(_world))
                .StartingIn(1);
        }

        private enum LayoutAlgorithm
        {
            LayoutAlgorithm_DAG,
            LayoutAlgorithm_Cyclic,
        }

        [Serializable]
        private class SerializeData
        {
            public bool isValid;
            public Graph graph;
            public GraphVisualizerComponent[] components;
            public LayoutAlgorithm layoutAlgorithm;
        }
    }
}
#endif
