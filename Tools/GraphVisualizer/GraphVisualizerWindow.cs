#if UNITY_EDITOR
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace Snm.Tools.GraphVisualizer
{
    public class GraphVisualizerWindow : EditorWindow
    {
        [SerializeField] private Graph _serializeGraph;
        [SerializeField, HideInInspector] private LayoutAlgorithm layoutAlgorithm;

        private VisualElement _world;
        private VisualElement _graphVE;

        public void CreateGUI()
        {
            var editor = Editor.CreateEditor(this);
            var layout_Buttons = new VisualElement() { style = { flexDirection = FlexDirection.Row } };

            var imgui_DefaultEditor = new IMGUIContainer(() => editor.OnInspectorGUI());
            var button_LoadTestGraph = new Button() { text = "LoadTestGraph", clickable = new(() => LoadGraph(GraphTestData.CreateComplexGraph())) };
            var enumField_Algorithm = new EnumField() { value = LayoutAlgorithm.LayoutAlgorithm_DAG };
            var button_Refresh = new Button() { text = "Refresh", clickable = new(() => Refresh()) };

            enumField_Algorithm.BindProperty(new SerializedObject(this).FindProperty(nameof(layoutAlgorithm)));

            rootVisualElement.Add(imgui_DefaultEditor);
            rootVisualElement.Add(layout_Buttons);
            layout_Buttons.Add(button_LoadTestGraph);
            layout_Buttons.Add(enumField_Algorithm);
            layout_Buttons.Add(button_Refresh);

            SetupGraphPanel();
        }

        private void Refresh()
        {
            if (_serializeGraph != null)
            {
                LayoutGraph();
                Visualize(_serializeGraph);
            }
        }

        public void LoadGraph(Graph graph)
        {
            _serializeGraph = graph;

            LayoutGraph();
            Visualize(_serializeGraph);
        }

        private void LayoutGraph()
        {
            if (layoutAlgorithm == LayoutAlgorithm.LayoutAlgorithm_DAG)
            {
                GraphLayout.LayoutGraph(_serializeGraph, GraphLayout.DefaultSpacing, cyclic: false);
            }
            else
            {
                GraphLayout.LayoutGraph(_serializeGraph, GraphLayout.DefaultSpacing, cyclic: true);
            }
        }

        public void Visualize(Graph graph)
        {
            if (_graphVE != null)
            {
                _world.Remove(_graphVE);
            }

            _graphVE = GraphVEBuilder.BuildGraphVE(graph);
            _world.Add(_graphVE);

            PutGraphToCenterOfWorld();
        }

        private void PutGraphToCenterOfWorld()
        {
            if (_graphVE == null) return;
            var graphSize = GetBounds(_graphVE.Children());
            var worldSize = new Vector2(_world.style.width.value.value, _world.style.height.value.value);
            var graphHalfSize = new Vector2(graphSize.x + graphSize.width * 0.5f, graphSize.y + graphSize.height * 0.5f);
            var worldHalfSize = new Vector2(worldSize.x / 2f, worldSize.y / 2f);
            var graphOffset = worldHalfSize - graphHalfSize;
            _graphVE.style.left = graphOffset.x;
            _graphVE.style.top = graphOffset.y;
        }

        public static Rect GetBounds(IEnumerable<VisualElement> elements)
        {
            if (elements == null || !elements.Any())
                return Rect.zero;

            float minX = float.MaxValue;
            float minY = float.MaxValue;
            float maxX = float.MinValue;
            float maxY = float.MinValue;

            foreach (var e in elements)
            {
                var x = e.style.left.value.value;
                var y = e.style.top.value.value;
                var w = e.style.width.value.value;
                var h = e.style.height.value.value;

                var xMin = x;
                var xMax = x + w;
                var yMin = y;
                var yMax = y + h;

                minX = Mathf.Min(minX, xMin);
                minY = Mathf.Min(minY, yMin);
                maxX = Mathf.Max(maxX, xMax);
                maxY = Mathf.Max(maxY, yMax);
            }

            return new Rect(minX, minY, maxX - minX, maxY - minY);
        }
        private void SetupGraphPanel()
        {
            var viewport = GraphVESupport.CreateViewport();
            _world = GraphVESupport.CreateWorld();

            rootVisualElement.Add(viewport);
            viewport.Add(_world);

            GraphVESupport.SetupDraggable(_world, null, false);
        }

        private enum LayoutAlgorithm
        {
            LayoutAlgorithm_DAG,
            LayoutAlgorithm_Cyclic,
        }
    }
}
#endif