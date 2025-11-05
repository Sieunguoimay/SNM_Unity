#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using Snm.Tools.GraphVisualizer;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Snm.Tools.ObjectBrowser
{
    public class TypeHierarchyGraphBuilder
    {
        public Graph CreateGraph(Type type)
        {
            var current = type;
            var nodes = new List<Node>();
            var connections = new List<Connection>();

            nodes.Add(CreateNode(current, out var first_Input, out var first_Output));
            var curr_Input = first_Input;

            while (current != null)
            {
                foreach (var i in current.GetInterfaces())
                {
                    nodes.Add(CreateNode(i, out var i_Input, out var i_Output));
                    connections.Add(new Connection() { to = curr_Input.id, from = i_Output.id });
                }

                var baseType = current.BaseType;

                if (baseType != null)
                {
                    nodes.Add(CreateNode(baseType, out var base_Input, out var base_Output));
                    connections.Add(new Connection() { to = curr_Input.id, from = base_Output.id });
                    curr_Input = base_Input;
                }

                current = baseType;
            }

            return new Graph()
            {
                nodes = nodes.ToArray(),
                connections = connections.ToArray()
            };
        }

        private Node CreateNode(Type type, out Port input, out Port output)
        {
            input = new Port() { name = "input", id = $"{Guid.NewGuid()}" };
            output = new Port() { name = "output", id = $"{Guid.NewGuid()}" };

            return new Node()
            {
                name = type.FullName,
                inputs = new[] { input },
                outputs = new[] { output },
            };
        }

        [MenuItem("CONTEXT/Object/Snm/TypeHierarchyGraphBuilderWindow")]
        public static void OpenTypeGraph()
        {
            var obj = Selection.activeObject;
            if (obj is MonoScript monoScript)
            {
                var graph = new TypeHierarchyGraphBuilder().CreateGraph(monoScript.GetClass());
                EditorWindow.GetWindow<GraphVisualizerWindow>().LoadGraph(graph);
            }
            else
            {
                var graph = new TypeHierarchyGraphBuilder().CreateGraph(obj.GetType());
                EditorWindow.GetWindow<GraphVisualizerWindow>().LoadGraph(graph);
            }
        }
    }
}
#endif