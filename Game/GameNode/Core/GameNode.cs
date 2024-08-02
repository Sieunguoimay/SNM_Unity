using System;
using System.Collections.Generic;

namespace GameNode
{
    public class GameNode : IGameNode
    {
        private readonly List<IGameNode> runtimeChildren = new();
        private IGameNode _parent;
        private bool _isSetup = false;
        
        IGameNode IGameNode.Parent => _parent;
        bool IGameNode.IsSetup => _isSetup;

        public virtual void Setup()
        {
            foreach (var child in GetChildren())
            {
                child.Setup();
            }

            _isSetup = true;
        }

        public virtual void TearDown()
        {
            foreach (var child in GetChildren())
            {
                child.TearDown();
                child.SetParent(null);
            }


            _isSetup = false;
        }

        void IGameNode.AddNode(IGameNode node)
        {
            if (node == null)
            {
                throw new ArgumentNullException("node");
            }
            node.Parent?.RemoveNode(node);
            runtimeChildren.Add(node);
            node.SetParent(this);

            if (_isSetup)
            {
                node.Setup();
            }
        }

        void IGameNode.RemoveNode(IGameNode node)
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
            foreach (var c in runtimeChildren)
            {
                yield return c;
            }
        }

        void IGameNode.SetParent(IGameNode node)
        {
            _parent = node;
        }
    }
}

