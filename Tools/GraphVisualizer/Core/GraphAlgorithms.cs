using System;
using System.Collections.Generic;
using UnityEngine;

namespace Snm.Tools.GraphVisualizer
{
    public static class GraphAlgorithms
    {
        public static Vector2Int[] LayoutDAG(int n, List<(int from, int to)> edges)
        {
            var adj = new List<int>[n];
            var rev = new List<int>[n];
            for (int i = 0; i < n; i++)
            {
                adj[i] = new List<int>();
                rev[i] = new List<int>();
            }

            foreach (var (from, to) in edges)
            {
                adj[from].Add(to);
                rev[to].Add(from);
            }

            var topo = TopologicalSort(n, adj);

            var layer = new int[n];
            for (int i = 0; i < n; i++)
                layer[i] = 0;

            foreach (var node in topo)
            {
                if (rev[node].Count == 0)
                {
                    layer[node] = 0;
                }
                else
                {
                    int maxParentLayer = 0;
                    foreach (var p in rev[node])
                        maxParentLayer = Math.Max(maxParentLayer, layer[p]);
                    layer[node] = maxParentLayer + 1;
                }
            }

            var layerBuckets = new Dictionary<int, List<int>>();
            foreach (var node in topo)
            {
                int L = layer[node];
                if (!layerBuckets.ContainsKey(L))
                    layerBuckets[L] = new List<int>();
                layerBuckets[L].Add(node);
            }

            var positions = new Vector2Int[n];
            foreach (var kvp in layerBuckets)
            {
                int L = kvp.Key;
                var nodesInLayer = kvp.Value;

                for (int i = 0; i < nodesInLayer.Count; i++)
                {
                    int node = nodesInLayer[i];
                    positions[node] = new Vector2Int(L, -i);
                }
            }

            return positions;
        }

        public static List<int> TopologicalSort(int n, List<int>[] adj)
        {
            var indegree = new int[n];
            for (int u = 0; u < n; u++)
                foreach (var v in adj[u])
                    indegree[v]++;

            var q = new Queue<int>();
            for (int i = 0; i < n; i++)
                if (indegree[i] == 0)
                    q.Enqueue(i);

            var order = new List<int>(n);

            while (q.Count > 0)
            {
                var u = q.Dequeue();
                order.Add(u);

                foreach (var v in adj[u])
                {
                    indegree[v]--;
                    if (indegree[v] == 0)
                        q.Enqueue(v);
                }
            }

            if (order.Count != n)
                throw new InvalidOperationException("Graph contains a cycle; layout requires a DAG.");

            return order;
        }

        public static Vector2Int[] LayoutPossiblyCyclic(int n, List<(int from, int to)> edges)
        {
            var adj = new List<int>[n];
            var rev = new List<int>[n];
            for (int i = 0; i < n; i++)
            {
                adj[i] = new List<int>();
                rev[i] = new List<int>();
            }

            foreach (var (from, to) in edges)
            {
                adj[from].Add(to);
                rev[to].Add(from);
            }

            var sccResult = TarjanScc(n, adj);
            int[] comp = sccResult.componentOf;
            int compCount = sccResult.componentCount;

            if (compCount == n)
            {
                return LayoutDagCore(n, adj, rev);
            }

            var dagAdj = new List<int>[compCount];
            var dagRev = new List<int>[compCount];
            for (int i = 0; i < compCount; i++)
            {
                dagAdj[i] = new List<int>();
                dagRev[i] = new List<int>();
            }

            foreach (var (from, to) in edges)
            {
                int cFrom = comp[from];
                int cTo = comp[to];
                if (cFrom != cTo)
                {
                    dagAdj[cFrom].Add(cTo);
                    dagRev[cTo].Add(cFrom);
                }
            }

            var compPositions = LayoutDagCore(compCount, dagAdj, dagRev);

            var nodesInComp = new List<int>[compCount];
            for (int i = 0; i < compCount; i++)
                nodesInComp[i] = new List<int>();

            for (int v = 0; v < n; v++)
                nodesInComp[comp[v]].Add(v);

            var finalPositions = new Vector2Int[n];

            for (int c = 0; c < compCount; c++)
            {
                var basePos = compPositions[c];
                var group = nodesInComp[c];
                if (group.Count == 1)
                {
                    finalPositions[group[0]] = basePos;
                }
                else
                {
                    for (int i = 0; i < group.Count; i++)
                    {
                        finalPositions[group[i]] = new Vector2Int(basePos.x, basePos.y - i);
                    }
                }
            }

            return finalPositions;
        }

