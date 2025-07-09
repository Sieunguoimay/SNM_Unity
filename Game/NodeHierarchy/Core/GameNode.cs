using System;
using System.Collections.Generic;

namespace Snm.Framework.NodeHierarchy
{
    public class GameNode : IGameNode
    {
        private readonly List<IGameNode> runtimeChildren = new();
        private IGameNode _parent;
        private bool _isSetup = false;

        private Action<IGameNode> _setupStatusChanged;
        private Action<IGameNode> _childNodeAdded;
        private Action<IGameNode> _childNodeRemoved;
        private Action<IGameNode> _parentChanged;

        IGameNode IGameNode.Parent => _parent;
        bool IGameNode.IsSetup => _isSetup;

        event Action<IGameNode> IGameNode.SetupStatusChanged
        {
            add => _setupStatusChanged += value; remove => _setupStatusChanged -= value;
        }
        event Action<IGameNode> IGameNode.ChildNodeAdded
        {
            add => _childNodeAdded += value; remove => _childNodeAdded -= value;
        }
        event Action<IGameNode> IGameNode.ChildNodeRemoved
        {
            add => _childNodeRemoved += value; remove => _childNodeRemoved -= value;
        }
        event Action<IGameNode> IGameNode.ParentChanged
        {
            add => _parentChanged += value; remove => _parentChanged -= value;
        }

        public virtual void Setup()
        {
            foreach (var child in GetChildren())
            {
                child.Setup();
            }

            _isSetup = true;
            _setupStatusChanged?.Invoke(this);
        }

        public virtual void TearDown()
        {
            foreach (var child in GetChildren())
            {
                child.TearDown();
                child.SetParent(null);
            }

            _isSetup = false;
            _setupStatusChanged?.Invoke(this);
        }

        void IGameNode.AddNode(IGameNode node)
        {
            if (node == null)
            {
                throw new ArgumentNullException("node");
            }
            node.Parent?.RemoveNode(node);

            runtimeChildren.Add(node);
            _childNodeAdded?.Invoke(this);

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
            _childNodeRemoved?.Invoke(this);
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
            _parentChanged?.Invoke(this);
        }
    }
}

