using System.Collections.Generic;
using UnityEngine;

namespace Snm.Tools.GraphVisualizer
{
    public static class GraphLayout
    {
        public static readonly Vector2 DefaultSpacing = new(250f, 120f);

        public static void LayoutGraph(Graph graph, Vector2 spacing, bool cyclic)
        {
            if (graph == null || graph.nodes == null)
                return;

            int n = graph.nodes.Length;

            var portOwner = new Dictionary<string, int>();

            for (int nodeIndex = 0; nodeIndex < n; nodeIndex++)
            {
                var node = graph.nodes[nodeIndex];

                if (node.inputs != null)
                {
                    foreach (var p in node.inputs)
                        if (!string.IsNullOrEmpty(p.id))
                            portOwner[p.id] = nodeIndex;
                }

                if (node.outputs != null)
                {
                    foreach (var p in node.outputs)
                        if (!string.IsNullOrEmpty(p.id))
                            portOwner[p.id] = nodeIndex;
                }
            }

            var edges = new List<(int from, int to)>();

            if (graph.connections != null)
            {
                foreach (var c in graph.connections)
                {
                    if (string.IsNullOrEmpty(c.from) || string.IsNullOrEmpty(c.to))
                        continue;

                    if (!portOwner.TryGetValue(c.from, out int fromNode))
                        continue;

                    if (!portOwner.TryGetValue(c.to, out int toNode))
                        continue;

                    if (fromNode == toNode)
                        continue;

                    edges.Add((fromNode, toNode));
                }
            }

            var positions = cyclic ? GraphAlgorithms.LayoutPossiblyCyclic(n, edges) : GraphAlgorithms.LayoutDAG(n, edges);

            for (int i = 0; i < n; i++)
            {
                var p = positions[i];
                graph.nodes[i].position = new Vector2(p.x * spacing.x, p.y * spacing.y);
            }
        }
    }

}
