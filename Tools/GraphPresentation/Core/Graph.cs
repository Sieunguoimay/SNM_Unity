using System;
using UnityEngine;

namespace Snm.Tools.GraphPresentation
{
    [Serializable]
    public class Graph
    {
        public Node[] nodes;
        public Connection[] connections;
    }

    [Serializable]
    public class Node
    {
        public string name;
        public Port[] inputs;
        public Port[] outputs;
        public Vector2 position;
    }

    [Serializable]
    public class Port
    {
        public string name;
        public string id;
    }

    [Serializable]
    public class Connection
    {
        public string from;
        public string to;
    }
}