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
        public string id;
        public string name;
        public Port[] inputs = new Port[0];
        public Port[] outputs = new Port[0];
        public Vector2 position;

        public Node()
        {
            id = $"{Guid.NewGuid()}";
        }
    }

    [Serializable]
    public class Port
    {
        public string name;
        public string id;

        public Port()
        {
            id = $"{Guid.NewGuid()}";
        }
    }

    [Serializable]
    public class Connection
    {
        public string from;
        public string to;
    }
}