        private static Vector2Int[] LayoutDagCore(int n, List<int>[] adj, List<int>[] rev)
        {
            var topo = TopologicalSortOrThrow(n, adj);

            var layer = new int[n];
            foreach (var node in topo)
            {
                if (rev[node].Count == 0)
                {
                    layer[node] = 0;
                }
                else
                {
                    int maxParentLayer = 0;
                    foreach (var p in rev[node])
                        maxParentLayer = Math.Max(maxParentLayer, layer[p]);
                    layer[node] = maxParentLayer + 1;
                }
            }

            var layerBuckets = new Dictionary<int, List<int>>();
            foreach (var node in topo)
            {
                int L = layer[node];
                if (!layerBuckets.ContainsKey(L))
                    layerBuckets[L] = new List<int>();
                layerBuckets[L].Add(node);
            }

            var positions = new Vector2Int[n];
            foreach (var kvp in layerBuckets)
            {
                int L = kvp.Key;
                var nodesInLayer = kvp.Value;
                for (int i = 0; i < nodesInLayer.Count; i++)
                {
                    int node = nodesInLayer[i];
                    positions[node] = new Vector2Int(L, -i);
                }
            }

            return positions;
        }

        private static List<int> TopologicalSortOrThrow(int n, List<int>[] adj)
        {
            var indegree = new int[n];
            for (int u = 0; u < n; u++)
                foreach (var v in adj[u])
                    indegree[v]++;

            var q = new Queue<int>();
            for (int i = 0; i < n; i++)
                if (indegree[i] == 0)
                    q.Enqueue(i);

            var order = new List<int>(n);

            while (q.Count > 0)
            {
                var u = q.Dequeue();
                order.Add(u);

                foreach (var v in adj[u])
                {
                    indegree[v]--;
                    if (indegree[v] == 0)
                        q.Enqueue(v);
                }
            }

            if (order.Count != n)
                throw new InvalidOperationException("Condensed graph still has a cycle. This should not happen.");
            return order;
        }

        private struct SccResult
        {
            public int[] componentOf;
            public int componentCount;
        }

        private static SccResult TarjanScc(int n, List<int>[] adj)
        {
            int index = 0;
            var stack = new Stack<int>();
            var onStack = new bool[n];
            var idx = new int[n];
            var low = new int[n];
            Array.Fill(idx, -1);

            int compCount = 0;
            var compOf = new int[n];

            void StrongConnect(int v)
            {
                idx[v] = index;
                low[v] = index;
                index++;
                stack.Push(v);
                onStack[v] = true;

                foreach (var w in adj[v])
                {
                    if (idx[w] == -1)
                    {
                        StrongConnect(w);
                        low[v] = Math.Min(low[v], low[w]);
                    }
                    else if (onStack[w])
                    {
                        low[v] = Math.Min(low[v], idx[w]);
                    }
                }

                if (low[v] == idx[v])
                {
                    while (true)
                    {
                        int w = stack.Pop();
                        onStack[w] = false;
                        compOf[w] = compCount;
                        if (w == v) break;
                    }
                    compCount++;
                }
            }

            for (int v = 0; v < n; v++)
                if (idx[v] == -1)
                    StrongConnect(v);

            return new SccResult
            {
                componentOf = compOf,
                componentCount = compCount
            };
        }
    }

}
