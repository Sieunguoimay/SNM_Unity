#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEditor;

namespace Snm.Tools.GraphPresentation
{
    public class TypeHierarchyGraphBuilder
    {
        public Graph CreateGraph(Type type)
        {
            var current = type;
            var nodes = new List<Node>();
            var connections = new List<Connection>();
            var first_Node = CreateNode(current);
            nodes.Add(first_Node);
            var curr_InputId = first_Node.id;

            while (current != null)
            {
                foreach (var i in current.GetInterfaces())
                {
                    var node = CreateNode(i);
                    nodes.Add(node);
                    connections.Add(new Connection() { from = curr_InputId, to = node.id });
                }

                var baseType = current.BaseType;

                if (baseType != null)
                {
                    var node = CreateNode(baseType);
                    nodes.Add(node);
                    connections.Add(new Connection() { from = curr_InputId, to = node.id });
                    curr_InputId = node.id;
                }

                current = baseType;
            }

            return new Graph()
            {
                nodes = nodes.ToArray(),
                connections = connections.ToArray()
            };
        }

        private Node CreateNode(Type type)
        {
            return new Node
            {
                name = type.FullName,
                inputs = new Port[0],
                outputs = new Port[0],
            };
        }

        [MenuItem("Assets/Snm/GraphVisualizerWindow_TypeHierarchy")]
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