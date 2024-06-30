using System;
using System.Collections.Generic;
using UnityEngine;

namespace GameNode
{
    public class NodeSO : ScriptableObject, IGameNode
    {
        [SerializeField]
        private NodeSO[] children;

        private readonly List<IGameNode> runtimeChildren = new();

        public IGameNode Parent { get; private set; } = null;

        [field: NonSerialized]
        public bool IsSetup { get; private set; } = false;

        public virtual void Setup()
        {
            foreach (var child in GetChildren())
            {
                child?.Setup();
            }
            IsSetup = true;
        }

        public virtual void TearDown()
        {
            foreach (var child in GetChildren())
            {
                child?.TearDown();
            }
            IsSetup = false;
        }

        public void AddNode(IGameNode node)
        {
            if (node == null)
            {
                throw new ArgumentNullException("node");
            }
            node.Parent?.RemoveNode(node);
            runtimeChildren.Add(node);
            node.SetParent(this);

            if (IsSetup)
            {
                node.Setup();
            }
        }

        public void RemoveNode(IGameNode node)
        {
            if (node == null)
            {
                throw new ArgumentNullException("node");
            }
            runtimeChildren.Remove(node);
            node.SetParent(null);

            if (node.IsSetup)
            {
                node.TearDown();
            }
        }

        public virtual IEnumerable<IGameNode> GetChildren()
        {
            foreach (var c in children)
            {
                yield return c;
            }
            foreach (var c in runtimeChildren)
            {
                yield return c;
            }
        }

        public void SetParent(IGameNode node)
        {
            Parent = node;
        }
    }
}