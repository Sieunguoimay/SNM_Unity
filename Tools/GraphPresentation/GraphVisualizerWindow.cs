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

            enumField_Algorithm.BindProperty(new SerializedObject(this).FindProperty("data.layoutAlgorithm"));

            rootVisualElement.Add(imgui_DefaultEditor);
            rootVisualElement.Add(layout_Buttons);
            layout_Buttons.Add(button_LoadTestGraph);
            layout_Buttons.Add(enumField_Algorithm);
            layout_Buttons.Add(button_Refresh);

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
            if (data.isValid)
            {
                LayoutGraph();
                Visualize(data.graph);
            }
        }

        private void LayoutGraph()
        {
            var layouter = GetComponentOfType<IGraphLayout>();
            layouter.LayoutGraph(data.graph, data.layoutAlgorithm == LayoutAlgorithm.LayoutAlgorithm_Cyclic);
        }

        public void Visualize(Graph graph)
        {
            if (_graphVE != null)
            {
                _world.Remove(_graphVE);
            }

            var builder = GetComponentOfType<IGraphVEBuilder>();
            _graphVE = builder.CreateGraphVE(graph);
            _world.Add(_graphVE);
            _graphVE.schedule
                .Execute(() => LayoutCenter(_graphVE))
                .StartingIn(100);
        }

        public TComp GetComponentOfType<TComp>()
        {
            var comp = data.components
                .FirstOrDefault(c => typeof(TComp).IsAssignableFrom(Type.GetType(c.type)));

            if (!string.IsNullOrEmpty(comp.type))
            {
                var compType = Type.GetType(comp.type);
                if (compType.GetConstructors().Any(c => c.GetParameters().Length == 1))
                {
                    return (TComp)Activator.CreateInstance(compType, comp.data);
                }
                return (TComp)Activator.CreateInstance(compType);
            }
            return default;
        }

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