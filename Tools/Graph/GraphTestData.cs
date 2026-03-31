using System.Collections.Generic;
using UnityEngine;
namespace Snm.Tools.GraphPresentation
{
    public static class GraphTestData
    {
        public static Graph CreateComplexGraph(int seed = 12345)
        {
            var nodes = new List<Node>();

            Node MakeNode(int index, string name, int inCount, int outCount)
            {
                var n = new Node
                {
                    name = $"{index:00}-{name}",
                    inputs = new Port[inCount],
                    outputs = new Port[outCount],
                    position = Vector2.zero
                };
                for (int i = 0; i < inCount; i++)
                    n.inputs[i] = new Port { name = $"In{i}" };
                for (int o = 0; o < outCount; o++)
                    n.outputs[o] = new Port { name = $"Out{o}" };
                return n;
            }

            nodes.Add(MakeNode(0, "SourceA", 0, 2));
            nodes.Add(MakeNode(1, "Preprocess", 2, 2));
            nodes.Add(MakeNode(2, "FeatureX", 2, 3));
            nodes.Add(MakeNode(3, "FeatureY", 2, 2));
            nodes.Add(MakeNode(4, "JoinAB", 3, 2));
            nodes.Add(MakeNode(5, "BranchA1", 1, 2));
            nodes.Add(MakeNode(6, "BranchA2", 1, 2));
            nodes.Add(MakeNode(7, "MergeA", 3, 2));
            nodes.Add(MakeNode(8, "PathB1", 1, 1));
            nodes.Add(MakeNode(9, "PathB2", 1, 2));
            nodes.Add(MakeNode(10, "PathB3", 2, 2));
            nodes.Add(MakeNode(11, "Aggregator", 3, 2));
            nodes.Add(MakeNode(12, "Loop1_A", 1, 1));
            nodes.Add(MakeNode(13, "Loop1_B", 1, 1));
            nodes.Add(MakeNode(14, "Loop1_C", 1, 1));
            nodes.Add(MakeNode(15, "Hub", 1, 5));
            nodes.Add(MakeNode(16, "Leaf1", 1, 0));
            nodes.Add(MakeNode(17, "Leaf2", 1, 0));
            nodes.Add(MakeNode(18, "Leaf3", 1, 0));
            nodes.Add(MakeNode(19, "Controller", 1, 3));
            nodes.Add(MakeNode(20, "WorkerA", 2, 1));
            nodes.Add(MakeNode(21, "WorkerB", 2, 1));
            nodes.Add(MakeNode(22, "Reducer", 3, 2));
            nodes.Add(MakeNode(23, "Sink", 2, 0));
            nodes.Add(MakeNode(24, "IslandA", 1, 1));
            nodes.Add(MakeNode(25, "IslandB", 1, 1));

            // helper: get port ids
            string Out(int nodeIndex, int outIdx = 0) => nodes[nodeIndex].outputs[outIdx].id;
            string In(int nodeIndex, int inIdx = 0) => nodes[nodeIndex].inputs[inIdx].id;

            var conns = new List<Connection>();

            void Connect(int fromNode, int fromOut, int toNode, int toIn)
            {
                conns.Add(new Connection { from = Out(fromNode, fromOut), to = In(toNode, toIn) });
            }

            Connect(0, 0, 1, 0);
            Connect(0, 1, 2, 0);

            Connect(1, 0, 2, 1);
            Connect(1, 1, 3, 0);

            Connect(2, 0, 3, 1);
            Connect(2, 1, 5, 0);
            Connect(2, 2, 6, 0);

            Connect(3, 0, 4, 0);

            Connect(5, 0, 7, 0);
            Connect(6, 0, 7, 1);

            Connect(4, 0, 10, 0);
            Connect(7, 0, 10, 1);
            Connect(10, 0, 11, 0);

            Connect(8, 0, 9, 0);
            Connect(9, 0, 10, 1);
            Connect(9, 1, 11, 1);

            Connect(11, 0, 22, 0);

            Connect(1, 1, 6, 0);
            Connect(5, 1, 3, 0);
            Connect(4, 1, 10, 0);

            Connect(12, 0, 13, 0);
            Connect(13, 0, 14, 0);
            Connect(14, 0, 12, 0);

            Connect(10, 1, 12, 0);

            Connect(11, 1, 15, 0);
            Connect(15, 0, 16, 0);
            Connect(15, 1, 17, 0);
            Connect(15, 2, 18, 0);

            Connect(19, 0, 20, 0);
            Connect(19, 1, 21, 0);

            Connect(11, 0, 20, 1);
            Connect(7, 1, 21, 1);

            Connect(20, 0, 22, 1);
            Connect(21, 0, 22, 2);

            Connect(22, 0, 23, 0);

            Connect(10, 0, 23, 1);

            Connect(24, 0, 25, 0);
            Connect(25, 0, 24, 0);

            var rng = new System.Random(seed);
            for (int i = 0; i < nodes.Count; i++)
            {
                float rx = (float)(rng.NextDouble() - 0.5) * 600f;
                float ry = (float)(rng.NextDouble() - 0.5) * 600f;
                nodes[i].position = new Vector2(rx, ry);
            }

            return new Graph
            {
                nodes = nodes.ToArray(),
                connections = conns.ToArray()
            };
        }
    }
}
